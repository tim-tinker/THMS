using Microsoft.Data.Sqlite;
using THMS.Domain.Energy;

namespace THMS.Data.Stores.SqlTables
{
    public class EvCommercialChargeSessionsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EvCommercialChargeSessions (
                Id TEXT PRIMARY KEY,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                KwhAdded REAL NOT NULL,
                ChargeCost REAL,
                VendorSessionId TEXT,
                Location TEXT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_EvCommercialChargeSessions_EndTime
                ON EvCommercialChargeSessions (EndTime);
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, EvCommercialChargeSession session)
        {
            using var cmd = new SqliteCommand(@"
                INSERT INTO EvCommercialChargeSessions
                (Id, StartTime, EndTime, KwhAdded, ChargeCost, VendorSessionId, Location)
                VALUES
                (@Id, @StartTime, @EndTime, @KwhAdded, @ChargeCost, @VendorSessionId, @Location)
                ON CONFLICT(EndTime) DO UPDATE SET
                    StartTime = excluded.StartTime,
                    KwhAdded = excluded.KwhAdded,
                    ChargeCost = excluded.ChargeCost,
                    VendorSessionId = excluded.VendorSessionId,
                    Location = excluded.Location;", conn);

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", session.EndTime);
            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@ChargeCost", session.ChargeCost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorSessionId", session.VendorSessionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Location", session.Location ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<EvCommercialChargeSession> GetRange(
            SqliteConnection conn,
            DateTime start,
            DateTime end)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartTime, EndTime, KwhAdded, ChargeCost, VendorSessionId, Location
                FROM EvCommercialChargeSessions
                WHERE StartTime >= @Start AND EndTime <= @End
                ORDER BY StartTime;", conn);

            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvCommercialChargeSession>();

            while (reader.Read())
            {
                list.Add(new EvCommercialChargeSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartTime = reader.GetDateTime(1),
                    EndTime = reader.GetDateTime(2),
                    KwhAdded = reader.GetDecimal(3),
                    ChargeCost = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    VendorSessionId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Location = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return list;
        }
    }
}
