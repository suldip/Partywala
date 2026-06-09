using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PartyClap.Services;

namespace PartyClap.DAL
{
    public class CustomerDAL
    {
        private readonly DBHelper _dbHelper;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CustomerDAL> _logger;
        private const string LocationsCacheKey = "CustomerDAL_Locations";

        public CustomerDAL(DBHelper dbHelper, IMemoryCache cache, ILogger<CustomerDAL> logger)
        {
            _dbHelper = dbHelper;
            _cache = cache;
            _logger = logger;
        }

        public List<ServiceListing> SearchServices(string searchTerm, string pinCode, decimal? minPrice, decimal? maxPrice, int? minRating, DateTime? eventDate, string category = null)
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
                        WHERE s.Cost > 0
                          AND v.PinCode IS NOT NULL
                          AND TRIM(v.PinCode) <> ''
                          AND (
                              v.PinCode IN (SELECT PinCode FROM AllowedPinCodes)
                              OR v.PinCode IN (SELECT DISTINCT PinCode FROM Locations WHERE PinCode IS NOT NULL)
                              OR EXISTS (
                                  SELECT 1 FROM VendorServiceLocations vsl
                                  WHERE vsl.VendorId = v.Id
                                    AND (
                                        vsl.PinCode IN (SELECT PinCode FROM AllowedPinCodes)
                                        OR vsl.PinCode IN (SELECT DISTINCT PinCode FROM Locations WHERE PinCode IS NOT NULL)
                                    )
                              )
                          )";

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        sql += " AND (v.Name LIKE @SearchTerm OR s.ServiceType LIKE @SearchTerm OR s.Description LIKE @SearchTerm OR v.Address LIKE @SearchTerm)";
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

                    var normalizedCategory = ExploreFilterHelper.NormalizeCategory(category);
                    if (!string.IsNullOrEmpty(normalizedCategory) && normalizedCategory != "all")
                    {
                        var patterns = ExploreFilterHelper.GetCategorySqlPatterns(normalizedCategory);
                        var categoryClauses = new List<string>();
                        for (var i = 0; i < patterns.Count; i++)
                        {
                            var paramName = $"@CategoryPattern{i}";
                            categoryClauses.Add($"LOWER(s.ServiceType) LIKE {paramName}");
                            command.Parameters.AddWithValue(paramName, patterns[i]);
                        }

                        sql += $" AND ({string.Join(" OR ", categoryClauses)})";
                    }

