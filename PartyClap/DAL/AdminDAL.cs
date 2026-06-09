using System;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;
using System.Collections.Generic;

namespace PartyClap.DAL
{
    public class AdminDAL
    {
        private readonly DBHelper _dbHelper;

        public AdminDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public Admin GetAdminByEmail(string email)
        {
            Admin admin = null;
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetAdminByEmail";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Email", email);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            admin = new Admin
                            {
                                Id = reader["Id"].ToString(),
                                Email = reader["Email"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString()
                            };
                        }
                    }
                }
            }
            return admin;
        }

        public int GetAdminCount()
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Admins";
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void UpdateAdminPasswordHash(string adminId, string passwordHash)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Admins SET PasswordHash = @PasswordHash WHERE Id = @Id";
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Id", adminId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void RegisterAdmin(Admin admin)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_RegisterAdmin";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", admin.Id);
                    command.Parameters.AddWithValue("p_Email", admin.Email);
                    command.Parameters.AddWithValue("p_PasswordHash", admin.PasswordHash);
                    command.ExecuteNonQuery();
                }
            }
        }
        public List<AllowedPinCode> GetAllowedPinCodes()
        {
            var codes = new List<AllowedPinCode>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM AllowedPinCodes ORDER BY PinCode";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            codes.Add(new AllowedPinCode { 
                                PinCode = reader["PinCode"].ToString(),
                                CityName = reader["CityName"]?.ToString() 
                            });
                        }
                    }
                }
            }
            return codes;
        }

        public void AddAllowedPinCode(string pinCode, string cityName)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO AllowedPinCodes (PinCode, CityName) VALUES (@PinCode, @CityName) ON DUPLICATE KEY UPDATE CityName = @CityName";
                    command.Parameters.AddWithValue("@PinCode", pinCode);
                    command.Parameters.AddWithValue("@CityName", cityName);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteAllowedPinCode(string pinCode)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM AllowedPinCodes WHERE PinCode = @PinCode";
                    command.Parameters.AddWithValue("@PinCode", pinCode);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
