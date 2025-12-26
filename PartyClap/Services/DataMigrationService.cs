using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.Services
{
    public class DataMigrationService
    {
        private readonly string _connectionString;
        private readonly InMemoryDataService _inMemoryData;

        public DataMigrationService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _inMemoryData = new InMemoryDataService();
        }

        public void Migrate()
        {
            Console.WriteLine("Starting migration from in-memory to MySQL...");

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                // 1. Migrate Customers
                foreach (var customer in _inMemoryData.Customers)
                {
                    InsertCustomer(connection, customer);
                }

                // 2. Migrate Vendors & their Services
                foreach (var vendor in _inMemoryData.Vendors)
                {
                    InsertVendor(connection, vendor);
                    
                    if (vendor.Services != null)
                    {
                        foreach (var service in vendor.Services)
                        {
                            InsertService(connection, service);
                        }
                    }
                }

                Console.WriteLine("Migration completed successfully.");
            }
        }

        private void InsertCustomer(MySqlConnection conn, Customer customer)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT IGNORE INTO Customers (Id, Name, Email, Phone) VALUES (@Id, @Name, @Email, @Phone)";
                cmd.Parameters.AddWithValue("@Id", customer.Id);
                cmd.Parameters.AddWithValue("@Name", customer.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", customer.Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", customer.Phone ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertVendor(MySqlConnection conn, Vendor vendor)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT IGNORE INTO Vendors (Id, Name, Email, Phone, Address, PinCode, IsRegistered, TrustScore, WalletBalance) 
                                   VALUES (@Id, @Name, @Email, @Phone, @Address, @PinCode, @IsRegistered, @TrustScore, @WalletBalance)";
                cmd.Parameters.AddWithValue("@Id", vendor.Id);
                cmd.Parameters.AddWithValue("@Name", vendor.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", vendor.Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", vendor.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", vendor.Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PinCode", vendor.PinCode ?? "400001");
                cmd.Parameters.AddWithValue("@IsRegistered", vendor.IsRegistered);
                cmd.Parameters.AddWithValue("@TrustScore", vendor.TrustScore);
                cmd.Parameters.AddWithValue("@WalletBalance", vendor.WalletBalance);
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertService(MySqlConnection conn, ServiceListing service)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT IGNORE INTO Services (Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost) 
                                   VALUES (@Id, @VendorId, @ServiceType, @Description, @Cost, @Unit, @MediaUrl, @Attributes, @WeekendCost)";
                cmd.Parameters.AddWithValue("@Id", service.Id);
                cmd.Parameters.AddWithValue("@VendorId", service.VendorId);
                cmd.Parameters.AddWithValue("@ServiceType", service.ServiceType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Description", service.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Cost", service.Cost);
                cmd.Parameters.AddWithValue("@Unit", service.Unit ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@MediaUrl", service.MediaUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Attributes", service.Attributes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@WeekendCost", service.WeekendCost ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void RunSqlScript(string scriptPath)
        {
            if (!File.Exists(scriptPath)) return;

            string script = File.ReadAllText(scriptPath);
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                // MySql.Data doesn't support executing multiple statements separated by ; in one go easily unless you use MySqlScript
                var scriptExecutor = new MySqlScript(connection, script);
                scriptExecutor.Execute();
            }
        }
    }
}
