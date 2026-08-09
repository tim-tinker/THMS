using Microsoft.Data.Sqlite;
using THMS.Domain.Energy;

namespace THMS.Data.Stores.SqlTables
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

        public void Upsert(SqliteConnection conn, EnergyAttributionResult result)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO EvAttribution (Timestamp, EvChargeWh, SolarWh, BatteryWh, GridWh, IsPartial)
            VALUES ($timestamp, $evChargeWh, $solarWh, $batteryWh, $gridWh, $isPartial)
            ON CONFLICT(Timestamp) DO UPDATE SET
                EvChargeWh = excluded.EvChargeWh,
                SolarWh = excluded.SolarWh,
                BatteryWh = excluded.BatteryWh,
                GridWh = excluded.GridWh,
                IsPartial = excluded.IsPartial;
            ";
            cmd.Parameters.AddWithValue("$timestamp", result.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$evChargeWh", result.EvChargeWh * 1000);
            cmd.Parameters.AddWithValue("$solarWh", result.SolarWh * 1000);
            cmd.Parameters.AddWithValue("$batteryWh", result.BatteryWh * 1000);
            cmd.Parameters.AddWithValue("$gridWh", result.GridWh * 1000);
            cmd.Parameters.AddWithValue("$isPartial", result.IsPartial ? 1 : 0);
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
                list.Add(Read(reader));

            return list;
        }

        public EnergyAttributionResult? GetLatest(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
                SELECT Timestamp, EvChargeWh, SolarWh, BatteryWh, GridWh, IsPartial
                FROM EvAttribution
                ORDER BY Timestamp DESC
                LIMIT 1;
            ";

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        private static EnergyAttributionResult Read(SqliteDataReader reader)
        {
            return new EnergyAttributionResult
            {
                Timestamp = reader.GetDateTime(0),
                EvChargeWh = reader.GetDecimal(1),
                SolarWh = reader.GetDecimal(2),
                BatteryWh = reader.GetDecimal(3),
                GridWh = reader.GetDecimal(4),
                IsPartial = reader.GetInt32(5) != 0
            };
        }
    }
}
