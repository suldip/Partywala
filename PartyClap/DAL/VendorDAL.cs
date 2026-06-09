using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using PartyClap.Models;
using PartyClap.Services;

namespace PartyClap.DAL
{
    public class VendorDAL
    {
        private readonly DBHelper _dbHelper;

        public VendorDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public void RegisterVendor(Vendor vendor)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_RegisterVendor";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", vendor.Id);
                    command.Parameters.AddWithValue("p_Name", vendor.Name);
                    command.Parameters.AddWithValue("p_Email", vendor.Email);
                    command.Parameters.AddWithValue("p_Phone", vendor.Phone);
                    command.Parameters.AddWithValue("p_Address", vendor.Address ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("p_PinCode", vendor.PinCode);
                    command.Parameters.AddWithValue("p_IsRegistered", vendor.IsRegistered);
                    command.Parameters.AddWithValue("p_TrustScore", vendor.TrustScore);
                    command.Parameters.AddWithValue("p_WalletBalance", vendor.WalletBalance);
                    command.Parameters.AddWithValue("p_AccountHolderName", vendor.AccountHolderName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("p_AccountNumber", vendor.AccountNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("p_IfscCode", vendor.IfscCode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("p_UpiId", vendor.UpiId ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
                
                // Save Service Locations
                if (vendor.ServiceLocations != null && vendor.ServiceLocations.Any())
                {
                    foreach (var pinCode in vendor.ServiceLocations)
                    {
                        if (!string.IsNullOrWhiteSpace(pinCode))
                        {
                            using (var command = (MySqlCommand)connection.CreateCommand())
                            {
                                command.CommandText = "sp_AddVendorServiceLocation";
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("p_VendorId", vendor.Id);
                                command.Parameters.AddWithValue("p_PinCode", pinCode);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        public List<Vendor> GetAllVendors()
        {
            var vendors = new List<Vendor>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT v.Id, v.Name, v.Email, v.Phone, v.Address, v.PinCode,
                               v.IsRegistered, v.TrustScore, v.WalletBalance, v.ProfilePicture,
                               (SELECT COUNT(*) FROM Services s WHERE s.VendorId = v.Id AND s.Cost > 0) AS ServiceCount
                        FROM Vendors v
                        ORDER BY v.Name";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vendors.Add(new Vendor
                            {
                                Id = reader["Id"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"]?.ToString(),
                                Phone = reader["Phone"]?.ToString(),
                                Address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null,
                                PinCode = reader["PinCode"] != DBNull.Value ? reader["PinCode"].ToString() : null,
                                IsRegistered = reader["IsRegistered"] != DBNull.Value && Convert.ToBoolean(reader["IsRegistered"]),
                                TrustScore = reader["TrustScore"] != DBNull.Value ? Convert.ToInt32(reader["TrustScore"]) : 100,
                                WalletBalance = reader["WalletBalance"] != DBNull.Value ? Convert.ToDecimal(reader["WalletBalance"]) : 0m,
                                ProfilePicture = reader["ProfilePicture"] != DBNull.Value ? reader["ProfilePicture"].ToString() : null
                            });
                        }
                    }
                }
            }
            return vendors;
        }

        public List<AdminVendorCalendarSummary> GetVendorCalendarSummaries(DateTime fromDate, DateTime toDate)
        {
            var vendors = GetAllVendors();
            var summaries = new List<AdminVendorCalendarSummary>();

            foreach (var vendor in vendors)
            {
                var schedule = GetVendorSchedule(vendor.Id, fromDate, toDate);
                var services = GetVendorServices(vendor.Id);
                summaries.Add(new AdminVendorCalendarSummary
                {
                    VendorId = vendor.Id,
                    VendorName = vendor.Name,
                    PinCode = vendor.PinCode,
                    Phone = vendor.Phone,
                    IsRegistered = vendor.IsRegistered,
                    ServiceCount = services.Count(s => s.Cost > 0),
                    BookedDays = schedule.Count(e => e.IsBooked),
                    UnderProcessDays = schedule.Count(e => e.IsUnderProcess),
                    AvailableDays = schedule.Count(e => !e.IsBooked && !e.IsUnderProcess && e.Date >= DateTime.Today)
                });
            }

            return summaries;
        }

        public Vendor GetVendor(string id)
        {
            Vendor vendor = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetVendor";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vendor = new Vendor
                            {
                                Id = reader["Id"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                IsRegistered = Convert.ToBoolean(reader["IsRegistered"]),
                                TrustScore = Convert.ToInt32(reader["TrustScore"]),
                                WalletBalance = Convert.ToDecimal(reader["WalletBalance"]),
                                ProfilePicture = reader.HasColumn("ProfilePicture") && reader["ProfilePicture"] != DBNull.Value ? reader["ProfilePicture"].ToString() : null,
                                Address = reader.HasColumn("Address") && reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null,
                                PinCode = reader.HasColumn("PinCode") && reader["PinCode"] != DBNull.Value ? reader["PinCode"].ToString() : null,
                                AccountHolderName = reader.HasColumn("AccountHolderName") && reader["AccountHolderName"] != DBNull.Value ? reader["AccountHolderName"].ToString() : null,
                                AccountNumber = reader.HasColumn("AccountNumber") && reader["AccountNumber"] != DBNull.Value ? reader["AccountNumber"].ToString() : null,
                                IfscCode = reader.HasColumn("IfscCode") && reader["IfscCode"] != DBNull.Value ? reader["IfscCode"].ToString() : null,
                                UpiId = reader.HasColumn("UpiId") && reader["UpiId"] != DBNull.Value ? reader["UpiId"].ToString() : null
                            };
                        }
                    }
                }
            }
            return vendor;
        }
        
        public Vendor GetVendorByEmail(string email)
        {
            Vendor vendor = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetVendorByEmail";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Email", email);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vendor = new Vendor
                            {
                                Id = reader["Id"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                IsRegistered = Convert.ToBoolean(reader["IsRegistered"]),
                                TrustScore = Convert.ToInt32(reader["TrustScore"]),
                                WalletBalance = Convert.ToDecimal(reader["WalletBalance"]),
                                ProfilePicture = reader.HasColumn("ProfilePicture") && reader["ProfilePicture"] != DBNull.Value ? reader["ProfilePicture"].ToString() : null,
                                Address = reader.HasColumn("Address") && reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null,
                                PinCode = reader.HasColumn("PinCode") && reader["PinCode"] != DBNull.Value ? reader["PinCode"].ToString() : null,
                                AccountHolderName = reader.HasColumn("AccountHolderName") && reader["AccountHolderName"] != DBNull.Value ? reader["AccountHolderName"].ToString() : null,
                                AccountNumber = reader.HasColumn("AccountNumber") && reader["AccountNumber"] != DBNull.Value ? reader["AccountNumber"].ToString() : null,
                                IfscCode = reader.HasColumn("IfscCode") && reader["IfscCode"] != DBNull.Value ? reader["IfscCode"].ToString() : null,
                                UpiId = reader.HasColumn("UpiId") && reader["UpiId"] != DBNull.Value ? reader["UpiId"].ToString() : null
                            };
                        }
                    }
                }
            }
            return vendor;
        }

        public bool HasVendorServices(string vendorId)
        {
            if (string.IsNullOrWhiteSpace(vendorId))
            {
                return false;
            }

            return GetVendorServices(vendorId).Any();
        }

        public int GetVendorActiveServiceCount(string vendorId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Services WHERE VendorId = @VendorId AND Cost > 0";
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// Creates or updates a vendor contact record after mobile OTP verification.
        /// Used during the multi-step registration wizard before the full profile is submitted.
        /// </summary>
        public Vendor SaveVendorContactDraft(string firstName, string lastName, string email, string phone)
        {
            var nameParts = new[] { firstName?.Trim(), lastName?.Trim() }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var fullName = string.Join(" ", nameParts);

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();

                var existing = GetVendorByPhone(phone);
                if (existing != null)
                {
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = @"
                            UPDATE Vendors
                            SET Name = @Name, Email = @Email
                            WHERE Id = @Id";
                        command.Parameters.AddWithValue("@Id", existing.Id);
                        command.Parameters.AddWithValue("@Name", fullName);
                        command.Parameters.AddWithValue("@Email", email);
                        command.ExecuteNonQuery();
                    }

                    existing.Name = fullName;
                    existing.Email = email;
                    return existing;
                }

                var vendor = new Vendor
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = fullName,
                    Email = email,
                    Phone = phone,
                    IsRegistered = false,
                    TrustScore = 100,
                    WalletBalance = 0
                };

                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Vendors (Id, Name, Email, Phone, Address, PinCode, IsRegistered, TrustScore, WalletBalance)
                        VALUES (@Id, @Name, @Email, @Phone, NULL, NULL, @IsRegistered, @TrustScore, @WalletBalance)";
                    command.Parameters.AddWithValue("@Id", vendor.Id);
                    command.Parameters.AddWithValue("@Name", vendor.Name);
                    command.Parameters.AddWithValue("@Email", vendor.Email);
                    command.Parameters.AddWithValue("@Phone", vendor.Phone);
                    command.Parameters.AddWithValue("@IsRegistered", vendor.IsRegistered);
                    command.Parameters.AddWithValue("@TrustScore", vendor.TrustScore);
                    command.Parameters.AddWithValue("@WalletBalance", vendor.WalletBalance);
                    command.ExecuteNonQuery();
                }

                return vendor;
            }
        }

        public Vendor GetVendorByPhone(string phone)
        {
            Vendor vendor = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, Name, Email, Phone, IsRegistered, TrustScore, WalletBalance FROM Vendors WHERE Phone = @Phone";
                    command.Parameters.AddWithValue("@Phone", phone);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vendor = new Vendor
                            {
                                Id = reader["Id"].ToString(),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"].ToString(),
                                IsRegistered = Convert.ToBoolean(reader["IsRegistered"]),
                                TrustScore = Convert.ToInt32(reader["TrustScore"]),
                                WalletBalance = Convert.ToDecimal(reader["WalletBalance"]),
                                ProfilePicture = reader.HasColumn("ProfilePicture") && reader["ProfilePicture"] != DBNull.Value ? reader["ProfilePicture"].ToString() : null,
                                Address = reader.HasColumn("Address") && reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null,
                                PinCode = reader.HasColumn("PinCode") && reader["PinCode"] != DBNull.Value ? reader["PinCode"].ToString() : null,
                                AccountHolderName = reader.HasColumn("AccountHolderName") && reader["AccountHolderName"] != DBNull.Value ? reader["AccountHolderName"].ToString() : null,
                                AccountNumber = reader.HasColumn("AccountNumber") && reader["AccountNumber"] != DBNull.Value ? reader["AccountNumber"].ToString() : null,
                                IfscCode = reader.HasColumn("IfscCode") && reader["IfscCode"] != DBNull.Value ? reader["IfscCode"].ToString() : null,
                                UpiId = reader.HasColumn("UpiId") && reader["UpiId"] != DBNull.Value ? reader["UpiId"].ToString() : null
                            };
                        }
                    }
                }
            }
            return vendor;
        }

        public void AddService(ServiceListing service)
        {
            if (FindExistingDuplicateServiceId(service) != null)
            {
                return;
            }

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Services (Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost)
                        VALUES (@Id, @VendorId, @ServiceType, @Description, @Cost, @Unit, @MediaUrl, @Attributes, @WeekendCost)";
                    
                    command.Parameters.AddWithValue("@Id", service.Id);
                    command.Parameters.AddWithValue("@VendorId", service.VendorId);
                    command.Parameters.AddWithValue("@ServiceType", service.ServiceType);
                    command.Parameters.AddWithValue("@Description", service.Description);
                    command.Parameters.AddWithValue("@Cost", service.Cost);
                    command.Parameters.AddWithValue("@Unit", service.Unit);
                    command.Parameters.AddWithValue("@MediaUrl", service.MediaUrl ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Attributes", service.Attributes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@WeekendCost", service.WeekendCost ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<ServiceListing> GetVendorServices(string vendorId)
        {
            var services = new List<ServiceListing>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost
                        FROM Services
                        WHERE VendorId = @VendorId
                        ORDER BY ServiceType, Cost, Id";
                    
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    
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
                                WeekendCost = reader["WeekendCost"] != DBNull.Value ? Convert.ToDecimal(reader["WeekendCost"]) : (decimal?)null
                            });
                        }
                    }
                }
            }

            var deduped = ServiceDedupHelper.Deduplicate(services);
            if (deduped.Count < services.Count)
            {
                PurgeDuplicateServices(vendorId, deduped.Select(s => s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase));
            }

            return deduped;
        }

        private string FindExistingDuplicateServiceId(ServiceListing service)
        {
            var existing = GetVendorServicesWithoutCleanup(service.VendorId);
            return existing
                .FirstOrDefault(s => ServiceDedupHelper.AreEquivalent(s, service))
                ?.Id;
        }

        private List<ServiceListing> GetVendorServicesWithoutCleanup(string vendorId)
        {
            var services = new List<ServiceListing>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost
                        FROM Services
                        WHERE VendorId = @VendorId";
                    command.Parameters.AddWithValue("@VendorId", vendorId);

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
                                WeekendCost = reader["WeekendCost"] != DBNull.Value ? Convert.ToDecimal(reader["WeekendCost"]) : (decimal?)null
                            });
                        }
                    }
                }
            }

            return services;
        }

        private void PurgeDuplicateServices(string vendorId, HashSet<string> keepIds)
        {
            if (keepIds == null || keepIds.Count == 0)
            {
                return;
            }

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    var parameterNames = keepIds.Select((_, index) => $"@KeepId{index}").ToList();
                    command.CommandText = $@"
                        DELETE FROM Services
                        WHERE VendorId = @VendorId
                          AND Id NOT IN ({string.Join(", ", parameterNames)})";
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    var index = 0;
                    foreach (var keepId in keepIds)
                    {
                        command.Parameters.AddWithValue($"@KeepId{index}", keepId);
                        index++;
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        public ServiceListing GetService(string serviceId)
        {
            ServiceListing service = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, VendorId, ServiceType, Description, Cost, Unit, MediaUrl, Attributes, WeekendCost
                        FROM Services
                        WHERE Id = @Id";
                    
                    command.Parameters.AddWithValue("@Id", serviceId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            service = new ServiceListing
                            {
                                Id = reader["Id"].ToString(),
                                VendorId = reader["VendorId"].ToString(),
                                ServiceType = reader["ServiceType"].ToString(),
                                Description = reader["Description"].ToString(),
                                Cost = Convert.ToDecimal(reader["Cost"]),
                                Unit = reader["Unit"].ToString(),
                                MediaUrl = reader["MediaUrl"] != DBNull.Value ? reader["MediaUrl"].ToString() : null,
                                Attributes = reader["Attributes"] != DBNull.Value ? reader["Attributes"].ToString() : null,
                                WeekendCost = reader["WeekendCost"] != DBNull.Value ? Convert.ToDecimal(reader["WeekendCost"]) : (decimal?)null
                            };
                        }
                    }
                }
            }
            return service;
        }

        public void UpdateService(ServiceListing service)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Services 
                        SET ServiceType = @ServiceType, 
                            Description = @Description, 
                            Cost = @Cost, 
                            Unit = @Unit,
                            MediaUrl = @MediaUrl, 
                            Attributes = @Attributes,
                            WeekendCost = @WeekendCost
                        WHERE Id = @Id";
                    
                    command.Parameters.AddWithValue("@Id", service.Id);
                    command.Parameters.AddWithValue("@ServiceType", service.ServiceType);
                    command.Parameters.AddWithValue("@Description", service.Description);
                    command.Parameters.AddWithValue("@Cost", service.Cost);
                    command.Parameters.AddWithValue("@Unit", service.Unit);
                    command.Parameters.AddWithValue("@MediaUrl", service.MediaUrl ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Attributes", service.Attributes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@WeekendCost", service.WeekendCost ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteService(string serviceId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Services WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", serviceId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddPortfolioItem(PortfolioItem item)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_AddPortfolioItem";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", item.Id);
                    command.Parameters.AddWithValue("p_VendorId", item.VendorId);
                    command.Parameters.AddWithValue("p_MediaType", item.MediaType);
                    command.Parameters.AddWithValue("p_MediaUrl", item.MediaUrl);
                    command.Parameters.AddWithValue("p_Description", item.Description);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<PortfolioItem> GetVendorPortfolio(string vendorId)
        {
            var portfolio = new List<PortfolioItem>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetVendorPortfolio";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_VendorId", vendorId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            portfolio.Add(new PortfolioItem
                            {
                                Id = reader["Id"].ToString(),
                                VendorId = reader["VendorId"].ToString(),
                                MediaType = reader["MediaType"].ToString(),
                                MediaUrl = reader["MediaUrl"].ToString(),
                                Description = reader["Description"].ToString()
                            });
                        }
                    }
                }
            }
            return portfolio;
        }
        
        public void EnsureCalendarBlockSchema()
        {
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS VendorCalendarBlocks (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            VendorId VARCHAR(36) NOT NULL,
                            BlockDate DATE NOT NULL,
                            StartTime VARCHAR(10) NULL,
                            EndTime VARCHAR(10) NULL,
                            IsAvailable TINYINT(1) NOT NULL DEFAULT 0,
                            IsHourly TINYINT(1) NOT NULL DEFAULT 0,
                            Label VARCHAR(120) NULL,
                            CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            INDEX idx_vendor_date (VendorId, BlockDate)
                        )";
                    command.ExecuteNonQuery();
                }
                AddColumnIfMissing(connection, "VendorCalendarBlocks", "IsHourly", "TINYINT(1) NOT NULL DEFAULT 0");
            }
        }

        private void AddColumnIfMissing(MySqlConnection connection, string table, string column, string definition)
        {
            try
            {
                using (var check = connection.CreateCommand())
                {
                    check.CommandText = @"SELECT COUNT(1) FROM information_schema.columns
                                          WHERE table_schema = DATABASE()
                                            AND table_name = @Table
                                            AND column_name = @Column";
                    check.Parameters.AddWithValue("@Table", table);
                    check.Parameters.AddWithValue("@Column", column);
                    if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;
                }
                using (var alter = connection.CreateCommand())
                {
                    alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
                    alter.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding column {table}.{column}: {ex.Message}");
            }
        }

        public List<VendorCalendarBlock> GetVendorCalendarBlocks(string vendorId, DateTime fromDate, DateTime toDate)
        {
            EnsureCalendarBlockSchema();
            var blocks = new List<VendorCalendarBlock>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, VendorId, BlockDate, StartTime, EndTime, IsAvailable, IsHourly, Label, CreatedDate
                        FROM VendorCalendarBlocks
                        WHERE VendorId = @VendorId
                          AND BlockDate BETWEEN DATE(@FromDate) AND DATE(@ToDate)
                        ORDER BY BlockDate, StartTime";
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                    command.Parameters.AddWithValue("@ToDate", toDate.Date);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            blocks.Add(new VendorCalendarBlock
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                VendorId = reader["VendorId"].ToString(),
                                BlockDate = Convert.ToDateTime(reader["BlockDate"]).Date,
                                StartTime = reader["StartTime"]?.ToString(),
                                EndTime = reader["EndTime"]?.ToString(),
                                IsAvailable = Convert.ToInt32(reader["IsAvailable"]) == 1,
                                IsHourly = reader["IsHourly"] != DBNull.Value && Convert.ToInt32(reader["IsHourly"]) == 1,
                                Label = reader["Label"]?.ToString(),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                            });
                        }
                    }
                }
            }
            return blocks;
        }

        public void AddVendorCalendarBlock(VendorCalendarBlock block)
        {
            EnsureCalendarBlockSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO VendorCalendarBlocks (VendorId, BlockDate, StartTime, EndTime, IsAvailable, IsHourly, Label, CreatedDate)
                        VALUES (@VendorId, @BlockDate, @StartTime, @EndTime, @IsAvailable, @IsHourly, @Label, @CreatedDate)";
                    command.Parameters.AddWithValue("@VendorId", block.VendorId);
                    command.Parameters.AddWithValue("@BlockDate", block.BlockDate.Date);
                    command.Parameters.AddWithValue("@StartTime", (object)block.StartTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EndTime", (object)block.EndTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IsAvailable", block.IsAvailable ? 1 : 0);
                    command.Parameters.AddWithValue("@IsHourly", block.IsHourly ? 1 : 0);
                    command.Parameters.AddWithValue("@Label", (object)block.Label ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedDate", block.CreatedDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateVendorCalendarBlock(VendorCalendarBlock block)
        {
            EnsureCalendarBlockSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE VendorCalendarBlocks
                        SET BlockDate = @BlockDate, StartTime = @StartTime, EndTime = @EndTime,
                            IsAvailable = @IsAvailable, IsHourly = @IsHourly, Label = @Label
                        WHERE Id = @Id AND VendorId = @VendorId";
                    command.Parameters.AddWithValue("@Id", block.Id);
                    command.Parameters.AddWithValue("@VendorId", block.VendorId);
                    command.Parameters.AddWithValue("@BlockDate", block.BlockDate.Date);
                    command.Parameters.AddWithValue("@StartTime", (object)block.StartTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@EndTime", (object)block.EndTime ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IsAvailable", block.IsAvailable ? 1 : 0);
                    command.Parameters.AddWithValue("@IsHourly", block.IsHourly ? 1 : 0);
                    command.Parameters.AddWithValue("@Label", (object)block.Label ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public VendorCalendarBlock GetVendorCalendarBlock(int blockId, string vendorId)
        {
            EnsureCalendarBlockSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, VendorId, BlockDate, StartTime, EndTime, IsAvailable, IsHourly, Label, CreatedDate
                        FROM VendorCalendarBlocks
                        WHERE Id = @Id AND VendorId = @VendorId";
                    command.Parameters.AddWithValue("@Id", blockId);
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new VendorCalendarBlock
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                VendorId = reader["VendorId"].ToString(),
                                BlockDate = Convert.ToDateTime(reader["BlockDate"]).Date,
                                StartTime = reader["StartTime"]?.ToString(),
                                EndTime = reader["EndTime"]?.ToString(),
                                IsAvailable = Convert.ToInt32(reader["IsAvailable"]) == 1,
                                IsHourly = reader["IsHourly"] != DBNull.Value && Convert.ToInt32(reader["IsHourly"]) == 1,
                                Label = reader["Label"]?.ToString(),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void DeleteVendorCalendarBlock(int blockId, string vendorId)
        {
            EnsureCalendarBlockSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM VendorCalendarBlocks WHERE Id = @Id AND VendorId = @VendorId";
                    command.Parameters.AddWithValue("@Id", blockId);
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<VendorScheduleEntry> GetVendorSchedule(string vendorId, DateTime fromDate, DateTime toDate)
        {
            var dayMap = new Dictionary<DateTime, VendorScheduleEntry>();

            void UpsertEntry(DateTime day, Action<VendorScheduleEntry> apply)
            {
                day = day.Date;
                if (!dayMap.TryGetValue(day, out var entry))
                {
                    entry = new VendorScheduleEntry { Date = day };
                    dayMap[day] = entry;
                }
                apply(entry);
            }

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT DATE(b.EventDate) AS EventDay,
                               DATE(COALESCE(b.EventEndDate, b.EventDate)) AS EventEndDay,
                               DATE_FORMAT(b.EventDate, '%Y-%m-%d') AS RangeStart,
                               DATE_FORMAT(COALESCE(b.EventEndDate, b.EventDate), '%Y-%m-%d') AS RangeEnd,
                               b.Status, 'Booking' AS Source,
                               COALESCE(c.Name, 'Customer') AS CustomerName,
                               COALESCE(c.Phone, '') AS CustomerPhone,
                               COALESCE(c.Email, '') AS CustomerEmail,
                               COALESCE(s.ServiceType, '') AS ServiceType,
                               '' AS EventType,
                               b.EventStartTime AS StartTime, b.EventEndTime AS EndTime,
                               b.PartyLocation, b.PartyPinCode,
                               b.CustomerTotalCost AS TotalCost,
                               b.Id AS RelatedId,
                               0 AS GuestCount,
                               CASE WHEN b.EventEndDate IS NOT NULL AND DATE(b.EventEndDate) > DATE(b.EventDate)
                                    THEN DATEDIFF(DATE(b.EventEndDate), DATE(b.EventDate)) + 1 ELSE 1 END AS DayCount,
                               b.AdvancePaid AS AdvancePaid, b.CustomerTotalCost AS CustomerTotalCost,
                               b.BalancePaidOnApp AS BalancePaidOnApp
                        FROM Bookings b
                        LEFT JOIN Customers c ON c.Id = b.CustomerId
                        LEFT JOIN Services s ON s.Id = b.ServiceId
                        WHERE b.VendorId = @VendorId
                          AND b.EventDate IS NOT NULL
                          AND DATE(b.EventDate) <= DATE(@ToDate)
                          AND DATE(COALESCE(b.EventEndDate, b.EventDate)) >= DATE(@FromDate)
                          AND b.Status NOT IN ('Cancelled', 'Rejected', 'Completed')
                        UNION ALL
                        SELECT DATE(sr.EventDate) AS EventDay,
                               DATE(COALESCE(sr.EventEndDate, sr.EventDate)) AS EventEndDay,
                               DATE_FORMAT(sr.EventDate, '%Y-%m-%d') AS RangeStart,
                               DATE_FORMAT(COALESCE(sr.EventEndDate, sr.EventDate), '%Y-%m-%d') AS RangeEnd,
                               sr.Status, 'Service Request' AS Source,
                               COALESCE(c.Name, 'Customer') AS CustomerName,
                               COALESCE(c.Phone, '') AS CustomerPhone,
                               COALESCE(c.Email, '') AS CustomerEmail,
                               COALESCE(s.ServiceType, '') AS ServiceType,
                               COALESCE(sr.EventType, '') AS EventType,
                               sr.EventStartTime AS StartTime, sr.EventEndTime AS EndTime,
                               sr.PartyLocation, sr.PartyPinCode,
                               sr.TotalCost AS TotalCost,
                               sr.Id AS RelatedId,
                               COALESCE(sr.GuestCount, 0) AS GuestCount,
                               COALESCE(sr.DayCount, 1) AS DayCount,
                               0 AS AdvancePaid, 0 AS CustomerTotalCost,
                               FALSE AS BalancePaidOnApp
                        FROM ServiceRequests sr
                        LEFT JOIN Customers c ON c.Id = sr.CustomerId
                        LEFT JOIN Services s ON s.Id = sr.ServiceId
                        WHERE sr.VendorId = @VendorId
                          AND sr.EventDate IS NOT NULL
                          AND DATE(sr.EventDate) <= DATE(@ToDate)
                          AND DATE(COALESCE(sr.EventEndDate, sr.EventDate)) >= DATE(@FromDate)
                          AND sr.Status NOT IN ('Rejected', 'Cancelled', 'Paid', 'Expired')";
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                    command.Parameters.AddWithValue("@ToDate", toDate.Date);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rangeStart = Convert.ToDateTime(reader["EventDay"]).Date;
                            var rangeEnd = reader["EventEndDay"] != DBNull.Value
                                ? Convert.ToDateTime(reader["EventEndDay"]).Date
                                : rangeStart;
                            var status = reader["Status"]?.ToString() ?? "";
                            var source = reader["Source"]?.ToString() ?? "";
                            var advancePaid = reader["AdvancePaid"] != DBNull.Value
                                ? Convert.ToDecimal(reader["AdvancePaid"])
                                : 0m;
                            var customerTotal = reader["CustomerTotalCost"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CustomerTotalCost"])
                                : 0m;
                            var balancePaidOnApp = reader["BalancePaidOnApp"] != DBNull.Value
                                && Convert.ToBoolean(reader["BalancePaidOnApp"]);
                            var isBooked = VendorScheduleHelper.IsScheduleBooked(
                                status, source, advancePaid, customerTotal, balancePaidOnApp);
                            var isUnderProcess = VendorScheduleHelper.IsScheduleUnderProcess(
                                status, source, advancePaid, customerTotal, balancePaidOnApp);

                            for (var day = rangeStart; day <= rangeEnd; day = day.AddDays(1))
                            {
                                if (day < fromDate.Date || day > toDate.Date) continue;

                                UpsertEntry(day, entry =>
                                {
                                    var startT = reader["StartTime"] != DBNull.Value
                                        ? FormatBookingTime(reader["StartTime"]) : null;
                                    var endT = reader["EndTime"] != DBNull.Value
                                        ? FormatBookingTime(reader["EndTime"]) : null;

                                    entry.Slots.Add(new VendorScheduleSlot
                                    {
                                        SlotType = isBooked ? "booked" : "pending",
                                        StartTime = startT,
                                        EndTime = endT,
                                        CustomerName = reader["CustomerName"]?.ToString() ?? "Customer",
                                        CustomerPhone = reader["CustomerPhone"]?.ToString() ?? "",
                                        CustomerEmail = reader["CustomerEmail"]?.ToString() ?? "",
                                        ServiceType = reader["ServiceType"]?.ToString() ?? "",
                                        EventType = reader["EventType"]?.ToString() ?? "",
                                        PartyLocation = reader["PartyLocation"] != DBNull.Value
                                            ? reader["PartyLocation"].ToString() : null,
                                        PartyPinCode = reader["PartyPinCode"] != DBNull.Value
                                            ? reader["PartyPinCode"].ToString() : null,
                                        Source = source,
                                        Status = isBooked ? status : "Under Process",
                                        RelatedId = reader["RelatedId"]?.ToString(),
                                        TotalCost = reader["TotalCost"] != DBNull.Value
                                            ? Convert.ToDecimal(reader["TotalCost"]) : (decimal?)null,
                                        GuestCount = reader["GuestCount"] != DBNull.Value
                                            ? Convert.ToInt32(reader["GuestCount"]) : (int?)null,
                                        Editable = false
                                    });

                                    void ApplyReaderFields()
                                    {
                                        entry.CustomerName = reader["CustomerName"]?.ToString() ?? "Customer";
                                        entry.CustomerPhone = reader["CustomerPhone"]?.ToString() ?? "";
                                        entry.CustomerEmail = reader["CustomerEmail"]?.ToString() ?? "";
                                        entry.ServiceType = reader["ServiceType"]?.ToString() ?? "";
                                        entry.EventType = reader["EventType"]?.ToString() ?? "";
                                        entry.StartTime = startT;
                                        entry.EndTime = endT;
                                        entry.PartyLocation = reader["PartyLocation"] != DBNull.Value
                                            ? reader["PartyLocation"].ToString() : null;
                                        entry.PartyPinCode = reader["PartyPinCode"] != DBNull.Value
                                            ? reader["PartyPinCode"].ToString() : null;
                                        entry.EventRangeStart = reader["RangeStart"]?.ToString();
                                        entry.EventRangeEnd = reader["RangeEnd"]?.ToString();
                                        entry.DayCount = reader["DayCount"] != DBNull.Value
                                            ? Convert.ToInt32(reader["DayCount"]) : (int?)null;
                                        entry.GuestCount = reader["GuestCount"] != DBNull.Value
                                            ? Convert.ToInt32(reader["GuestCount"]) : (int?)null;
                                        entry.TotalCost = reader["TotalCost"] != DBNull.Value
                                            ? Convert.ToDecimal(reader["TotalCost"]) : (decimal?)null;
                                        entry.RelatedId = reader["RelatedId"]?.ToString();
                                    }

                                    if (isBooked)
                                    {
                                        entry.IsBooked = true;
                                        entry.IsUnderProcess = false;
                                        entry.Source = source;
                                        entry.Status = status;
                                        ApplyReaderFields();
                                    }
                                    else if (isUnderProcess && !entry.IsBooked)
                                    {
                                        entry.IsUnderProcess = true;
                                        entry.Source = source;
                                        entry.Status = "Under Process";
                                        ApplyReaderFields();
                                    }
                                });
                            }
                        }
                    }
                }
            }

            foreach (var block in GetVendorCalendarBlocks(vendorId, fromDate, toDate).Where(b => !b.IsAvailable))
            {
                var blockLabel = string.IsNullOrWhiteSpace(block.Label) ? "PartyClap" : block.Label.Trim();
                var slotType = VendorScheduleHelper.ResolveManualBlockSlotType(blockLabel);
                UpsertEntry(block.BlockDate, entry =>
                {
                    entry.Slots.Add(new VendorScheduleSlot
                    {
                        BlockId = block.Id,
                        SlotType = slotType,
                        StartTime = block.StartTime,
                        EndTime = block.EndTime,
                        Label = blockLabel,
                        IsHourly = block.IsHourly,
                        Editable = true,
                        Source = "Manual Block",
                        Status = slotType == "partyclap" ? "PartyClap" : "Blocked"
                    });

                    if (!entry.IsBooked && !entry.IsUnderProcess)
                    {
                        entry.IsBooked = true;
                        entry.IsUnderProcess = false;
                        entry.Source = "Manual Block";
                        entry.Status = "Blocked";
                        entry.Label = blockLabel;
                        entry.StartTime = block.StartTime;
                        entry.EndTime = block.EndTime;
                    }
                });
            }

            for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
            {
                if (!dayMap.ContainsKey(day))
                {
                    dayMap[day] = new VendorScheduleEntry { Date = day, IsBooked = false, IsUnderProcess = false };
                }
            }

            var entries = dayMap.Values.ToList();
            VendorScheduleHelper.ApplySlotExpiryRules(entries);
            entries.Sort((a, b) => a.Date.CompareTo(b.Date));
            return entries;
        }

        public List<Booking> GetVendorBookings(string vendorId)
        {
            var bookings = new List<Booking>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetVendorBookings";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_VendorId", vendorId);
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
        
        // Get bookings with customer details for vendor dashboard
        public List<Dictionary<string, object>> GetVendorBookingsWithCustomerDetails(string vendorId)
        {
            var bookings = new List<Dictionary<string, object>>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    // Join with Customers and Services to get customer info and service type
                    command.CommandText = @"
                        SELECT b.*, 
                               c.Name as CustomerName, 
                               c.Email as CustomerEmail, 
                               c.Phone as CustomerPhone,
                               s.ServiceType
                        FROM Bookings b
                        LEFT JOIN Customers c ON b.CustomerId = c.Id
                        LEFT JOIN Services s ON b.ServiceId = s.Id
                        WHERE b.VendorId = @VendorId
                        ORDER BY b.BookingDate DESC";
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var bookingDict = new Dictionary<string, object>
                            {
                                ["Id"] = reader["Id"].ToString(),
                                ["CustomerId"] = reader["CustomerId"].ToString(),
                                ["VendorId"] = reader["VendorId"].ToString(),
                                ["ServiceId"] = reader["ServiceId"].ToString(),
                                ["BookingDate"] = Convert.ToDateTime(reader["BookingDate"]),
                                ["EventDate"] = Convert.ToDateTime(reader["EventDate"]),
                                ["VendorCost"] = Convert.ToDecimal(reader["VendorCost"]),
                                ["CustomerTotalCost"] = Convert.ToDecimal(reader["CustomerTotalCost"]),
                                ["AdvancePaid"] = Convert.ToDecimal(reader["AdvancePaid"]),
                                ["BalanceAmount"] = Convert.ToDecimal(reader["BalanceAmount"]),
                                ["Status"] = reader["Status"].ToString(),
                                ["BalancePaidOnApp"] = Convert.ToBoolean(reader["BalancePaidOnApp"]),
                                ["CustomerName"] = reader["CustomerName"]?.ToString() ?? "Unknown Customer",
                                ["CustomerEmail"] = reader["CustomerEmail"]?.ToString() ?? "",
                                ["CustomerPhone"] = reader["CustomerPhone"]?.ToString() ?? "",
                                ["ServiceType"] = reader["ServiceType"]?.ToString() ?? "",
                                ["PartyLocation"] = reader["PartyLocation"] != DBNull.Value ? reader["PartyLocation"].ToString() : "",
                                ["PartyPinCode"] = reader["PartyPinCode"] != DBNull.Value ? reader["PartyPinCode"].ToString() : "",
                                ["PartyLatitude"] = reader["PartyLatitude"] != DBNull.Value ? Convert.ToDecimal(reader["PartyLatitude"]) : (decimal?)null,
                                ["PartyLongitude"] = reader["PartyLongitude"] != DBNull.Value ? Convert.ToDecimal(reader["PartyLongitude"]) : (decimal?)null,
                                ["EventEndDate"] = reader["EventEndDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EventEndDate"]) : null,
                                ["EventStartTime"] = reader["EventStartTime"] != DBNull.Value ? FormatBookingTime(reader["EventStartTime"]) : null,
                                ["EventEndTime"] = reader["EventEndTime"] != DBNull.Value ? FormatBookingTime(reader["EventEndTime"]) : null
                            };
                            bookings.Add(bookingDict);
                        }
                    }
                }
            }
            return bookings;
        }
        
        public Booking GetBooking(string bookingId)
        {
            Booking booking = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Bookings WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", bookingId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            booking = new Booking
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
                                BalancePaidOnApp = Convert.ToBoolean(reader["BalancePaidOnApp"]),
                                PartyLocation = reader["PartyLocation"] != DBNull.Value ? reader["PartyLocation"].ToString() : null,
                                PartyPinCode = reader["PartyPinCode"] != DBNull.Value ? reader["PartyPinCode"].ToString() : null,
                                PartyLatitude = reader["PartyLatitude"] != DBNull.Value ? Convert.ToDecimal(reader["PartyLatitude"]) : (decimal?)null,
                                PartyLongitude = reader["PartyLongitude"] != DBNull.Value ? Convert.ToDecimal(reader["PartyLongitude"]) : (decimal?)null
                            };
                        }
                    }
                }
            }
            return booking;
        }
        
        public void AddBooking(Booking booking)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_AddBooking";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", booking.Id);
                    command.Parameters.AddWithValue("p_CustomerId", booking.CustomerId);
                    command.Parameters.AddWithValue("p_VendorId", booking.VendorId);
                    command.Parameters.AddWithValue("p_ServiceId", booking.ServiceId);
                    command.Parameters.AddWithValue("p_BookingDate", DateTime.Now);
                    command.Parameters.AddWithValue("p_EventDate", booking.EventDate);
                    command.Parameters.AddWithValue("p_VendorCost", booking.VendorCost);
                    command.Parameters.AddWithValue("p_CustomerTotalCost", booking.CustomerTotalCost);
                    command.Parameters.AddWithValue("p_AdvancePaid", booking.AdvancePaid);
                    command.Parameters.AddWithValue("p_BalanceAmount", booking.BalanceAmount);
                    command.Parameters.AddWithValue("p_Status", booking.Status);
                    command.Parameters.AddWithValue("p_BalancePaidOnApp", booking.BalancePaidOnApp);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public void UpdateBookingStatus(string bookingId, string status, decimal? vendorCost = null, decimal? customerTotalCost = null)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_UpdateBookingStatus";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", bookingId);
                    command.Parameters.AddWithValue("p_Status", status);
                    command.Parameters.AddWithValue("p_BalancePaidOnApp", false);
                    command.Parameters.AddWithValue("p_VendorCost", vendorCost ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("p_CustomerTotalCost", customerTotalCost ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        public void MarkAdvanceAsPaid(string bookingId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Bookings
                        SET AdvancePaid = CustomerTotalCost,
                            BalanceAmount = 0,
                            BalancePaidOnApp = TRUE,
                            Status = 'Confirmed'
                        WHERE Id = @BookingId";
                    command.Parameters.AddWithValue("@BookingId", bookingId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void MarkBalanceAsPaid(string bookingId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Bookings 
                        SET BalancePaidOnApp = TRUE,
                            Status = 'Confirmed'
                        WHERE Id = @BookingId";
                    command.Parameters.AddWithValue("@BookingId", bookingId);
                    command.ExecuteNonQuery();
                }
            }
        }
        public void UpdateVendor(Vendor vendor)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Vendors 
                        SET Name = @Name, 
                            Email = @Email, 
                            Phone = @Phone, 
                            ProfilePicture = @ProfilePicture,
                            Address = @Address, 
                            PinCode = @PinCode,
                            IsRegistered = @IsRegistered,
                            TrustScore = @TrustScore,
                            WalletBalance = @WalletBalance,
                            AccountHolderName = @AccountHolderName,
                            AccountNumber = @AccountNumber,
                            IfscCode = @IfscCode,
                            UpiId = @UpiId
                        WHERE Id = @Id";
                    
                    command.Parameters.AddWithValue("@Id", vendor.Id);
                    command.Parameters.AddWithValue("@Name", vendor.Name);
                    command.Parameters.AddWithValue("@Email", vendor.Email);
                    command.Parameters.AddWithValue("@Phone", vendor.Phone);
                    command.Parameters.AddWithValue("@ProfilePicture", vendor.ProfilePicture ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Address", vendor.Address ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PinCode", vendor.PinCode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IsRegistered", vendor.IsRegistered);
                    command.Parameters.AddWithValue("@TrustScore", vendor.TrustScore);
                    command.Parameters.AddWithValue("@WalletBalance", vendor.WalletBalance);
                    command.Parameters.AddWithValue("@AccountHolderName", vendor.AccountHolderName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AccountNumber", vendor.AccountNumber ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IfscCode", vendor.IfscCode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@UpiId", vendor.UpiId ?? (object)DBNull.Value);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Atomically debits the vendor's wallet for a payout request and records a
        /// Debit transaction. The balance check lives inside the UPDATE (WalletBalance >= @Amount)
        /// so a vendor can never withdraw more than they hold, even under concurrent requests.
        /// Returns false when the amount is invalid or exceeds the available balance.
        /// </summary>
        public bool RequestPayout(string vendorId, decimal amount, string description)
        {
            if (string.IsNullOrEmpty(vendorId) || amount <= 0) return false;

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var transaction = (MySqlTransaction)connection.BeginTransaction())
                {
                    try
                    {
                        int rows;
                        using (var command = (MySqlCommand)connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
                                UPDATE Vendors
                                SET WalletBalance = WalletBalance - @Amount
                                WHERE Id = @VendorId AND WalletBalance >= @Amount";
                            command.Parameters.AddWithValue("@Amount", amount);
                            command.Parameters.AddWithValue("@VendorId", vendorId);
                            rows = command.ExecuteNonQuery();
                        }

                        // No rows affected => insufficient balance (or vendor missing). Abort.
                        if (rows == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        using (var command = (MySqlCommand)connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandText = @"
                                INSERT INTO WalletTransactions (Id, VendorId, TransactionType, Amount, Description, TransactionDate)
                                VALUES (@Id, @VendorId, 'Debit', @Amount, @Description, NOW())";
                            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                            command.Parameters.AddWithValue("@VendorId", vendorId);
                            command.Parameters.AddWithValue("@Amount", amount);
                            command.Parameters.AddWithValue("@Description", description ?? "Payout request");
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public bool IsVendorServingPinCode(string vendorId, string partyPinCode)
        {
            if (string.IsNullOrWhiteSpace(vendorId) || string.IsNullOrWhiteSpace(partyPinCode))
            {
                return false;
            }

            var normalized = VendorRegistrationRules.NormalizePinCode(partyPinCode);
            if (string.IsNullOrEmpty(normalized) || normalized.Length != 6)
            {
                return false;
            }

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT COUNT(1)
                        FROM (
                            SELECT PinCode FROM Vendors WHERE Id = @VendorId
                            UNION
                            SELECT PinCode FROM VendorServiceLocations WHERE VendorId = @VendorId
                        ) served
                        WHERE PinCode = @PartyPinCode";
                    command.Parameters.AddWithValue("@VendorId", vendorId);
                    command.Parameters.AddWithValue("@PartyPinCode", normalized);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
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
    }
}
