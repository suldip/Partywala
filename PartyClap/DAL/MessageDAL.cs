using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class MessageDAL
    {
        private readonly DBHelper _dbHelper;

        private static bool _schemaEnsured;
        private static readonly object _schemaLock = new object();

        public MessageDAL(DBHelper dbHelper)
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
                            CREATE TABLE IF NOT EXISTS Messages (
                                Id VARCHAR(36) NOT NULL PRIMARY KEY,
                                SenderId VARCHAR(36) NOT NULL,
                                ReceiverId VARCHAR(36) NOT NULL,
                                SenderRole VARCHAR(20) NOT NULL,
                                Content TEXT NOT NULL,
                                Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                IsRead TINYINT(1) NOT NULL DEFAULT 0,
                                INDEX idx_messages_sender (SenderId),
                                INDEX idx_messages_receiver (ReceiverId),
                                INDEX idx_messages_timestamp (Timestamp)
                            );";
                        command.ExecuteNonQuery();
                    }
                }
                _schemaEnsured = true;
            }
        }

        public void SendMessage(Message message)
        {
            EnsureSchema();
            if (string.IsNullOrWhiteSpace(message.Id))
            {
                message.Id = Guid.NewGuid().ToString();
            }

            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Messages (Id, SenderId, ReceiverId, SenderRole, Content, Timestamp, IsRead)
                        VALUES (@Id, @SenderId, @ReceiverId, @SenderRole, @Content, @Timestamp, @IsRead);";
                    command.Parameters.AddWithValue("@Id", message.Id);
                    command.Parameters.AddWithValue("@SenderId", message.SenderId);
                    command.Parameters.AddWithValue("@ReceiverId", message.ReceiverId);
                    command.Parameters.AddWithValue("@SenderRole", message.SenderRole ?? "Customer");
                    command.Parameters.AddWithValue("@Content", message.Content);
                    command.Parameters.AddWithValue("@Timestamp", message.Timestamp == default ? DateTime.Now : message.Timestamp);
                    command.Parameters.AddWithValue("@IsRead", message.IsRead);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Message> GetChatHistory(string user1Id, string user2Id)
        {
            EnsureSchema();
            var messages = new List<Message>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, SenderId, ReceiverId, SenderRole, Content, Timestamp, IsRead
                        FROM Messages
                        WHERE (SenderId = @User1Id AND ReceiverId = @User2Id)
                           OR (SenderId = @User2Id AND ReceiverId = @User1Id)
                        ORDER BY Timestamp ASC;";
                    command.Parameters.AddWithValue("@User1Id", user1Id);
                    command.Parameters.AddWithValue("@User2Id", user2Id);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(MapMessage(reader));
                        }
                    }
                }
            }

            MarkConversationRead(user1Id, user2Id);
            return messages;
        }

        public List<MessageConversationSummary> GetConversations(string userId)
        {
            EnsureSchema();
            var messages = new List<Message>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, SenderId, ReceiverId, SenderRole, Content, Timestamp, IsRead
                        FROM Messages
                        WHERE SenderId = @UserId OR ReceiverId = @UserId
                        ORDER BY Timestamp DESC;";
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(MapMessage(reader));
                        }
                    }
                }
            }

            var summaries = new Dictionary<string, MessageConversationSummary>(StringComparer.OrdinalIgnoreCase);
            foreach (var message in messages)
            {
                var partnerId = string.Equals(message.SenderId, userId, StringComparison.OrdinalIgnoreCase)
                    ? message.ReceiverId
                    : message.SenderId;

                if (summaries.ContainsKey(partnerId))
                {
                    if (string.Equals(message.ReceiverId, userId, StringComparison.OrdinalIgnoreCase) && !message.IsRead)
                    {
                        summaries[partnerId].UnreadCount++;
                    }
                    continue;
                }

                summaries[partnerId] = new MessageConversationSummary
                {
                    OtherUserId = partnerId,
                    LastMessage = message.Content,
                    LastTimestamp = message.Timestamp,
                    UnreadCount = string.Equals(message.ReceiverId, userId, StringComparison.OrdinalIgnoreCase) && !message.IsRead ? 1 : 0
                };
            }

            return summaries.Values.OrderByDescending(c => c.LastTimestamp).ToList();
        }

        private void MarkConversationRead(string userId, string otherUserId)
        {
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Messages
                        SET IsRead = 1
                        WHERE ReceiverId = @UserId AND SenderId = @OtherUserId AND IsRead = 0;";
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@OtherUserId", otherUserId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static Message MapMessage(IDataReader reader)
        {
            return new Message
            {
                Id = reader["Id"].ToString(),
                SenderId = reader["SenderId"].ToString(),
                ReceiverId = reader["ReceiverId"].ToString(),
                SenderRole = reader["SenderRole"].ToString(),
                Content = reader["Content"].ToString(),
                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                IsRead = Convert.ToBoolean(reader["IsRead"])
            };
        }
    }
}
