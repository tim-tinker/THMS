using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class ChargingCostTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ChargingCostRecords (
                    Id TEXT PRIMARY KEY,
                    VehicleId TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    EnergyWh REAL NOT NULL,
                    Cost REAL NOT NULL,
                    Location TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public void Insert(SqliteConnection conn, ChargingCostRecord record)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ChargingCostRecords
                (Id, VehicleId, Timestamp, EnergyWh, Cost, Location)
                VALUES
                (@Id, @VehicleId, @Timestamp, @EnergyWh, @Cost, @Location);";

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", record.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Timestamp", record.Timestamp);
            cmd.Parameters.AddWithValue("@EnergyWh", record.EnergyWh);
            cmd.Parameters.AddWithValue("@Cost", record.Cost);
            cmd.Parameters.AddWithValue("@Location", (object?)record.Location ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<ChargingCostRecord> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, VehicleId, Timestamp, EnergyWh, Cost, Location
                FROM ChargingCostRecords
                WHERE Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new ChargingCostRecord
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    VehicleId = Guid.Parse(reader.GetString(1)),
                    Timestamp = reader.GetDateTime(2),
                    EnergyWh = (decimal)(double)reader.GetDouble(3),
                    Cost = (decimal)(double)reader.GetDouble(4),
                    Location = reader.IsDBNull(5) ? null : reader.GetString(5)
                };
            }
        }
    }
}
