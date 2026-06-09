using Microsoft.AspNetCore.Server.Kestrel.Core;
using PartyClap.DAL;
using PartyClap.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

const string AppPathBase = "/PartyClap";

// Don't advertise the web server in the "Server" response header (B055).
// Kestrel/local dev: suppress here. IIS production: also requires web.config removeServerHeader + outbound rewrite.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
    if (builder.Environment.IsDevelopment())
    {
        // Avoid Chrome ERR_HTTP2_PROTOCOL_ERROR with the dev HTTPS cert on Windows.
        serverOptions.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1;
        });
    }
});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Use PascalCase for JSON property names (matches C# model properties)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddSingleton<DBHelper>();
builder.Services.AddScoped<VendorDAL>();
builder.Services.AddScoped<CustomerDAL>();
builder.Services.AddScoped<LocationDAL>();
builder.Services.AddScoped<CartDAL>();
builder.Services.AddScoped<AdminDAL>();
builder.Services.AddScoped<ReviewDAL>();
builder.Services.AddScoped<MessageDAL>();
builder.Services.AddScoped<StateDAL>();
builder.Services.AddScoped<SettingsDAL>();
builder.Services.AddScoped<NotificationDAL>();
builder.Services.AddScoped<IDataService, AdoNetDataService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
builder.Services.AddSingleton<IPricingService, PricingService>();

// Cookie-based authentication. Identity is carried in a signed cookie (claims),
// enforced via [Authorize]/[Authorize(Roles=...)] instead of forgeable session strings.
var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = AppPathBase + "/Account/Login";
        options.AccessDeniedPath = AppPathBase + "/Account/AccessDenied";
        options.Cookie.Path = AppPathBase;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    })
    // Short-lived cookie that temporarily holds the external (Google) identity
    // between the OAuth challenge and our callback, before we issue our app cookie.
    .AddCookie(AuthConstants.ExternalScheme, options =>
    {
        options.Cookie.Name = "PartyClap.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Path = AppPathBase;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

// Google (Gmail) login is registered only when credentials are configured
// (Authentication:Google:ClientId / ClientSecret via user-secrets or env vars),
// so the app still starts cleanly in environments without OAuth set up.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = AuthConstants.ExternalScheme;
        // With UsePathBase("/PartyClap") this resolves to /PartyClap/signin-google.
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });
}
builder.Services.AddAuthorization();

// Add MemoryCache for caching frequently-read data
builder.Services.AddMemoryCache();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOtpService, SessionOtpService>();
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));
builder.Services.AddHttpClient<SmsIndiaHubOtpSender>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IOtpSmsSender>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OtpOptions>>().Value;
    if (options.SmsEnabled)
    {
        return sp.GetRequiredService<SmsIndiaHubOtpSender>();
    }

    return sp.GetRequiredService<NoOpOtpSmsSender>();
});
builder.Services.AddScoped<NoOpOtpSmsSender>();

// Trust X-Forwarded-* from IIS / reverse proxies so HTTPS detection and redirects stay correct.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });

    builder.Services.PostConfigure<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);

    builder.Services.PostConfigure<CookieAuthenticationOptions>(
        AuthConstants.ExternalScheme,
        options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = AppPathBase;
    options.Cookie.Name = "PartyClap.Session";
    if (!builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

var app = builder.Build();

// Ensure AppSettings table exists (platform fee % and other admin settings).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SettingsDAL>().EnsureSchema();
    scope.ServiceProvider.GetRequiredService<NotificationDAL>().EnsureSchema();
    scope.ServiceProvider.GetRequiredService<CartDAL>().EnsureSchema();
}

// Log startup failures to logs/startup.log (helps diagnose IIS 500.30).
var startupLogDir = Path.Combine(app.Environment.ContentRootPath, "logs");
try
{
    Directory.CreateDirectory(startupLogDir);
    File.AppendAllText(
        Path.Combine(startupLogDir, "startup.log"),
        $"[{DateTime.UtcNow:O}] PartyClap started. Environment={app.Environment.EnvironmentName}{Environment.NewLine}");
}
catch
{
    // Ignore — folder permissions may be fixed after first deploy.
}

// Configure the HTTP request pipeline.
// Honor proxy-forwarded scheme/host before redirects and path base.
app.UseForwardedHeaders();

// Buffer HTML in Development so Chrome gets Content-Length instead of chunked bodies
// (fixes ERR_INCOMPLETE_CHUNKED_ENCODING / blank pages on localhost HTTPS).
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await next(context);

        buffer.Position = 0;
        context.Response.Body = originalBody;

        var isHtml = context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;
        if (isHtml && context.Response.StatusCode == StatusCodes.Status200OK)
        {
            var bytes = buffer.ToArray();
            context.Response.ContentLength = bytes.Length;
            context.Response.Headers.Remove("Transfer-Encoding");
            await originalBody.WriteAsync(bytes);
        }
        else
        {
            await buffer.CopyToAsync(originalBody);
        }
    });
}

// Session/auth cookies use Path=/PartyClap — redirect bare /Account/... URLs so cookies work.
// When hosted as an IIS sub-app at /PartyClap, PathBase is already /PartyClap and Path is / — skip redirect.
app.Use(async (context, next) =>
{
    var pathBase = context.Request.PathBase.Value ?? string.Empty;
    if (pathBase.StartsWith(AppPathBase, StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var path = context.Request.Path.Value ?? string.Empty;
    if (!path.StartsWith(AppPathBase, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect(AppPathBase + path + context.Request.QueryString, permanent: false);
        return;
    }

    await next();
});

// Set path base early so it applies to all middleware
app.UsePathBase(AppPathBase);

// Security headers (X-Frame-Options, CSP, etc.) and strip server-identifying headers.
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHtmlCommentStripping();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS is enforced by the IIS site binding in production; app-level redirect can loop behind IIS/proxies.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = StaticFileCachePolicy.Apply
});

app.UseRouting();
app.UseSession(); // Enable Session
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Catch any request that did not match a real controller/action endpoint.
app.MapFallbackToController("PageNotFound", "Home");

try
{
    app.Run();
}
catch (Exception ex)
{
    try
    {
        File.AppendAllText(
            Path.Combine(startupLogDir, "startup-error.log"),
            $"[{DateTime.UtcNow:O}] Startup failed:{Environment.NewLine}{ex}{Environment.NewLine}");
    }
    catch
    {
        // Best-effort diagnostic for IIS 500.30.
    }

    throw;
}
