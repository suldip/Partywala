using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class CustomerDAL
    {
        private readonly DBHelper _dbHelper;

        public CustomerDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public List<ServiceListing> SearchServices(string searchTerm, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate)
        {
            var services = new List<ServiceListing>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    var sql = @"
                        SELECT s.Id, s.VendorId, s.ServiceType, s.Description, s.Cost, s.Unit, s.MediaUrl, s.Attributes, 
                               v.Name as VendorName, v.PinCode, v.Address
                        FROM Services s
                        JOIN Vendors v ON s.VendorId = v.Id
                        WHERE 1=1";

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        sql += " AND (v.Name LIKE @SearchTerm OR s.ServiceType LIKE @SearchTerm OR s.Description LIKE @SearchTerm)";
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    }

                    if (!string.IsNullOrEmpty(pinCode))
                    {
                        sql += " AND v.PinCode = @PinCode";
                        command.Parameters.AddWithValue("@PinCode", pinCode);
                    }

                    if (minPrice.HasValue)
                    {
                        sql += " AND s.Cost >= @MinPrice";
                        command.Parameters.AddWithValue("@MinPrice", minPrice.Value);
                    }

                    if (maxPrice.HasValue)
                    {
                        sql += " AND s.Cost <= @MaxPrice";
                        command.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);
                    }

                    // Note: Rating filtering would require parsing JSON attributes or a separate Rating column
                    // For now, we'll skip rating filter in SQL and could do it in memory if needed, 
                    // or assume the Attributes JSON contains it.
                    
                    command.CommandText = sql;
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(new ServiceListing
                            {
                                Id = reader["Id"].ToString(),
                                VendorId = reader["VendorId"].ToString(),
                                ServiceType = reader["ServiceType"].ToString(),
                                Description = reader["Description"].ToString(),
                                Cost = Convert.ToDecimal(reader["Cost"]),
                                Unit = reader["Unit"].ToString(),
                                MediaUrl = reader["MediaUrl"] != DBNull.Value ? reader["MediaUrl"].ToString() : null,
                                Attributes = reader["Attributes"] != DBNull.Value ? reader["Attributes"].ToString() : null,
                                VendorName = reader["VendorName"].ToString(),
                                PinCode = reader["PinCode"] != DBNull.Value ? reader["PinCode"].ToString() : null,
                                Address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null
                            });
                        }
                    }
                }
            }
            return services;
        }
        
        public void CreateServiceRequest(ServiceRequest request)
        {
            try 
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT INTO ServiceRequests (Id, CustomerId, VendorId, ServiceId, EventDate, EventType, GuestCount, AdditionalDetails, Status, CreatedDate)
                            VALUES (@Id, @CustomerId, @VendorId, @ServiceId, @EventDate, @EventType, @GuestCount, @AdditionalDetails, @Status, @CreatedDate)";
                        command.Parameters.AddWithValue("@Id", request.Id);
                        command.Parameters.AddWithValue("@CustomerId", request.CustomerId);
                        command.Parameters.AddWithValue("@VendorId", request.VendorId);
                        command.Parameters.AddWithValue("@ServiceId", request.ServiceId);
                        command.Parameters.AddWithValue("@EventDate", request.EventDate);
                        command.Parameters.AddWithValue("@EventType", request.EventType);
                        command.Parameters.AddWithValue("@GuestCount", request.GuestCount);
                        command.Parameters.AddWithValue("@AdditionalDetails", (object)request.AdditionalDetails ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Status", request.Status);
                        command.Parameters.AddWithValue("@CreatedDate", request.CreatedDate);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating service request: {ex.Message}");
                Console.WriteLine($"Error creating service request: {ex.Message}");
                throw;
            }
        }
        
        public List<ServiceRequest> GetVendorServiceRequests(string vendorId)
        {
            var requests = new List<ServiceRequest>();
            try
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT sr.*, 
                                   c.Name as CustomerName, 
                                   c.Email as CustomerEmail, 
                                   c.Phone as CustomerPhone,
                                   s.ServiceType
                            FROM ServiceRequests sr
                            LEFT JOIN Customers c ON sr.CustomerId = c.Id
                            LEFT JOIN Services s ON sr.ServiceId = s.Id
                            WHERE sr.VendorId = @VendorId
                            ORDER BY sr.CreatedDate DESC";
                        command.Parameters.AddWithValue("@VendorId", vendorId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                requests.Add(new ServiceRequest
                                {
                                    Id = reader["Id"].ToString(),
                                    CustomerId = reader["CustomerId"].ToString(),
                                    VendorId = reader["VendorId"].ToString(),
                                    ServiceId = reader["ServiceId"].ToString(),
                                    EventDate = Convert.ToDateTime(reader["EventDate"]),
                                    EventType = reader["EventType"]?.ToString() ?? "",
                                    GuestCount = Convert.ToInt32(reader["GuestCount"]),
                                    AdditionalDetails = reader["AdditionalDetails"]?.ToString(),
                                    Status = reader["Status"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    ResponseDate = reader["ResponseDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ResponseDate"]) : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetVendorServiceRequests: {ex.Message}");
                Console.WriteLine($"Error in GetVendorServiceRequests: {ex.Message}");
            }
            return requests;
        }
        
        public List<Dictionary<string, object>> GetCustomerServiceRequestsWithDetails(string customerId)
        {
            var requests = new List<Dictionary<string, object>>();
            try
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT sr.*, 
                                   v.Name as VendorName, 
                                   v.Email as VendorEmail, 
                                   v.Phone as VendorPhone,
                                   s.ServiceType,
                                   s.Cost as ServiceCost
                            FROM ServiceRequests sr
                            LEFT JOIN Vendors v ON sr.VendorId = v.Id
                            LEFT JOIN Services s ON sr.ServiceId = s.Id
                            WHERE sr.CustomerId = @CustomerId
                            ORDER BY sr.CreatedDate DESC";
                        command.Parameters.AddWithValue("@CustomerId", customerId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var requestDict = new Dictionary<string, object>
                                {
                                    ["Id"] = reader["Id"].ToString(),
                                    ["CustomerId"] = reader["CustomerId"].ToString(),
                                    ["VendorId"] = reader["VendorId"].ToString(),
                                    ["ServiceId"] = reader["ServiceId"].ToString(),
                                    ["EventDate"] = Convert.ToDateTime(reader["EventDate"]),
                                    ["EventType"] = reader["EventType"]?.ToString() ?? "",
                                    ["GuestCount"] = Convert.ToInt32(reader["GuestCount"]),
                                    ["AdditionalDetails"] = reader["AdditionalDetails"]?.ToString() ?? "",
                                    ["Status"] = reader["Status"].ToString(),
                                    ["CreatedDate"] = Convert.ToDateTime(reader["CreatedDate"]),
                                    ["ResponseDate"] = reader["ResponseDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ResponseDate"]) : null,
                                    ["VendorName"] = reader["VendorName"]?.ToString() ?? "Unknown Vendor",
                                    ["VendorEmail"] = reader["VendorEmail"]?.ToString() ?? "",
                                    ["VendorPhone"] = reader["VendorPhone"]?.ToString() ?? "",
                                    ["ServiceType"] = reader["ServiceType"]?.ToString() ?? "",
                                    ["ServiceCost"] = reader["ServiceCost"] != DBNull.Value ? Convert.ToDecimal(reader["ServiceCost"]) : 0m
                                };
                                requests.Add(requestDict);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerServiceRequestsWithDetails: {ex.Message}");
                Console.WriteLine($"Error in GetCustomerServiceRequestsWithDetails: {ex.Message}");
            }
            return requests;
        }
        
        public List<Dictionary<string, object>> GetVendorServiceRequestsWithDetails(string vendorId)
        {
            var requests = new List<Dictionary<string, object>>();
            // Removing try-catch to expose errors
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT sr.*, 
                               c.Name as CustomerName, 
                               c.Email as CustomerEmail, 
                               c.Phone as CustomerPhone,
                               s.ServiceType,
                               s.Cost as ServiceCost
                        FROM ServiceRequests sr
                        LEFT JOIN Customers c ON sr.CustomerId = c.Id
                        LEFT JOIN Services s ON sr.ServiceId = s.Id
                        WHERE sr.VendorId = @VendorId
                        ORDER BY sr.CreatedDate DESC";
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var requestDict = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"].ToString(),
                                ["CustomerId"] = reader["CustomerId"].ToString(),
                                ["VendorId"] = reader["VendorId"].ToString(),
                                ["ServiceId"] = reader["ServiceId"].ToString(),
                                ["EventDate"] = Convert.ToDateTime(reader["EventDate"]),
                                ["EventType"] = reader["EventType"]?.ToString() ?? "",
                                ["GuestCount"] = Convert.ToInt32(reader["GuestCount"]),
                                ["AdditionalDetails"] = reader["AdditionalDetails"]?.ToString() ?? "",
                                ["Status"] = reader["Status"].ToString(),
                                ["CreatedDate"] = Convert.ToDateTime(reader["CreatedDate"]),
                                ["ResponseDate"] = reader["ResponseDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ResponseDate"]) : null,
                                ["CustomerName"] = reader["CustomerName"]?.ToString() ?? "Unknown Customer",
                                ["CustomerEmail"] = reader["CustomerEmail"]?.ToString() ?? "",
                                ["CustomerPhone"] = reader["CustomerPhone"]?.ToString() ?? "",
                                ["ServiceType"] = reader["ServiceType"]?.ToString() ?? "",
                                ["ServiceCost"] = reader["ServiceCost"] != DBNull.Value ? Convert.ToDecimal(reader["ServiceCost"]) : 0m
                            };
                            requests.Add(requestDict);
                        }
                    }
                }
            }
            return requests;
        }
        
        public void UpdateServiceRequestStatus(string requestId, string status)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE ServiceRequests 
                        SET Status = @Status, 
                            ResponseDate = NOW()
                        WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@Id", requestId);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public List<Booking> GetCustomerBookings(string customerId)
        {
            var bookings = new List<Booking>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, CustomerId, VendorId, ServiceId, BookingDate, EventDate, 
                               VendorCost, CustomerTotalCost, AdvancePaid, BalanceAmount, 
                               Status, BalancePaidOnApp
                        FROM Bookings
                        WHERE CustomerId = @CustomerId
                        ORDER BY BookingDate DESC";
                    
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookings.Add(new Booking
                            {
                                Id = reader["Id"].ToString(),
                                CustomerId = reader["CustomerId"].ToString(),
                                VendorId = reader["VendorId"].ToString(),
                                ServiceId = reader["ServiceId"].ToString(),
                                BookingDate = Convert.ToDateTime(reader["BookingDate"]),
                                EventDate = Convert.ToDateTime(reader["EventDate"]),
                                VendorCost = Convert.ToDecimal(reader["VendorCost"]),
                                CustomerTotalCost = Convert.ToDecimal(reader["CustomerTotalCost"]),
                                AdvancePaid = Convert.ToDecimal(reader["AdvancePaid"]),
                                BalanceAmount = Convert.ToDecimal(reader["BalanceAmount"]),
                                Status = reader["Status"].ToString(),
                                BalancePaidOnApp = Convert.ToBoolean(reader["BalancePaidOnApp"])
                            });
                        }
                    }
                }
            }
            return bookings;
        }

        public void RegisterCustomer(Customer customer)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Customers (Id, Name, Email, Phone, PasswordHash, WalletBalance)
                        VALUES (@Id, @Name, @Email, @Phone, @PasswordHash, @WalletBalance)";
                    
                    command.Parameters.AddWithValue("@Id", customer.Id);
                    command.Parameters.AddWithValue("@Name", customer.Name);
                    command.Parameters.AddWithValue("@Email", customer.Email);
                    command.Parameters.AddWithValue("@Phone", customer.Phone);
                    command.Parameters.AddWithValue("@PasswordHash", customer.Password); // Storing plain for MVP, MUST HASH in prod
                    command.Parameters.AddWithValue("@WalletBalance", customer.WalletBalance);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        public Customer GetCustomerByEmail(string email)
        {
            Customer customer = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, Name, Email, Phone, PasswordHash, WalletBalance
                        FROM Customers
                        WHERE Email = @Email
                        LIMIT 1";
                    
                    command.Parameters.AddWithValue("@Email", email);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customer = new Customer
                            {
                                Id = reader["Id"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                Password = reader["PasswordHash"].ToString(),
                                WalletBalance = reader["WalletBalance"] != DBNull.Value ? Convert.ToDecimal(reader["WalletBalance"]) : 0m
                            };
                        }
                    }
                }
            }
            return customer;
        }

        public Customer GetCustomerByPhone(string phone)
        {
            Customer customer = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, Name, Email, Phone, PasswordHash, WalletBalance
                        FROM Customers
                        WHERE Phone = @Phone
                        LIMIT 1";
                    
                    command.Parameters.AddWithValue("@Phone", phone);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customer = new Customer
                            {
                                Id = reader["Id"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                Password = reader["PasswordHash"].ToString(),
                                WalletBalance = reader["WalletBalance"] != DBNull.Value ? Convert.ToDecimal(reader["WalletBalance"]) : 0m
                            };
                        }
                    }
                }
            }
            return customer;
        }
        
        public List<Location> GetLocations()
        {
            var locations = new List<Location>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT PinCode, MAX(Address) as Address FROM Vendors WHERE PinCode IS NOT NULL GROUP BY PinCode";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var address = reader["Address"].ToString();
                            var parts = address.Split(',');
                            var city = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim();
                            
                            locations.Add(new Location
                            {
                                PinCode = reader["PinCode"].ToString(),
                                DisplayName = $"{city} ({reader["PinCode"]})"
                            });
                        }
                    }
                }
            }
            return locations;
        }
        
        public Customer GetCustomerById(string customerId)
        {
            Customer customer = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, Name, Email, Phone, PasswordHash, WalletBalance
                        FROM Customers
                        WHERE Id = @Id
                        LIMIT 1";
                    
                    command.Parameters.AddWithValue("@Id", customerId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customer = new Customer
                            {
                                Id = reader["Id"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                Password = reader["PasswordHash"].ToString(),
                                WalletBalance = reader["WalletBalance"] != DBNull.Value ? Convert.ToDecimal(reader["WalletBalance"]) : 0m
                            };
                        }
                    }
                }
            }
            return customer;
        }
        
        public void AddMoneyToWallet(string customerId, decimal amount, string description)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var transaction = (MySqlTransaction)connection.BeginTransaction())
                {
                    try
                    {
                        // Update customer wallet balance
                        using (var command = (MySqlCommand)connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
                                UPDATE Customers 
                                SET WalletBalance = WalletBalance + @Amount
                                WHERE Id = @CustomerId";
                            command.Parameters.AddWithValue("@Amount", amount);
                            command.Parameters.AddWithValue("@CustomerId", customerId);
                            command.ExecuteNonQuery();
                        }
                        
                        // Record transaction
                        using (var command = (MySqlCommand)connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
                                INSERT INTO WalletTransactions (Id, CustomerId, TransactionType, Amount, Description, TransactionDate)
                                VALUES (@Id, @CustomerId, @TransactionType, @Amount, @Description, NOW())";
                            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                            command.Parameters.AddWithValue("@CustomerId", customerId);
                            command.Parameters.AddWithValue("@TransactionType", "Credit");
                            command.Parameters.AddWithValue("@Amount", amount);
                            command.Parameters.AddWithValue("@Description", description);
                            command.ExecuteNonQuery();
                        }
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        public List<WalletTransaction> GetWalletTransactions(string customerId, int limit = 10)
        {
            var transactions = new List<WalletTransaction>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, CustomerId, TransactionType, Amount, Description, TransactionDate, BookingId
                        FROM WalletTransactions
                        WHERE CustomerId = @CustomerId
                        ORDER BY TransactionDate DESC
                        LIMIT @Limit";
                    
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    command.Parameters.AddWithValue("@Limit", limit);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(new WalletTransaction
                            {
                                Id = reader["Id"].ToString(),
                                CustomerId = reader["CustomerId"].ToString(),
                                TransactionType = reader["TransactionType"].ToString(),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                Description = reader["Description"]?.ToString(),
                                TransactionDate = Convert.ToDateTime(reader["TransactionDate"]),
                                BookingId = reader["BookingId"] != DBNull.Value ? reader["BookingId"].ToString() : null
                            });
                        }
                    }
                }
            }
            return transactions;
        }
    }
}
