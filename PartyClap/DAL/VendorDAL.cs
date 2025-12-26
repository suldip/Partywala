using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using PartyClap.Models;

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
                                WalletBalance = Convert.ToDecimal(reader["WalletBalance"])
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
                                WalletBalance = Convert.ToDecimal(reader["WalletBalance"])
                            };
                        }
                    }
                }
            }
            return vendor;
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
                                WalletBalance = Convert.ToDecimal(reader["WalletBalance"])
                            };
                        }
                    }
                }
            }
            return vendor;
        }

        public void AddService(ServiceListing service)
        {
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
                                ["ServiceType"] = reader["ServiceType"]?.ToString() ?? ""
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
                                VendorCost = Convert.ToDecimal(reader["VendorCost"]),
                                CustomerTotalCost = Convert.ToDecimal(reader["CustomerTotalCost"]),
                                AdvancePaid = Convert.ToDecimal(reader["AdvancePaid"]),
                                BalanceAmount = Convert.ToDecimal(reader["BalanceAmount"]),
                                Status = reader["Status"].ToString(),
                                BalancePaidOnApp = Convert.ToBoolean(reader["BalancePaidOnApp"])
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
                            Address = @Address, 
                            PinCode = @PinCode,
                            TrustScore = @TrustScore,
                            WalletBalance = @WalletBalance
                        WHERE Id = @Id";
                    
                    command.Parameters.AddWithValue("@Id", vendor.Id);
                    command.Parameters.AddWithValue("@Name", vendor.Name);
                    command.Parameters.AddWithValue("@Email", vendor.Email);
                    command.Parameters.AddWithValue("@Phone", vendor.Phone);
                    command.Parameters.AddWithValue("@Address", vendor.Address);
                    command.Parameters.AddWithValue("@PinCode", vendor.PinCode);
                    command.Parameters.AddWithValue("@TrustScore", vendor.TrustScore);
                    command.Parameters.AddWithValue("@WalletBalance", vendor.WalletBalance);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
