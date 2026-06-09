using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class CartDAL
    {
        private readonly DBHelper _dbHelper;

        private static bool _schemaEnsured;
        private static readonly object _schemaLock = new object();

        public CartDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// Ensures cart schema exists and supports account-backed carts (B041).
        /// </summary>
        public void EnsureSchema()
        {
            if (_schemaEnsured) return;
            lock (_schemaLock)
            {
                if (_schemaEnsured) return;
                try
                {
                    using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                CREATE TABLE IF NOT EXISTS CartItems (
                                    Id INT AUTO_INCREMENT PRIMARY KEY,
                                    CookieId VARCHAR(64) NULL,
                                    CustomerId VARCHAR(36) NULL,
                                    ServiceId VARCHAR(36) NOT NULL,
                                    VendorId VARCHAR(36) NOT NULL,
                                    EventDate DATETIME NULL,
                                    EventStartTime VARCHAR(8) NULL,
                                    EventEndTime VARCHAR(8) NULL,
                                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    INDEX idx_cart_customer (CustomerId),
                                    INDEX idx_cart_cookie (CookieId),
                                    INDEX idx_cart_service (ServiceId)
                                );";
                            command.ExecuteNonQuery();
                        }

                        AddColumnIfMissing(connection, "CartItems", "CustomerId", "VARCHAR(36) NULL");
                        AddColumnIfMissing(connection, "CartItems", "EventStartTime", "VARCHAR(8) NULL");
                        AddColumnIfMissing(connection, "CartItems", "EventEndTime", "VARCHAR(8) NULL");
                        AddColumnIfMissing(connection, "CartItems", "CreatedAt", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");
                        AddColumnIfMissing(connection, "CartItems", "EventEndDate", "DATETIME NULL");
                        AddColumnIfMissing(connection, "Bookings", "EventStartTime", "VARCHAR(8) NULL");
                        AddColumnIfMissing(connection, "Bookings", "EventEndTime", "VARCHAR(8) NULL");
                        AddColumnIfMissing(connection, "Bookings", "EventEndDate", "DATETIME NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "EventEndDate", "DATETIME NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "EventStartTime", "VARCHAR(8) NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "EventEndTime", "VARCHAR(8) NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "DayCount", "INT NOT NULL DEFAULT 1");
                        AddColumnIfMissing(connection, "ServiceRequests", "TotalCost", "DECIMAL(12,2) NOT NULL DEFAULT 0");
                        AddColumnIfMissing(connection, "CartItems", "PartyLocation", "VARCHAR(500) NULL");
                        AddColumnIfMissing(connection, "CartItems", "PartyPinCode", "VARCHAR(10) NULL");
                        AddColumnIfMissing(connection, "CartItems", "PartyLatitude", "DECIMAL(10,7) NULL");
                        AddColumnIfMissing(connection, "CartItems", "PartyLongitude", "DECIMAL(10,7) NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "PartyLocation", "VARCHAR(500) NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "PartyPinCode", "VARCHAR(10) NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "PartyLatitude", "DECIMAL(10,7) NULL");
                        AddColumnIfMissing(connection, "ServiceRequests", "PartyLongitude", "DECIMAL(10,7) NULL");
                        AddColumnIfMissing(connection, "Bookings", "PartyLocation", "VARCHAR(500) NULL");
                        AddColumnIfMissing(connection, "Bookings", "PartyPinCode", "VARCHAR(10) NULL");
                        AddColumnIfMissing(connection, "Bookings", "PartyLatitude", "DECIMAL(10,7) NULL");
                        AddColumnIfMissing(connection, "Bookings", "PartyLongitude", "DECIMAL(10,7) NULL");
                    }
                    _schemaEnsured = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CartDAL.EnsureSchema: {ex.Message}");
                }
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

        public void AddToCart(string customerId, string cookieId, string serviceId, string vendorId, DateTime? eventDate,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null)
        {
            EnsureSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    using (var exists = (MySqlCommand)connection.CreateCommand())
                    {
                        exists.CommandText = @"
                            SELECT COUNT(1) FROM CartItems
                            WHERE CustomerId = @CustomerId AND ServiceId = @ServiceId";
                        exists.Parameters.AddWithValue("@CustomerId", customerId);
                        exists.Parameters.AddWithValue("@ServiceId", serviceId);
                        if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
                        {
                            using (var update = (MySqlCommand)connection.CreateCommand())
                            {
                                update.CommandText = @"
                                    UPDATE CartItems
                                    SET EventDate = @EventDate, VendorId = @VendorId,
                                        PartyLocation = @PartyLocation, PartyPinCode = @PartyPinCode,
                                        PartyLatitude = @PartyLatitude, PartyLongitude = @PartyLongitude
                                    WHERE CustomerId = @CustomerId AND ServiceId = @ServiceId";
                                update.Parameters.AddWithValue("@EventDate", eventDate ?? (object)DBNull.Value);
                                update.Parameters.AddWithValue("@VendorId", vendorId);
                                update.Parameters.AddWithValue("@CustomerId", customerId);
                                update.Parameters.AddWithValue("@ServiceId", serviceId);
                                update.Parameters.AddWithValue("@PartyLocation", string.IsNullOrWhiteSpace(partyLocation) ? (object)DBNull.Value : partyLocation.Trim());
                                update.Parameters.AddWithValue("@PartyPinCode", string.IsNullOrWhiteSpace(partyPinCode) ? (object)DBNull.Value : partyPinCode.Trim());
                                update.Parameters.AddWithValue("@PartyLatitude", partyLatitude ?? (object)DBNull.Value);
                                update.Parameters.AddWithValue("@PartyLongitude", partyLongitude ?? (object)DBNull.Value);
                                update.ExecuteNonQuery();
                            }
                            return;
                        }
                    }

                    using (var insert = (MySqlCommand)connection.CreateCommand())
                    {
                        insert.CommandText = @"
                            INSERT INTO CartItems (CookieId, CustomerId, ServiceId, VendorId, EventDate, PartyLocation, PartyPinCode, PartyLatitude, PartyLongitude)
                            VALUES (NULL, @CustomerId, @ServiceId, @VendorId, @EventDate, @PartyLocation, @PartyPinCode, @PartyLatitude, @PartyLongitude)";
                        insert.Parameters.AddWithValue("@CustomerId", customerId);
                        insert.Parameters.AddWithValue("@ServiceId", serviceId);
                        insert.Parameters.AddWithValue("@VendorId", vendorId);
                        insert.Parameters.AddWithValue("@EventDate", eventDate ?? (object)DBNull.Value);
                        insert.Parameters.AddWithValue("@PartyLocation", string.IsNullOrWhiteSpace(partyLocation) ? (object)DBNull.Value : partyLocation.Trim());
                        insert.Parameters.AddWithValue("@PartyPinCode", string.IsNullOrWhiteSpace(partyPinCode) ? (object)DBNull.Value : partyPinCode.Trim());
                        insert.Parameters.AddWithValue("@PartyLatitude", partyLatitude ?? (object)DBNull.Value);
                        insert.Parameters.AddWithValue("@PartyLongitude", partyLongitude ?? (object)DBNull.Value);
                        insert.ExecuteNonQuery();
                    }
                    return;
                }

                if (string.IsNullOrWhiteSpace(cookieId))
                {
                    return;
                }

                using (var insert = (MySqlCommand)connection.CreateCommand())
                {
                    insert.CommandText = @"
                        INSERT INTO CartItems (CookieId, CustomerId, ServiceId, VendorId, EventDate, PartyLocation, PartyPinCode, PartyLatitude, PartyLongitude)
                        SELECT @CookieId, NULL, @ServiceId, @VendorId, @EventDate, @PartyLocation, @PartyPinCode, @PartyLatitude, @PartyLongitude
                        FROM DUAL
                        WHERE NOT EXISTS (
                            SELECT 1 FROM CartItems
                            WHERE CookieId = @CookieId AND ServiceId = @ServiceId AND CustomerId IS NULL
                        );";
                    insert.Parameters.AddWithValue("@CookieId", cookieId);
                    insert.Parameters.AddWithValue("@ServiceId", serviceId);
                    insert.Parameters.AddWithValue("@VendorId", vendorId);
                    insert.Parameters.AddWithValue("@EventDate", eventDate ?? (object)DBNull.Value);
                    insert.Parameters.AddWithValue("@PartyLocation", string.IsNullOrWhiteSpace(partyLocation) ? (object)DBNull.Value : partyLocation.Trim());
                    insert.Parameters.AddWithValue("@PartyPinCode", string.IsNullOrWhiteSpace(partyPinCode) ? (object)DBNull.Value : partyPinCode.Trim());
                    insert.Parameters.AddWithValue("@PartyLatitude", partyLatitude ?? (object)DBNull.Value);
                    insert.Parameters.AddWithValue("@PartyLongitude", partyLongitude ?? (object)DBNull.Value);
                    insert.ExecuteNonQuery();
                }
            }
        }

        public List<CartItem> GetCartItems(string customerId, string cookieId)
        {
            EnsureSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    if (!string.IsNullOrWhiteSpace(customerId))
                    {
                        command.CommandText = BuildCartSelectSql("ci.CustomerId = @OwnerId");
                        command.Parameters.AddWithValue("@OwnerId", customerId);
                    }
                    else
                    {
                        command.CommandText = BuildCartSelectSql("ci.CookieId = @OwnerId AND ci.CustomerId IS NULL");
                        command.Parameters.AddWithValue("@OwnerId", cookieId ?? string.Empty);
                    }

                    return ReadCartItems(command);
                }
            }
        }

        public void MergeGuestCart(string customerId, string cookieId)
        {
            if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(cookieId))
            {
                return;
            }

            EnsureSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();

                var guestItems = new List<(int Id, string ServiceId, string VendorId, DateTime? EventDate)>();
                using (var select = (MySqlCommand)connection.CreateCommand())
                {
                    select.CommandText = @"
                        SELECT Id, ServiceId, VendorId, EventDate
                        FROM CartItems
                        WHERE CookieId = @CookieId AND CustomerId IS NULL";
                    select.Parameters.AddWithValue("@CookieId", cookieId);
                    using (var reader = select.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            guestItems.Add((
                                Convert.ToInt32(reader["Id"]),
                                reader["ServiceId"].ToString(),
                                reader["VendorId"].ToString(),
                                reader["EventDate"] != DBNull.Value ? Convert.ToDateTime(reader["EventDate"]) : (DateTime?)null));
                        }
                    }
                }

                foreach (var guestItem in guestItems)
                {
                    using (var exists = (MySqlCommand)connection.CreateCommand())
                    {
                        exists.CommandText = @"
                            SELECT COUNT(1) FROM CartItems
                            WHERE CustomerId = @CustomerId AND ServiceId = @ServiceId";
                        exists.Parameters.AddWithValue("@CustomerId", customerId);
                        exists.Parameters.AddWithValue("@ServiceId", guestItem.ServiceId);
                        var alreadyOwned = Convert.ToInt32(exists.ExecuteScalar()) > 0;
                        if (alreadyOwned)
                        {
                            using (var delete = (MySqlCommand)connection.CreateCommand())
                            {
                                delete.CommandText = "DELETE FROM CartItems WHERE Id = @Id";
                                delete.Parameters.AddWithValue("@Id", guestItem.Id);
                                delete.ExecuteNonQuery();
                            }
                            continue;
                        }
                    }

                    using (var move = (MySqlCommand)connection.CreateCommand())
                    {
                        move.CommandText = @"
                            UPDATE CartItems
                            SET CustomerId = @CustomerId, CookieId = NULL
                            WHERE Id = @Id";
                        move.Parameters.AddWithValue("@CustomerId", customerId);
                        move.Parameters.AddWithValue("@Id", guestItem.Id);
                        move.ExecuteNonQuery();
                    }
                }
            }
        }

        private static string BuildCartSelectSql(string ownerPredicate)
        {
            return $@"
                SELECT ci.Id, ci.CookieId, ci.CustomerId, ci.ServiceId, ci.VendorId, ci.EventDate,
                       ci.EventEndDate, ci.EventStartTime, ci.EventEndTime,
                       ci.PartyLocation, ci.PartyPinCode, ci.PartyLatitude, ci.PartyLongitude,
                       s.ServiceType, s.Cost, s.WeekendCost, s.Unit, s.MediaUrl,
                       v.Name AS VendorName
                FROM CartItems ci
                INNER JOIN Services s ON ci.ServiceId = s.Id
                INNER JOIN Vendors v ON ci.VendorId = v.Id
                WHERE {ownerPredicate}
                ORDER BY ci.Id";
        }

        private static List<CartItem> ReadCartItems(MySqlCommand command)
        {
            var items = new List<CartItem>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new CartItem
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CookieId = reader["CookieId"] != DBNull.Value ? reader["CookieId"].ToString() : null,
                        CustomerId = reader["CustomerId"] != DBNull.Value ? reader["CustomerId"].ToString() : null,
                        ServiceId = reader["ServiceId"].ToString(),
                        VendorId = reader["VendorId"].ToString(),
                        ServiceType = reader["ServiceType"].ToString(),
                        VendorName = reader["VendorName"].ToString(),
                        Cost = Convert.ToDecimal(reader["Cost"]),
                        WeekendCost = reader["WeekendCost"] != DBNull.Value ? Convert.ToDecimal(reader["WeekendCost"]) : (decimal?)null,
                        EventDate = reader["EventDate"] != DBNull.Value ? Convert.ToDateTime(reader["EventDate"]) : (DateTime?)null,
                        EventEndDate = reader["EventEndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EventEndDate"]) : (DateTime?)null,
                        EventStartTime = reader["EventStartTime"] != DBNull.Value ? FormatTime(reader["EventStartTime"]) : null,
                        EventEndTime = reader["EventEndTime"] != DBNull.Value ? FormatTime(reader["EventEndTime"]) : null,
                        Unit = reader["Unit"] != DBNull.Value ? reader["Unit"].ToString() : "Event",
                        MediaUrl = reader["MediaUrl"] != DBNull.Value ? reader["MediaUrl"].ToString() : null,
                        PartyLocation = reader["PartyLocation"] != DBNull.Value ? reader["PartyLocation"].ToString() : null,
                        PartyPinCode = reader["PartyPinCode"] != DBNull.Value ? reader["PartyPinCode"].ToString() : null,
                        PartyLatitude = reader["PartyLatitude"] != DBNull.Value ? Convert.ToDecimal(reader["PartyLatitude"]) : (decimal?)null,
                        PartyLongitude = reader["PartyLongitude"] != DBNull.Value ? Convert.ToDecimal(reader["PartyLongitude"]) : (decimal?)null
                    });
                }
            }

            return items;
        }

        public void RemoveFromCart(int cartItemId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM CartItems WHERE Id = @CartItemId";
                    command.Parameters.AddWithValue("@CartItemId", cartItemId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ClearCart(string customerId, string cookieId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    if (!string.IsNullOrWhiteSpace(customerId))
                    {
                        command.CommandText = "DELETE FROM CartItems WHERE CustomerId = @CustomerId";
                        command.Parameters.AddWithValue("@CustomerId", customerId);
                    }
                    else
                    {
                        command.CommandText = "DELETE FROM CartItems WHERE CookieId = @CookieId AND CustomerId IS NULL";
                        command.Parameters.AddWithValue("@CookieId", cookieId ?? string.Empty);
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateCartItemDate(int cartItemId, DateTime? eventDate)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "UPDATE CartItems SET EventDate = @EventDate WHERE Id = @CartItemId";
                    command.Parameters.AddWithValue("@CartItemId", cartItemId);
                    command.Parameters.AddWithValue("@EventDate", eventDate ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Updates a cart item's event date range plus its start and end time.</summary>
        public void UpdateCartItemSchedule(int cartItemId, DateTime? eventDate, DateTime? eventEndDate, string startTime, string endTime,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null)
        {
            EnsureSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"UPDATE CartItems
                                            SET EventDate = @EventDate,
                                                EventEndDate = @EventEndDate,
                                                EventStartTime = @StartTime,
                                                EventEndTime = @EndTime,
                                                PartyLocation = @PartyLocation,
                                                PartyPinCode = @PartyPinCode,
                                                PartyLatitude = @PartyLatitude,
                                                PartyLongitude = @PartyLongitude
                                            WHERE Id = @CartItemId";
                    command.Parameters.AddWithValue("@CartItemId", cartItemId);
                    command.Parameters.AddWithValue("@EventDate", eventDate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EventEndDate", eventEndDate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@StartTime", string.IsNullOrWhiteSpace(startTime) ? (object)DBNull.Value : startTime);
                    command.Parameters.AddWithValue("@EndTime", string.IsNullOrWhiteSpace(endTime) ? (object)DBNull.Value : endTime);
                    command.Parameters.AddWithValue("@PartyLocation", string.IsNullOrWhiteSpace(partyLocation) ? (object)DBNull.Value : partyLocation.Trim());
                    command.Parameters.AddWithValue("@PartyPinCode", string.IsNullOrWhiteSpace(partyPinCode) ? (object)DBNull.Value : partyPinCode.Trim());
                    command.Parameters.AddWithValue("@PartyLatitude", partyLatitude ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PartyLongitude", partyLongitude ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Persists the event schedule and end date onto a created booking row.</summary>
        public void UpdateBookingSchedule(string bookingId, string startTime, string endTime, DateTime? eventEndDate = null,
            string partyLocation = null, string partyPinCode = null, decimal? partyLatitude = null, decimal? partyLongitude = null)
        {
            if (string.IsNullOrWhiteSpace(bookingId)) return;
            EnsureSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"UPDATE Bookings
                                            SET EventStartTime = @StartTime,
                                                EventEndTime = @EndTime,
                                                EventEndDate = @EventEndDate,
                                                PartyLocation = @PartyLocation,
                                                PartyPinCode = @PartyPinCode,
                                                PartyLatitude = @PartyLatitude,
                                                PartyLongitude = @PartyLongitude
                                            WHERE Id = @BookingId";
                    command.Parameters.AddWithValue("@BookingId", bookingId);
                    command.Parameters.AddWithValue("@StartTime", string.IsNullOrWhiteSpace(startTime) ? (object)DBNull.Value : startTime);
                    command.Parameters.AddWithValue("@EndTime", string.IsNullOrWhiteSpace(endTime) ? (object)DBNull.Value : endTime);
                    command.Parameters.AddWithValue("@EventEndDate", eventEndDate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PartyLocation", string.IsNullOrWhiteSpace(partyLocation) ? (object)DBNull.Value : partyLocation.Trim());
                    command.Parameters.AddWithValue("@PartyPinCode", string.IsNullOrWhiteSpace(partyPinCode) ? (object)DBNull.Value : partyPinCode.Trim());
                    command.Parameters.AddWithValue("@PartyLatitude", partyLatitude ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PartyLongitude", partyLongitude ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Normalises a stored time value to "HH:mm" regardless of whether the
        // driver returns it as a string or a TimeSpan.
        private static string FormatTime(object value)
        {
            if (value is TimeSpan ts) return ts.ToString(@"hh\:mm");
            var s = value.ToString();
            if (s.Length >= 5) return s.Substring(0, 5);
            return s;
        }
    }
}
