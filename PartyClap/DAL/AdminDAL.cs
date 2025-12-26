using System;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;

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
    }
}
