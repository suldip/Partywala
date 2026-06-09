using MySql.Data.MySqlClient;

var cs = "server=localhost;port=3306;database=partyclapdb;user=root;password=pass@123;";
using var conn = new MySqlConnection(cs);
conn.Open();

void Count(string label, string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    Console.WriteLine(label + ": " + cmd.ExecuteScalar());
}

Count("Exact Mumbai", "SELECT COUNT(*) FROM Locations WHERE City = 'Mumbai'");
Count("Case Mumbai", "SELECT COUNT(*) FROM Locations WHERE TRIM(UPPER(City)) = 'MUMBAI'");
Count("Maharashtra cities", "SELECT COUNT(DISTINCT City) FROM Locations WHERE State = 'Maharashtra'");
