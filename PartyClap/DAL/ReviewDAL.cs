using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class ReviewDAL
    {
        private readonly DBHelper _dbHelper;

        public ReviewDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public void AddReview(Review review)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_AddReview";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", review.Id);
                    command.Parameters.AddWithValue("p_BookingId", review.BookingId);
                    command.Parameters.AddWithValue("p_CustomerId", review.CustomerId);
                    command.Parameters.AddWithValue("p_VendorId", review.VendorId);
                    command.Parameters.AddWithValue("p_ServiceId", review.ServiceId);
                    command.Parameters.AddWithValue("p_Rating", review.Rating);
                    command.Parameters.AddWithValue("p_Comment", review.Comment ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Review> GetServiceReviews(string serviceId)
        {
            var reviews = new List<Review>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetServiceReviews";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_ServiceId", serviceId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviews.Add(new Review
                            {
                                Id = reader["Id"].ToString(),
                                BookingId = reader["BookingId"].ToString(),
                                CustomerId = reader["CustomerId"].ToString(),
                                VendorId = reader["VendorId"].ToString(),
                                ServiceId = reader["ServiceId"].ToString(),
                                Rating = Convert.ToInt32(reader["Rating"]),
                                Comment = reader["Comment"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                CustomerName = reader["CustomerName"].ToString()
                            });
                        }
                    }
                }
            }
            return reviews;
        }

        public List<Review> GetVendorReviews(string vendorId)
        {
            var reviews = new List<Review>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetVendorReviews";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_VendorId", vendorId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviews.Add(new Review
                            {
                                Id = reader["Id"].ToString(),
                                BookingId = reader["BookingId"].ToString(),
                                CustomerId = reader["CustomerId"].ToString(),
                                VendorId = reader["VendorId"].ToString(),
                                ServiceId = reader["ServiceId"].ToString(),
                                Rating = Convert.ToInt32(reader["Rating"]),
                                Comment = reader["Comment"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                CustomerName = reader["CustomerName"].ToString(),
                                ServiceName = reader["ServiceName"].ToString()
                            });
                        }
                    }
                }
            }
            return reviews;
        }
    }
}