                    if (eventDate.HasValue)
                    {
                        sql += @" AND v.Id NOT IN (
                            SELECT b.VendorId FROM Bookings b
                            WHERE b.EventDate IS NOT NULL
                              AND DATE(b.EventDate) = DATE(@EventDate)
                              AND b.Status NOT IN ('Cancelled', 'Rejected', 'Completed')
                            UNION
                            SELECT sr.VendorId FROM ServiceRequests sr
                            WHERE sr.EventDate IS NOT NULL
                              AND DATE(sr.EventDate) = DATE(@EventDate)
                              AND sr.Status NOT IN ('Rejected', 'Cancelled', 'Paid', 'Expired')
                        )";
                        command.Parameters.AddWithValue("@EventDate", eventDate.Value.Date);
                    }
                    
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
            return ServiceDedupHelper.Deduplicate(services);
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
                            INSERT INTO ServiceRequests (Id, CustomerId, VendorId, ServiceId, EventDate, EventEndDate, EventStartTime, EventEndTime, DayCount, TotalCost, EventType, GuestCount, AdditionalDetails, PartyLocation, PartyPinCode, PartyLatitude, PartyLongitude, Status, CreatedDate)
                            VALUES (@Id, @CustomerId, @VendorId, @ServiceId, @EventDate, @EventEndDate, @EventStartTime, @EventEndTime, @DayCount, @TotalCost, @EventType, @GuestCount, @AdditionalDetails, @PartyLocation, @PartyPinCode, @PartyLatitude, @PartyLongitude, @Status, @CreatedDate)";
                        command.Parameters.AddWithValue("@Id", request.Id);
                        command.Parameters.AddWithValue("@CustomerId", request.CustomerId);
                        command.Parameters.AddWithValue("@VendorId", request.VendorId);
                        command.Parameters.AddWithValue("@ServiceId", request.ServiceId);
                        command.Parameters.AddWithValue("@EventDate", request.EventDate);
                        command.Parameters.AddWithValue("@EventEndDate", request.EventEndDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EventStartTime", string.IsNullOrWhiteSpace(request.EventStartTime) ? (object)DBNull.Value : request.EventStartTime);
                        command.Parameters.AddWithValue("@EventEndTime", string.IsNullOrWhiteSpace(request.EventEndTime) ? (object)DBNull.Value : request.EventEndTime);
                        command.Parameters.AddWithValue("@DayCount", request.DayCount);
                        command.Parameters.AddWithValue("@TotalCost", request.TotalCost);
                        command.Parameters.AddWithValue("@EventType", request.EventType);
                        command.Parameters.AddWithValue("@GuestCount", request.GuestCount);
                        command.Parameters.AddWithValue("@AdditionalDetails", (object)request.AdditionalDetails ?? DBNull.Value);
                        command.Parameters.AddWithValue("@PartyLocation", string.IsNullOrWhiteSpace(request.PartyLocation) ? (object)DBNull.Value : request.PartyLocation.Trim());
                        command.Parameters.AddWithValue("@PartyPinCode", string.IsNullOrWhiteSpace(request.PartyPinCode) ? (object)DBNull.Value : request.PartyPinCode.Trim());
                        command.Parameters.AddWithValue("@PartyLatitude", request.PartyLatitude ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PartyLongitude", request.PartyLongitude ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Status", request.Status);
                        command.Parameters.AddWithValue("@CreatedDate", request.CreatedDate);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service request {RequestId}", request?.Id);
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
                                    EventEndDate = reader["EventEndDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EventEndDate"]) : null,
                                    EventStartTime = reader["EventStartTime"] != DBNull.Value ? FormatBookingTime(reader["EventStartTime"]) : null,
                                    EventEndTime = reader["EventEndTime"] != DBNull.Value ? FormatBookingTime(reader["EventEndTime"]) : null,
                                    DayCount = reader["DayCount"] != DBNull.Value ? Convert.ToInt32(reader["DayCount"]) : 1,
                                    TotalCost = reader["TotalCost"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCost"]) : 0m,
                                    EventType = reader["EventType"]?.ToString() ?? "",
                                    GuestCount = Convert.ToInt32(reader["GuestCount"]),
                                    AdditionalDetails = reader["AdditionalDetails"]?.ToString(),
                                    PartyLocation = TryGetString(reader, "PartyLocation"),
                                    PartyPinCode = TryGetString(reader, "PartyPinCode"),
                                    PartyLatitude = TryGetDecimal(reader, "PartyLatitude"),
                                    PartyLongitude = TryGetDecimal(reader, "PartyLongitude"),
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
                _logger.LogError(ex, "Error loading service requests for vendor {VendorId}", vendorId);
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
                                   s.Cost as DailyCost,
                                   COALESCE(NULLIF(sr.TotalCost, 0), s.Cost * COALESCE(sr.DayCount, 1)) as ServiceCost,
                                   sr.EventEndDate, sr.DayCount, sr.TotalCost
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
                                    ["EventEndDate"] = reader["EventEndDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EventEndDate"]) : null,
                                    ["EventStartTime"] = reader["EventStartTime"] != DBNull.Value ? FormatBookingTime(reader["EventStartTime"]) : null,
                                    ["EventEndTime"] = reader["EventEndTime"] != DBNull.Value ? FormatBookingTime(reader["EventEndTime"]) : null,
                                    ["DayCount"] = reader["DayCount"] != DBNull.Value ? Convert.ToInt32(reader["DayCount"]) : 1,
                                    ["EventType"] = reader["EventType"]?.ToString() ?? "",
                                    ["GuestCount"] = Convert.ToInt32(reader["GuestCount"]),
                                    ["AdditionalDetails"] = reader["AdditionalDetails"]?.ToString() ?? "",
                                    ["PartyLocation"] = TryGetString(reader, "PartyLocation"),
                                    ["PartyPinCode"] = TryGetString(reader, "PartyPinCode"),
                                    ["PartyLatitude"] = TryGetDecimal(reader, "PartyLatitude"),
                                    ["PartyLongitude"] = TryGetDecimal(reader, "PartyLongitude"),
                                    ["Status"] = reader["Status"].ToString(),
                                    ["CreatedDate"] = Convert.ToDateTime(reader["CreatedDate"]),
                                    ["ResponseDate"] = reader["ResponseDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ResponseDate"]) : null,
                                    ["VendorName"] = reader["VendorName"]?.ToString() ?? "Unknown Vendor",
                                    ["VendorEmail"] = reader["VendorEmail"]?.ToString() ?? "",
                                    ["VendorPhone"] = reader["VendorPhone"]?.ToString() ?? "",
                                    ["ServiceType"] = reader["ServiceType"]?.ToString() ?? "",
                                    ["ServiceCost"] = reader["ServiceCost"] != DBNull.Value ? Convert.ToDecimal(reader["ServiceCost"]) : 0m,
                                    ["TotalCost"] = reader["TotalCost"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCost"]) : 0m
                                };
                                requests.Add(requestDict);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service requests for customer {CustomerId}", customerId);
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
                               COALESCE(NULLIF(sr.TotalCost, 0), s.Cost * COALESCE(sr.DayCount, 1)) as ServiceCost,
                               sr.EventEndDate, sr.DayCount, sr.TotalCost
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
                                ["EventEndDate"] = reader["EventEndDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EventEndDate"]) : null,
                                ["EventStartTime"] = reader["EventStartTime"] != DBNull.Value ? FormatBookingTime(reader["EventStartTime"]) : null,
                                ["EventEndTime"] = reader["EventEndTime"] != DBNull.Value ? FormatBookingTime(reader["EventEndTime"]) : null,
                                ["DayCount"] = reader["DayCount"] != DBNull.Value ? Convert.ToInt32(reader["DayCount"]) : 1,
                                ["EventType"] = reader["EventType"]?.ToString() ?? "",
                                ["GuestCount"] = Convert.ToInt32(reader["GuestCount"]),
                                ["AdditionalDetails"] = reader["AdditionalDetails"]?.ToString() ?? "",
                                ["PartyLocation"] = TryGetString(reader, "PartyLocation"),
                                ["PartyPinCode"] = TryGetString(reader, "PartyPinCode"),
                                ["PartyLatitude"] = TryGetDecimal(reader, "PartyLatitude"),
                                ["PartyLongitude"] = TryGetDecimal(reader, "PartyLongitude"),
                                ["Status"] = reader["Status"].ToString(),
                                ["CreatedDate"] = Convert.ToDateTime(reader["CreatedDate"]),
                                ["ResponseDate"] = reader["ResponseDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ResponseDate"]) : null,
                                ["CustomerName"] = reader["CustomerName"]?.ToString() ?? "Unknown Customer",
                                ["CustomerEmail"] = reader["CustomerEmail"]?.ToString() ?? "",
                                ["CustomerPhone"] = reader["CustomerPhone"]?.ToString() ?? "",
                                ["ServiceType"] = reader["ServiceType"]?.ToString() ?? "",
                                ["ServiceCost"] = reader["ServiceCost"] != DBNull.Value ? Convert.ToDecimal(reader["ServiceCost"]) : 0m,
                                ["TotalCost"] = reader["TotalCost"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCost"]) : 0m
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
                        SELECT Id, CustomerId, VendorId, ServiceId, BookingDate, EventDate, EventEndDate,
                               EventStartTime, EventEndTime,
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
                                EventEndDate = reader["EventEndDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EventEndDate"]) : null,
                                EventStartTime = reader["EventStartTime"] != DBNull.Value ? FormatBookingTime(reader["EventStartTime"]) : null,
                                EventEndTime = reader["EventEndTime"] != DBNull.Value ? FormatBookingTime(reader["EventEndTime"]) : null,
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
                        SELECT Id, Name, Email, Phone, PasswordHash, WalletBalance, ProfilePicture, SecondaryPhone, DateOfBirth, Gender, JoinedDate
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
                                WalletBalance = reader["WalletBalance"] != DBNull.Value ? Convert.ToDecimal(reader["WalletBalance"]) : 0m,
                                ProfilePicture = reader["ProfilePicture"]?.ToString(),
                                SecondaryPhone = reader["SecondaryPhone"]?.ToString(),
                                DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateOfBirth"]) : null,
                                Gender = reader["Gender"]?.ToString(),
                                JoinedDate = reader["JoinedDate"] != DBNull.Value ? Convert.ToDateTime(reader["JoinedDate"]) : DateTime.Now
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
                        SELECT Id, Name, Email, Phone, PasswordHash, WalletBalance, ProfilePicture, SecondaryPhone, DateOfBirth, Gender, JoinedDate
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
                                WalletBalance = reader["WalletBalance"] != DBNull.Value ? Convert.ToDecimal(reader["WalletBalance"]) : 0m,
                                ProfilePicture = reader["ProfilePicture"]?.ToString(),
                                SecondaryPhone = reader["SecondaryPhone"]?.ToString(),
                                DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateOfBirth"]) : null,
                                Gender = reader["Gender"]?.ToString(),
                                JoinedDate = reader["JoinedDate"] != DBNull.Value ? Convert.ToDateTime(reader["JoinedDate"]) : DateTime.Now
                            };
                        }
                    }
                }
            }
            return customer;
        }
        
        public List<Location> GetLocations()
        {
            // Try cache first
            if (_cache != null && _cache.TryGetValue(LocationsCacheKey, out List<Location> cachedLocations))
            {
                return cachedLocations;
            }

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

            // Cache result for 30 minutes to reduce DB load
            if (_cache != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                _cache.Set(LocationsCacheKey, locations, cacheOptions);
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
                        SELECT Id, Name, Email, Phone, PasswordHash, WalletBalance, ProfilePicture, SecondaryPhone, DateOfBirth, Gender, JoinedDate
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
                                WalletBalance = reader["WalletBalance"] != DBNull.Value ? Convert.ToDecimal(reader["WalletBalance"]) : 0m,
                                ProfilePicture = reader["ProfilePicture"]?.ToString(),
                                SecondaryPhone = reader["SecondaryPhone"]?.ToString(),
                                DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateOfBirth"]) : null,
                                Gender = reader["Gender"]?.ToString(),
                                JoinedDate = reader["JoinedDate"] != DBNull.Value ? Convert.ToDateTime(reader["JoinedDate"]) : DateTime.Now
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
        
        public List<WalletTransaction> GetWalletTransactions(string ownerId, string ownerType = "Customer", int limit = 10)
        {
            var transactions = new List<WalletTransaction>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    string idColumn = ownerType == "Vendor" ? "VendorId" : "CustomerId";
                    command.CommandText = $@"
                        SELECT Id, CustomerId, VendorId, TransactionType, Amount, Description, TransactionDate, BookingId
                        FROM WalletTransactions
                        WHERE {idColumn} = @OwnerId
                        ORDER BY TransactionDate DESC
                        LIMIT @Limit";
                    
                    command.Parameters.AddWithValue("@OwnerId", ownerId);
                    command.Parameters.AddWithValue("@Limit", limit);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(new WalletTransaction
                            {
                                Id = reader["Id"].ToString(),
                                CustomerId = reader["CustomerId"] != DBNull.Value ? reader["CustomerId"].ToString() : null,
                                VendorId = reader.HasColumn("VendorId") && reader["VendorId"] != DBNull.Value ? reader["VendorId"].ToString() : null,
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
        public void UpdateCustomerProfile(Customer customer)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Customers 
                        SET Name = @Name, 
                            Phone = @Phone, 
                            SecondaryPhone = @SecondaryPhone, 
                            DateOfBirth = @DateOfBirth, 
                            Gender = @Gender,
                            ProfilePicture = @ProfilePicture
                        WHERE Id = @Id";
                    
                    command.Parameters.AddWithValue("@Name", customer.Name);
                    command.Parameters.AddWithValue("@Phone", customer.Phone);
                    command.Parameters.AddWithValue("@SecondaryPhone", (object)customer.SecondaryPhone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DateOfBirth", (object)customer.DateOfBirth ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Gender", (object)customer.Gender ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProfilePicture", (object)customer.ProfilePicture ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Id", customer.Id);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Address> GetCustomerAddresses(string customerId)
        {
            var addresses = new List<Address>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM CustomerAddresses WHERE CustomerId = @CustomerId ORDER BY IsDefault DESC, CreatedDate DESC";
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            addresses.Add(new Address
                            {
                                Id = reader["Id"].ToString(),
                                CustomerId = reader["CustomerId"].ToString(),
                                Label = reader["Label"].ToString(),
                                RecipientName = reader["RecipientName"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                AddressLine1 = reader["AddressLine1"].ToString(),
                                AddressLine2 = reader["AddressLine2"]?.ToString(),
                                City = reader["City"].ToString(),
                                State = reader["State"].ToString(),
                                PinCode = reader["PinCode"].ToString(),
                                IsDefault = Convert.ToBoolean(reader["IsDefault"]),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                            });
                        }
                    }
                }
            }
            return addresses;
        }

        public void AddAddress(Address address)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var transaction = (MySqlTransaction)connection.BeginTransaction())
                {
                    try
                    {
                        if (address.IsDefault)
                        {
                            using (var cmd = (MySqlCommand)connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = "UPDATE CustomerAddresses SET IsDefault = 0 WHERE CustomerId = @CustomerId";
                                cmd.Parameters.AddWithValue("@CustomerId", address.CustomerId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (var cmd = (MySqlCommand)connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = @"
                                INSERT INTO CustomerAddresses (Id, CustomerId, Label, RecipientName, Phone, AddressLine1, AddressLine2, City, State, PinCode, IsDefault, CreatedDate)
                                VALUES (@Id, @CustomerId, @Label, @RecipientName, @Phone, @AddressLine1, @AddressLine2, @City, @State, @PinCode, @IsDefault, NOW())";
                            
                            cmd.Parameters.AddWithValue("@Id", address.Id);
                            cmd.Parameters.AddWithValue("@CustomerId", address.CustomerId);
                            cmd.Parameters.AddWithValue("@Label", address.Label);
                            cmd.Parameters.AddWithValue("@RecipientName", address.RecipientName);
                            cmd.Parameters.AddWithValue("@Phone", address.Phone);
                            cmd.Parameters.AddWithValue("@AddressLine1", address.AddressLine1);
                            cmd.Parameters.AddWithValue("@AddressLine2", (object)address.AddressLine2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@City", address.City);
                            cmd.Parameters.AddWithValue("@State", address.State);
                            cmd.Parameters.AddWithValue("@PinCode", address.PinCode);
                            cmd.Parameters.AddWithValue("@IsDefault", address.IsDefault);
                            
                            cmd.ExecuteNonQuery();
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

        public void DeleteAddress(string addressId, string customerId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    // Scope the delete to the owning customer to prevent IDOR.
                    command.CommandText = "DELETE FROM CustomerAddresses WHERE Id = @Id AND CustomerId = @CustomerId";
                    command.Parameters.AddWithValue("@Id", addressId);
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static string FormatBookingTime(object value)
        {
            if (value is TimeSpan ts) return ts.ToString(@"hh\:mm");
            var s = value.ToString();
            if (s.Length >= 5) return s.Substring(0, 5);
            return s;
        }

        private static string TryGetString(IDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return "";
            }
        }

        private static decimal? TryGetDecimal(IDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
    }
}
