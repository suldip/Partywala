using System;
using MySql.Data.MySqlClient;

namespace PartyClap.DAL
{
    /// <summary>
    /// Simple key/value application settings store (admin-configurable values
    /// such as the platform fee percentage).
    /// </summary>
    public class SettingsDAL
    {
        private readonly DBHelper _dbHelper;

        private static bool _schemaEnsured;
        private static readonly object _schemaLock = new object();

        // Default seed values applied when the table is first created.
        private static readonly (string Key, string Value)[] Defaults = new[]
        {
            ("PlatformFeePercent", "10"),
            ("GstPercent", "18")
        };

        public SettingsDAL(DBHelper dbHelper)
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
                        using (var create = connection.CreateCommand())
                        {
                            create.CommandText = @"
                                CREATE TABLE IF NOT EXISTS AppSettings (
                                    SettingKey VARCHAR(64) PRIMARY KEY,
                                    SettingValue VARCHAR(255) NULL,
                                    UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                                );";
                            create.ExecuteNonQuery();
                        }
                        using (var seed = connection.CreateCommand())
                        {
                            seed.CommandText = "INSERT IGNORE INTO AppSettings (SettingKey, SettingValue) VALUES (@Key, @Value)";
                            var pKey = seed.Parameters.Add("@Key", MySqlDbType.VarChar);
                            var pValue = seed.Parameters.Add("@Value", MySqlDbType.VarChar);
                            seed.Prepare();
                            foreach (var (key, value) in Defaults)
                            {
                                pKey.Value = key;
                                pValue.Value = value;
                                seed.ExecuteNonQuery();
                            }
                        }
                    }
                    _schemaEnsured = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in SettingsDAL.EnsureSchema: {ex.Message}");
                }
            }
        }

        public string GetValue(string key, string defaultValue = null)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT SettingValue FROM AppSettings WHERE SettingKey = @Key LIMIT 1";
                    command.Parameters.AddWithValue("@Key", key);
                    var result = command.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return defaultValue;
                    return result.ToString();
                }
            }
        }

        public void SetValue(string key, string value)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO AppSettings (SettingKey, SettingValue) VALUES (@Key, @Value)
                                            ON DUPLICATE KEY UPDATE SettingValue = @Value, UpdatedDate = NOW()";
                    command.Parameters.AddWithValue("@Key", key);
                    command.Parameters.AddWithValue("@Value", value);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
