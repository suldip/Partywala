using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class ReviewDAL
    {
        private readonly DBHelper _dbHelper;

        private static bool _schemaEnsured;
        private static readonly object _schemaLock = new object();

        public ReviewDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public void EnsureSchema()
        {
            if (_schemaEnsured) return;
            lock (_schemaLock)
            {
                if (_schemaEnsured) return;
                using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Reviews (
                                Id VARCHAR(36) NOT NULL PRIMARY KEY,
                                BookingId VARCHAR(36) NOT NULL,
                                CustomerId VARCHAR(36) NOT NULL,
                                VendorId VARCHAR(36) NOT NULL,
                                ServiceId VARCHAR(36) NOT NULL,
                                Rating INT NOT NULL,
                                Comment TEXT NULL,
                                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                UNIQUE KEY uk_reviews_booking (BookingId),
                                INDEX idx_reviews_vendor (VendorId),
                                INDEX idx_reviews_service (ServiceId)
                            );";
                        command.ExecuteNonQuery();
                    }
                }
                _schemaEnsured = true;
            }
        }

        public void AddReview(Review review)
        {
            EnsureSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Reviews (Id, BookingId, CustomerId, VendorId, ServiceId, Rating, Comment, CreatedAt)
                        VALUES (@Id, @BookingId, @CustomerId, @VendorId, @ServiceId, @Rating, @Comment, @CreatedAt);";
                    command.Parameters.AddWithValue("@Id", review.Id);
                    command.Parameters.AddWithValue("@BookingId", review.BookingId);
                    command.Parameters.AddWithValue("@CustomerId", review.CustomerId);
                    command.Parameters.AddWithValue("@VendorId", review.VendorId);
                    command.Parameters.AddWithValue("@ServiceId", review.ServiceId);
                    command.Parameters.AddWithValue("@Rating", review.Rating);
                    command.Parameters.AddWithValue("@Comment", (object)review.Comment ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedAt", review.CreatedAt == default ? DateTime.Now : review.CreatedAt);
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool HasReviewForBooking(string bookingId)
        {
            EnsureSchema();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(1) FROM Reviews WHERE BookingId = @BookingId";
                    command.Parameters.AddWithValue("@BookingId", bookingId);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        public HashSet<string> GetReviewedBookingIds(string customerId)
        {
            EnsureSchema();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT BookingId FROM Reviews WHERE CustomerId = @CustomerId";
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ids.Add(reader["BookingId"].ToString());
                        }
                    }
                }
            }
            return ids;
        }

        public List<Review> GetServiceReviews(string serviceId)
        {
            EnsureSchema();
            return QueryReviews("WHERE r.ServiceId = @EntityId", "EntityId", serviceId);
        }

        public List<Review> GetVendorReviews(string vendorId)
        {
            EnsureSchema();
            return QueryReviews("WHERE r.VendorId = @EntityId", "EntityId", vendorId);
        }

        public ReviewSummary GetVendorReviewSummary(string vendorId)
        {
            EnsureSchema();
            return QuerySummary("VendorId", vendorId, vendorId, null);
        }

        public Dictionary<string, ReviewSummary> GetServiceReviewSummaries(IEnumerable<string> serviceIds)
        {
            EnsureSchema();
            var ids = serviceIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList()
                ?? new List<string>();
            var summaries = new Dictionary<string, ReviewSummary>(StringComparer.OrdinalIgnoreCase);
            if (ids.Count == 0) return summaries;

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                var paramNames = new List<string>();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    for (var i = 0; i < ids.Count; i++)
                    {
                        var param = $"@ServiceId{i}";
                        paramNames.Add(param);
                        command.Parameters.AddWithValue(param, ids[i]);
                    }

                    command.CommandText = $@"
                        SELECT ServiceId, AVG(Rating) AS AverageRating, COUNT(*) AS ReviewCount
                        FROM Reviews
                        WHERE ServiceId IN ({string.Join(", ", paramNames)})
                        GROUP BY ServiceId";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var serviceId = reader["ServiceId"].ToString();
                            summaries[serviceId] = new ReviewSummary
                            {
                                ServiceId = serviceId,
                                AverageRating = reader["AverageRating"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDouble(reader["AverageRating"]),
                                ReviewCount = Convert.ToInt32(reader["ReviewCount"])
                            };
                        }
                    }
                }
            }

            return summaries;
        }

        private ReviewSummary QuerySummary(string column, string entityId, string vendorId, string serviceId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = $@"
                        SELECT AVG(Rating) AS AverageRating, COUNT(*) AS ReviewCount
                        FROM Reviews
                        WHERE {column} = @EntityId";
                    command.Parameters.AddWithValue("@EntityId", entityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new ReviewSummary
                            {
                                VendorId = vendorId,
                                ServiceId = serviceId,
                                AverageRating = reader["AverageRating"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDouble(reader["AverageRating"]),
                                ReviewCount = Convert.ToInt32(reader["ReviewCount"])
                            };
                        }
                    }
                }
            }

            return new ReviewSummary
            {
                VendorId = vendorId,
                ServiceId = serviceId,
                AverageRating = 0,
                ReviewCount = 0
            };
        }

        private List<Review> QueryReviews(string whereClause, string paramName, string paramValue)
        {
            var reviews = new List<Review>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = $@"
                        SELECT r.Id, r.BookingId, r.CustomerId, r.VendorId, r.ServiceId, r.Rating, r.Comment, r.CreatedAt,
                               c.Name AS CustomerName, s.ServiceType AS ServiceName
                        FROM Reviews r
                        LEFT JOIN Customers c ON c.Id = r.CustomerId
                        LEFT JOIN Services s ON s.Id = r.ServiceId
                        {whereClause}
                        ORDER BY r.CreatedAt DESC";
                    command.Parameters.AddWithValue($"@{paramName}", paramValue);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviews.Add(MapReview(reader));
                        }
                    }
                }
            }
            return reviews;
        }

        private static Review MapReview(IDataReader reader)
        {
            return new Review
            {
                Id = reader["Id"].ToString(),
                BookingId = reader["BookingId"].ToString(),
                CustomerId = reader["CustomerId"].ToString(),
                VendorId = reader["VendorId"].ToString(),
                ServiceId = reader["ServiceId"].ToString(),
                Rating = Convert.ToInt32(reader["Rating"]),
                Comment = reader["Comment"] == DBNull.Value ? null : reader["Comment"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                CustomerName = reader["CustomerName"] == DBNull.Value ? "Customer" : reader["CustomerName"].ToString(),
                ServiceName = reader["ServiceName"] == DBNull.Value ? null : reader["ServiceName"].ToString()
            };
        }
    }
}
