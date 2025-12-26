using PartyClap.DAL;
using PartyClap.Services;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IDataService, AdoNetDataService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddScoped<DataMigrationService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Migrate data from in-memory to MySQL
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<DataMigrationService>();
    try 
    {
        migrationService.Migrate();
        
        // Load comprehensive location data first
        string locationDataPath = Path.Combine(app.Environment.ContentRootPath, "populate_indian_locations.sql");
        if (File.Exists(locationDataPath))
        {
            Console.WriteLine("Loading location data from populate_indian_locations.sql...");
            migrationService.RunSqlScript(locationDataPath);
            Console.WriteLine("Location data loaded successfully.");
        }
        else
        {
            Console.WriteLine("Warning: populate_indian_locations.sql not found. Location data may be incomplete.");
        }
        
        // Then load sample vendor data
        string sampleDataPath = Path.Combine(app.Environment.ContentRootPath, "SampleData_MySQL.sql");
        if (File.Exists(sampleDataPath))
        {
            Console.WriteLine("Loading sample data from SampleData_MySQL.sql...");
            migrationService.RunSqlScript(sampleDataPath);
            Console.WriteLine("Sample data loaded successfully.");
        }

        // Run Reviews table setup
        string reviewsSqlPath = Path.Combine(app.Environment.ContentRootPath, "db_add_reviews.sql");
        if (File.Exists(reviewsSqlPath))
        {
            Console.WriteLine("Running db_add_reviews.sql...");
            migrationService.RunSqlScript(reviewsSqlPath);
            Console.WriteLine("Reviews table setup completed.");
        }
        // Run Disputes table setup
        string disputesSqlPath = Path.Combine(app.Environment.ContentRootPath, "db_add_disputes.sql");
        if (File.Exists(disputesSqlPath))
        {
            Console.WriteLine("Running db_add_disputes.sql...");
            migrationService.RunSqlScript(disputesSqlPath);
            Console.WriteLine("Disputes table setup completed.");
        }
        
        // Run Messages table setup
        string messagesSqlPath = Path.Combine(app.Environment.ContentRootPath, "db_add_messages.sql");
        if (File.Exists(messagesSqlPath))
        {
            Console.WriteLine("Running db_add_messages.sql...");
            migrationService.RunSqlScript(messagesSqlPath);
            Console.WriteLine("Messages table setup completed.");
        }
    } 
    catch (Exception ex) 
    {
        Console.WriteLine("Migration error: " + ex.Message);
        Console.WriteLine("Stack trace: " + ex.StackTrace);
    }
}

// Configure the HTTP request pipeline.
// Set path base early so it applies to all middleware
app.UsePathBase("/PartyClap");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Enable Session
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
