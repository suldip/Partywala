using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class CartDAL
    {
        private readonly DBHelper _dbHelper;

        public CartDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public void AddToCart(string cookieId, string serviceId, string vendorId, DateTime? eventDate)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_AddToCart";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_CookieId", cookieId);
                    command.Parameters.AddWithValue("p_ServiceId", serviceId);
                    command.Parameters.AddWithValue("p_VendorId", vendorId);
                    command.Parameters.AddWithValue("p_EventDate", eventDate ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<CartItem> GetCartItems(string cookieId)
        {
            var items = new List<CartItem>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetCartItems";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_CookieId", cookieId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new CartItem
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                CookieId = reader["CookieId"].ToString(),
                                ServiceId = reader["ServiceId"].ToString(),
                                VendorId = reader["VendorId"].ToString(),
                                ServiceType = reader["ServiceType"].ToString(),
                                VendorName = reader["VendorName"].ToString(),
                                Cost = Convert.ToDecimal(reader["Cost"]),
                                WeekendCost = reader["WeekendCost"] != DBNull.Value ? Convert.ToDecimal(reader["WeekendCost"]) : (decimal?)null,
                                EventDate = reader["EventDate"] != DBNull.Value ? Convert.ToDateTime(reader["EventDate"]) : (DateTime?)null,
                                Unit = reader.HasColumn("Unit") && reader["Unit"] != DBNull.Value ? reader["Unit"].ToString() : "Event",
                                MediaUrl = reader.HasColumn("MediaUrl") && reader["MediaUrl"] != DBNull.Value ? reader["MediaUrl"].ToString() : null
                            });
                        }
                    }
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
                    command.CommandText = "sp_RemoveFromCart";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_CartItemId", cartItemId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ClearCart(string cookieId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_ClearCart";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_CookieId", cookieId);
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
    }
}
