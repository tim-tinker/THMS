using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class VehicleTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Vehicles (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Make TEXT NOT NULL,
                    Model TEXT NOT NULL,
                    Year INTEGER NOT NULL,
                    Vin TEXT NOT NULL,
                    Type TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Insert(SqliteConnection conn, VehicleBase vehicle)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Vehicles (Id, Name, Make, Model, Year, Vin, Type)
                VALUES (@Id, @Name, @Make, @Model, @Year, @Vin, @Type);";

            cmd.Parameters.AddWithValue("@Id", vehicle.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", vehicle.Name);
            cmd.Parameters.AddWithValue("@Make", vehicle.Make);
            cmd.Parameters.AddWithValue("@Model", vehicle.Model);
            cmd.Parameters.AddWithValue("@Year", vehicle.Year);
            cmd.Parameters.AddWithValue("@Vin", vehicle.Vin);
            cmd.Parameters.AddWithValue("@Type", vehicle.GetType().Name);

            cmd.ExecuteNonQuery();
        }

        public (string Name, string Make, string Model, int Year, string Vin, string Type)? GetBase(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Name, Make, Model, Year, Vin, Type
                FROM Vehicles
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetString(5)
            );
        }

        public IEnumerable<Guid> GetAllIds(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id FROM Vehicles ORDER BY Name;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                yield return Guid.Parse(reader.GetString(0));
        }
    }
}
