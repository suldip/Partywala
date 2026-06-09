using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using PartyClap.Models;

namespace PartyClap.DAL
{
    public class LocationDAL
    {
        private readonly DBHelper _dbHelper;

        public LocationDAL(DBHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public List<Location> GetLocations()
        {
            var locations = new List<Location>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = "sp_GetLocations";
                    command.CommandType = CommandType.StoredProcedure;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locations.Add(new Location
                            {
                                PinCode = reader["PinCode"].ToString(),
                                AreaName = reader["AreaName"].ToString(),
                                City = reader["City"].ToString(),
                                State = reader["State"].ToString()
                            });
                        }
                    }
                }
            }
            return locations;
        }

        public List<string> GetStates()
        {
            var states = new List<string>();
            try
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = "SELECT DISTINCT State FROM Locations ORDER BY State";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var state = reader["State"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(state))
                                {
                                    states.Add(state);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetStates: {ex.Message}");
                Console.WriteLine($"Error in GetStates: {ex.Message}");
            }
            return states;
        }

        public List<string> GetCitiesByState(string state)
        {
            var cities = new List<string>();
            if (string.IsNullOrWhiteSpace(state))
            {
                return cities;
            }
            
            try
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        // Use case-insensitive comparison and trim whitespace
                        // Also try LIKE for partial matches in case of slight variations
                        command.CommandText = @"SELECT DISTINCT City 
                                              FROM Locations 
                                              WHERE TRIM(UPPER(State)) = TRIM(UPPER(@State))
                                                 OR State LIKE @StateLike
                                              ORDER BY City";
                        command.Parameters.AddWithValue("@State", state.Trim());
                        command.Parameters.AddWithValue("@StateLike", $"%{state.Trim()}%");
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var city = reader["City"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(city))
                                {
                                    var trimmedCity = city.Trim();
                                    // Avoid duplicates
                                    if (!cities.Contains(trimmedCity, StringComparer.OrdinalIgnoreCase))
                                    {
                                        cities.Add(trimmedCity);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Log for debugging
                System.Diagnostics.Debug.WriteLine($"Found {cities.Count} cities for state '{state}'");
                Console.WriteLine($"Found {cities.Count} cities for state '{state}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCitiesByState for state '{state}': {ex.Message}");
                Console.WriteLine($"Error in GetCitiesByState for state '{state}': {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return cities;
        }

        public List<Location> GetPinCodesByCity(string city, string state = null)
        {
            var locations = new List<Location>();
            if (string.IsNullOrWhiteSpace(city))
            {
                return locations;
            }
            
            try
            {
                using (var connection = _dbHelper.CreateConnection())
                {
                    connection.Open();
                    using (var command = (MySqlCommand)connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT PinCode, AreaName, City, State
                                              FROM Locations
                                              WHERE TRIM(UPPER(City)) = TRIM(UPPER(@City))";
                        command.Parameters.AddWithValue("@City", city.Trim());

                        if (!string.IsNullOrWhiteSpace(state))
                        {
                            command.CommandText += " AND TRIM(UPPER(State)) = TRIM(UPPER(@State))";
                            command.Parameters.AddWithValue("@State", state.Trim());
                        }

                        command.CommandText += " ORDER BY PinCode";

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                locations.Add(new Location
                                {
                                    PinCode = reader["PinCode"]?.ToString() ?? "",
                                    AreaName = reader["AreaName"]?.ToString() ?? "",
                                    City = reader["City"]?.ToString() ?? "",
                                    State = reader["State"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetPinCodesByCity for city '{city}': {ex.Message}");
                Console.WriteLine($"Error in GetPinCodesByCity for city '{city}': {ex.Message}");
            }
            return locations;
        }

        public List<Location> SearchPinCodes(string searchTerm)
        {
            var locations = new List<Location>();
            using (var connection = _dbHelper.CreateConnection())
            {
                connection.Open();
                using (var command = (MySqlCommand)connection.CreateCommand())
                {
                    command.CommandText = @"SELECT PinCode, AreaName, City, State 
                                          FROM Locations 
                                          WHERE PinCode LIKE @SearchTerm 
                                             OR AreaName LIKE @SearchTerm 
                                             OR City LIKE @SearchTerm 
                                          ORDER BY PinCode 
                                          LIMIT 50";
                    command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            locations.Add(new Location
                            {
                                PinCode = reader["PinCode"].ToString(),
                                AreaName = reader["AreaName"].ToString(),
                                City = reader["City"].ToString(),
                                State = reader["State"].ToString()
                            });
                        }
                    }
                }
            }
            return locations;
        }
    }
}
