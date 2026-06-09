namespace PartyClap.Services
{
    /// <summary>
    /// Adds standard browser security headers on every response (B027)
    /// and strips server-identifying headers (B055).
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private static readonly string[] ServerIdentifyingHeaders =
        {
            "Server",
            "X-Powered-By",
            "X-AspNet-Version",
            "X-AspNetMvc-Version"
        };

        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var isDevelopment = _environment.IsDevelopment();

            context.Response.OnStarting(() =>
            {
                ApplySecurityHeaders(context.Response.Headers, isDevelopment);
                StripServerIdentifyingHeaders(context.Response.Headers);
                return Task.CompletedTask;
            });

            await _next(context);
        }

        internal static void StripServerIdentifyingHeaders(IHeaderDictionary headers)
        {
            foreach (var headerName in ServerIdentifyingHeaders)
            {
                if (headers.ContainsKey(headerName))
                {
                    headers.Remove(headerName);
                }
            }
        }

        internal static void ApplySecurityHeaders(IHeaderDictionary headers, bool isDevelopment = false)
        {
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] =
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
            headers["Content-Security-Policy"] = BuildContentSecurityPolicy(isDevelopment);
        }

        private static string BuildContentSecurityPolicy(bool isDevelopment)
        {
            // Allow ASP.NET Core browser refresh / hot reload WebSockets in Development only.
            var connectSrc = isDevelopment
                ? "connect-src 'self' https://accounts.google.com ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*"
                : "connect-src 'self' https://accounts.google.com";

            return string.Join("; ", new[]
            {
                "default-src 'self'",
                "base-uri 'self'",
                "form-action 'self' https://accounts.google.com",
                "frame-ancestors 'self'",
                "frame-src 'self' https://accounts.google.com",
                "object-src 'none'",
                "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net",
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com",
                "font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.gstatic.com data:",
                "img-src 'self' data: blob: https:",
                connectSrc,
                "manifest-src 'self'",
                "worker-src 'self'"
            });
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
