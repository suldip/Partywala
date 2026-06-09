using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class StateDAL
    {
        private readonly DBHelper _dbHelper;

        // Ensure the States table / seed / indexes exist only once per process.
        private static bool _schemaEnsured;
        private static readonly object _schemaLock = new object();

        private static readonly (string Name, string Code)[] IndianStates = new[]
        {
            ("Andhra Pradesh", "AP"), ("Arunachal Pradesh", "AR"), ("Assam", "AS"),
            ("Bihar", "BR"), ("Chhattisgarh", "CG"), ("Goa", "GA"), ("Gujarat", "GJ"),
            ("Haryana", "HR"), ("Himachal Pradesh", "HP"), ("Jharkhand", "JH"),
            ("Karnataka", "KA"), ("Kerala", "KL"), ("Madhya Pradesh", "MP"),
            ("Maharashtra", "MH"), ("Manipur", "MN"), ("Meghalaya", "ML"),
            ("Mizoram", "MZ"), ("Nagaland", "NL"), ("Odisha", "OD"), ("Punjab", "PB"),
            ("Rajasthan", "RJ"), ("Sikkim", "SK"), ("Tamil Nadu", "TN"),
            ("Telangana", "TS"), ("Tripura", "TR"), ("Uttar Pradesh", "UP"),
            ("Uttarakhand", "UK"), ("West Bengal", "WB"),
            ("Andaman and Nicobar Islands", "AN"), ("Chandigarh", "CH"),
            ("Dadra and Nagar Haveli and Daman and Diu", "DH"), ("Delhi", "DL"),
            ("Jammu and Kashmir", "JK"), ("Ladakh", "LA"), ("Lakshadweep", "LD"),
            ("Puducherry", "PY")
        };

        public StateDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// Creates the States table, seeds all Indian states/UTs, and adds
        /// supporting indexes if they are missing. Idempotent and runs once.
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

                        ExecuteNonQuery(connection, @"
                            CREATE TABLE IF NOT EXISTS States (
                                Id INT AUTO_INCREMENT PRIMARY KEY,
                                Name VARCHAR(100) NOT NULL UNIQUE,
                                Code VARCHAR(10) NULL,
                                IsEnabled TINYINT(1) NOT NULL DEFAULT 1,
                                CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                            );");

                        SeedStates(connection);
                        EnsureLocationIndexes(connection);
                    }
                    _schemaEnsured = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in StateDAL.EnsureSchema: {ex.Message}");
                }
            }
        }

        private void SeedStates(MySqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT IGNORE INTO States (Name, Code) VALUES (@Name, @Code)";
                var pName = command.Parameters.Add("@Name", MySqlDbType.VarChar);
                var pCode = command.Parameters.Add("@Code", MySqlDbType.VarChar);
                command.Prepare();
                foreach (var (name, code) in IndianStates)
                {
                    pName.Value = name;
                    pCode.Value = code;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void EnsureLocationIndexes(MySqlConnection connection)
        {
            CreateIndexIfMissing(connection, "Locations", "idx_locations_state", "State");
            CreateIndexIfMissing(connection, "Locations", "idx_locations_city", "City");
        }

        private void CreateIndexIfMissing(MySqlConnection connection, string table, string indexName, string column)
        {
            try
            {
                using (var check = connection.CreateCommand())
                {
                    check.CommandText = @"SELECT COUNT(1) FROM information_schema.statistics
                                          WHERE table_schema = DATABASE()
                                            AND table_name = @Table
                                            AND index_name = @Index";
                    check.Parameters.AddWithValue("@Table", table);
                    check.Parameters.AddWithValue("@Index", indexName);
                    var exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
                    if (exists) return;
                }
                ExecuteNonQuery(connection, $"CREATE INDEX {indexName} ON {table} ({column});");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating index {indexName}: {ex.Message}");
            }
        }

        private static void ExecuteNonQuery(MySqlConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns every state with enable status and live city / PIN-code counts
        /// (computed from the Locations table). Intended for the admin screen.
        /// </summary>
        public List<State> GetAllStates()
        {
            EnsureSchema();
            var states = new List<State>();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT s.Id, s.Name, s.Code, s.IsEnabled,
                               COUNT(DISTINCT l.City) AS CityCount,
                               COUNT(DISTINCT l.PinCode) AS PinCodeCount
                        FROM States s
                        LEFT JOIN Locations l ON TRIM(UPPER(l.State)) = TRIM(UPPER(s.Name))
                        GROUP BY s.Id, s.Name, s.Code, s.IsEnabled
                        ORDER BY s.Name";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            states.Add(new State
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Code = reader["Code"]?.ToString(),
                                IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                                CityCount = Convert.ToInt32(reader["CityCount"]),
                                PinCodeCount = Convert.ToInt32(reader["PinCodeCount"])
                            });
                        }
                    }
                }
            }
            return states;
        }

        /// <summary>Names of states that are currently enabled (for public dropdowns).</summary>
        public List<string> GetEnabledStateNames()
        {
            EnsureSchema();
            var names = new List<string>();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Name FROM States WHERE IsEnabled = 1 ORDER BY Name";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names.Add(reader["Name"].ToString());
                        }
                    }
                }
            }
            return names;
        }

        public void SetStateEnabled(int id, bool enabled)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE States SET IsEnabled = @Enabled WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Enabled", enabled ? 1 : 0);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SetAllStatesEnabled(bool enabled)
        {
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE States SET IsEnabled = @Enabled";
                    command.Parameters.AddWithValue("@Enabled", enabled ? 1 : 0);
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool IsStateEnabled(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName)) return false;
            EnsureSchema();
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT IsEnabled FROM States WHERE TRIM(UPPER(Name)) = TRIM(UPPER(@Name)) LIMIT 1";
                    command.Parameters.AddWithValue("@Name", stateName.Trim());
                    var result = command.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return false;
                    return Convert.ToBoolean(result);
                }
            }
        }

        /// <summary>Resolves the state for a PIN code from the Locations table.</summary>
        public string GetStateByPinCode(string pinCode)
        {
            if (string.IsNullOrWhiteSpace(pinCode)) return null;
            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT State FROM Locations WHERE PinCode = @PinCode LIMIT 1";
                    command.Parameters.AddWithValue("@PinCode", pinCode.Trim());
                    var result = command.ExecuteScalar();
                    return result == null || result == DBNull.Value ? null : result.ToString();
                }
            }
        }

        /// <summary>
        /// Bulk-imports location rows into the Locations table (INSERT IGNORE so
        /// re-imports are safe) and auto-registers any new states found.
        /// Returns the number of rows processed.
        /// </summary>
        public int ImportLocations(IEnumerable<Location> rows)
        {
            EnsureSchema();
            int processed = 0;
            var newStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var connection = (MySqlConnection)_dbHelper.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"INSERT IGNORE INTO Locations (PinCode, AreaName, City, State)
                                            VALUES (@PinCode, @AreaName, @City, @State)";
                    var pPin = command.Parameters.Add("@PinCode", MySqlDbType.VarChar);
                    var pArea = command.Parameters.Add("@AreaName", MySqlDbType.VarChar);
                    var pCity = command.Parameters.Add("@City", MySqlDbType.VarChar);
                    var pState = command.Parameters.Add("@State", MySqlDbType.VarChar);
                    command.Prepare();

                    foreach (var row in rows)
                    {
                        if (row == null || string.IsNullOrWhiteSpace(row.PinCode)) continue;

                        pPin.Value = Trunc(row.PinCode, 10);
                        pArea.Value = Trunc(row.AreaName ?? "", 100);
                        pCity.Value = Trunc(row.City ?? "", 50);
                        pState.Value = Trunc(row.State ?? "", 50);
                        command.ExecuteNonQuery();

                        if (!string.IsNullOrWhiteSpace(row.State))
                        {
                            newStates.Add(row.State.Trim());
                        }
                        processed++;
                    }

                    transaction.Commit();
                }

                // Register any states present in the data but missing from States.
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT IGNORE INTO States (Name, IsEnabled) VALUES (@Name, 1)";
                    var pName = command.Parameters.Add("@Name", MySqlDbType.VarChar);
                    command.Prepare();
                    foreach (var state in newStates)
                    {
                        pName.Value = Trunc(state, 100);
                        command.ExecuteNonQuery();
                    }
                }
            }
            return processed;
        }

        private static string Trunc(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
            value = value.Trim();
            return value.Length > max ? value.Substring(0, max) : value;
        }
    }
}
