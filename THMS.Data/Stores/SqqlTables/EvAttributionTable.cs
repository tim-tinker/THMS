using Microsoft.Data.Sqlite;
using THMS.Domain.Energy;

namespace THMS.Data.Stores
{
    public class EvAttributionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EvAttribution (
                Timestamp TEXT PRIMARY KEY,
                EvChargeWh REAL NOT NULL,
                SolarWh REAL NOT NULL,
                BatteryWh REAL NOT NULL,
                GridWh REAL NOT NULL,
                IsPartial INTEGER NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public IReadOnlyCollection<EnergyAttributionResult> GetRange(
            SqliteConnection conn, DateTime start, DateTime end)
        {
            var list = new List<EnergyAttributionResult>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
                SELECT Timestamp, EvChargeWh, SolarWh, BatteryWh, GridWh, IsPartial
                FROM EvAttribution
                WHERE Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp;
            ";

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new EnergyAttributionResult
                {
                    Timestamp = reader.GetDateTime(0),
                    EvChargeWh = reader.GetDecimal(1),
                    SolarWh = reader.GetDecimal(2),
                    BatteryWh = reader.GetDecimal(3),
                    GridWh = reader.GetDecimal(4),
                    IsPartial = reader.GetInt32(5) != 0
                });
            }

            return list;
        }
    }
}
