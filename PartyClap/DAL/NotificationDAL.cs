using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class NotificationDAL
    {
        private readonly DBHelper _dbHelper;

        private static bool _schemaEnsured;
        private static readonly object _schemaLock = new object();

        public NotificationDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

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
                                CREATE TABLE IF NOT EXISTS Notifications (
                                    Id VARCHAR(36) PRIMARY KEY,
                                    UserId VARCHAR(36) NOT NULL,
                                    UserType VARCHAR(20) NOT NULL,
                                    Title VARCHAR(200) NOT NULL,
                                    Message TEXT NOT NULL,
                                    Type VARCHAR(50) NULL,
                                    RelatedId VARCHAR(36) NULL,
                                    Icon VARCHAR(20) NULL,
                                    ActionUrl VARCHAR(500) NULL,
                                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    IsRead TINYINT(1) NOT NULL DEFAULT 0,
                                    INDEX idx_notif_user (UserId, UserType),
                                    INDEX idx_notif_unread (UserId, UserType, IsRead)
                                );";
                            command.ExecuteNonQuery();
                        }
                    }
                    _schemaEnsured = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in NotificationDAL.EnsureSchema: {ex.Message}");
                }
            }
        }

        public void CreateNotification(Notification notification)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO Notifications
                            (Id, UserId, UserType, Title, Message, Type, RelatedId, Icon, ActionUrl, CreatedAt, IsRead)
                        VALUES
                            (@Id, @UserId, @UserType, @Title, @Message, @Type, @RelatedId, @Icon, @ActionUrl, @CreatedAt, @IsRead)";
                    command.Parameters.AddWithValue("@Id", notification.Id);
                    command.Parameters.AddWithValue("@UserId", notification.UserId);
                    command.Parameters.AddWithValue("@UserType", notification.UserType);
                    command.Parameters.AddWithValue("@Title", notification.Title ?? "");
                    command.Parameters.AddWithValue("@Message", notification.Message ?? "");
                    command.Parameters.AddWithValue("@Type", (object)notification.Type ?? DBNull.Value);
                    command.Parameters.AddWithValue("@RelatedId", (object)notification.RelatedId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Icon", (object)notification.Icon ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ActionUrl", (object)notification.ActionUrl ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedAt", notification.CreatedAt);
                    command.Parameters.AddWithValue("@IsRead", notification.IsRead);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Notification> GetUserNotifications(string userId, string userType)
        {
            EnsureSchema();
            var notifications = new List<Notification>();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, UserId, UserType, Title, Message, Type, RelatedId, Icon, ActionUrl, CreatedAt, IsRead
                        FROM Notifications
                        WHERE UserId = @UserId AND UserType = @UserType
                        ORDER BY CreatedAt DESC";
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@UserType", userType);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notifications.Add(MapNotification(reader));
                        }
                    }
                }
            }
            return notifications;
        }

        public int GetUnreadCount(string userId, string userType)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT COUNT(1) FROM Notifications
                        WHERE UserId = @UserId AND UserType = @UserType AND IsRead = 0";
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@UserType", userType);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void MarkAsRead(string notificationId)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Notifications SET IsRead = 1 WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", notificationId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void MarkAllAsRead(string userId, string userType)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE Notifications SET IsRead = 1
                        WHERE UserId = @UserId AND UserType = @UserType AND IsRead = 0";
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@UserType", userType);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Notification GetNotification(string notificationId)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, UserId, UserType, Title, Message, Type, RelatedId, Icon, ActionUrl, CreatedAt, IsRead
                        FROM Notifications WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", notificationId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapNotification(reader);
                        }
                    }
                }
            }
            return null;
        }

        private static Notification MapNotification(MySqlDataReader reader)
        {
            return new Notification
            {
                Id = reader["Id"].ToString(),
                UserId = reader["UserId"].ToString(),
                UserType = reader["UserType"].ToString(),
                Title = reader["Title"].ToString(),
                Message = reader["Message"].ToString(),
                Type = reader["Type"] != DBNull.Value ? reader["Type"].ToString() : null,
                RelatedId = reader["RelatedId"] != DBNull.Value ? reader["RelatedId"].ToString() : null,
                Icon = reader["Icon"] != DBNull.Value ? reader["Icon"].ToString() : null,
                ActionUrl = reader["ActionUrl"] != DBNull.Value ? reader["ActionUrl"].ToString() : null,
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                IsRead = Convert.ToBoolean(reader["IsRead"])
            };
        }
    }
}
