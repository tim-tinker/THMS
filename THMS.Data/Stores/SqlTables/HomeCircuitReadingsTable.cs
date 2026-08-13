using Microsoft.Data.Sqlite;
using THMS.Domain.Energy;

namespace THMS.Data.Stores.SqlTables
{
    public class HomeCircuitReadingsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS HomeCircuitReadings (
                Id TEXT PRIMARY KEY,
                Timestamp TEXT NOT NULL,
                WattHours REAL NOT NULL,
                CircuitId TEXT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_HomeCircuitReadings_Timestamp
                ON HomeCircuitReadings (Timestamp);
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, HomeCircuitReading reading)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO HomeCircuitReadings
                (Id, Timestamp, WattHours, CircuitId)
                VALUES
                (@Id, @Timestamp, @WattHours, @CircuitId)
                ON CONFLICT(Timestamp) DO UPDATE SET
                    WattHours = excluded.WattHours,
                    CircuitId = excluded.CircuitId;", conn);

            cmd.Parameters.AddWithValue("@Id", reading.Id.ToString());
            cmd.Parameters.AddWithValue("@Timestamp", reading.Timestamp);
            cmd.Parameters.AddWithValue("@WattHours", reading.KiloWattHours);
            cmd.Parameters.AddWithValue("@CircuitId", reading.CircuitId ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<HomeCircuitReading> GetRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, Timestamp, WattHours, CircuitId
                FROM HomeCircuitReadings
                WHERE Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<HomeCircuitReading>();

            while (reader.Read())
                list.Add(Read(reader));

            return list;
        }

        public HomeCircuitReading? GetLatest(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, Timestamp, WattHours, CircuitId
                FROM HomeCircuitReadings
                ORDER BY Timestamp DESC
                LIMIT 1;", conn);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        private static HomeCircuitReading Read(SqliteDataReader reader)
        {
            return new HomeCircuitReading
            {
                Id = Guid.Parse(reader.GetString(0)),
                Timestamp = reader.GetDateTime(1),
                KiloWattHours = reader.GetDecimal(2),
                CircuitId = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }
    }
}
