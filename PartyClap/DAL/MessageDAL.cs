using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class MessageDAL
    {
        private readonly DBHelper _dbHelper;

        public MessageDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public void SendMessage(Message message)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_SendMessage";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Id", message.Id);
                    command.Parameters.AddWithValue("p_SenderId", message.SenderId);
                    command.Parameters.AddWithValue("p_ReceiverId", message.ReceiverId);
                    command.Parameters.AddWithValue("p_SenderRole", message.SenderRole);
                    command.Parameters.AddWithValue("p_Content", message.Content);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Message> GetChatHistory(string user1Id, string user2Id)
        {
            var messages = new List<Message>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetChatHistory";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_User1Id", user1Id);
                    command.Parameters.AddWithValue("p_User2Id", user2Id);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new Message
                            {
                                Id = reader["Id"].ToString(),
                                SenderId = reader["SenderId"].ToString(),
                                ReceiverId = reader["ReceiverId"].ToString(),
                                SenderRole = reader["SenderRole"].ToString(),
                                Content = reader["Content"].ToString(),
                                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                IsRead = Convert.ToBoolean(reader["IsRead"])
                            });
                        }
                    }
                }
            }
            return messages;
        }
    }
}
