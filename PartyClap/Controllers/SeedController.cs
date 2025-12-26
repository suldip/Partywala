using Microsoft.AspNetCore.Mvc;
using PartyClap.DAL;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PartyClap.Controllers
{
    public class SeedController : Controller
    {
        private readonly DBHelper _dbHelper;
        private readonly IWebHostEnvironment _env;

        public SeedController(DBHelper dbHelper, IWebHostEnvironment env)
        {
            _dbHelper = dbHelper;
            _env = env;
        }

        public IActionResult Index()
        {
            try
            {
                var filePath = Path.Combine(_env.ContentRootPath, "SampleData_MySQL.sql");
                if (!System.IO.File.Exists(filePath))
                {
                    return Content($"Error: SampleData_MySQL.sql not found at {filePath}");
                }

                var script = System.IO.File.ReadAllText(filePath);

                using (var connection = _dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = script;
                        command.ExecuteNonQuery();
                    }

                    // Count rows
                    using (var cmd = (MySqlCommand)connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM Vendors";
                        var vendorCount = Convert.ToInt32(cmd.ExecuteScalar());

                        cmd.CommandText = "SELECT COUNT(*) FROM Services";
                        var serviceCount = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        cmd.CommandText = "SELECT COUNT(*) FROM Locations";
                        var locationCount = Convert.ToInt32(cmd.ExecuteScalar());

                        return Content($"Data seeded successfully! <br/> Vendors: {vendorCount}, Services: {serviceCount}, Locations: {locationCount} <br/> <a href='/Customer/Explore'>Go to Explore Page</a>", "text/html");
                    }
                }
            }
            catch (Exception ex)
            {
                // Verify if it's just a duplicate entry error, which implies data might already be there
                if (ex.Message.Contains("Duplicate entry"))
                {
                    return Content($"Data partially seeded or already exists. Error: {ex.Message} <br/> <a href='/Customer/Explore'>Go to Explore Page</a>", "text/html");
                }
                return Content($"Error seeding data: {ex.Message}");
            }
        }
    }
}
