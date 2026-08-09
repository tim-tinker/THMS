using Microsoft.Data.Sqlite;
using THMS.Domain.Energy;

namespace THMS.Data.Stores.SqlTables
{
    public class EvCircuitSegmentsTable
    {
        // ---------------------------------------------------------
        // Schema
        // ---------------------------------------------------------
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();

            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EvCircuitSegments (
                Id TEXT PRIMARY KEY,
                SessionId TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                DurationSeconds INTEGER NOT NULL,
                Kwh REAL NOT NULL,
                GridKwh REAL NOT NULL,
                SolarKwh REAL NOT NULL,
                BatteryKwh REAL NOT NULL
            );
            ";

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // Save (overwrite)
        // ---------------------------------------------------------
        public void SaveEvCircuitSegments(SqliteConnection conn, Guid sessionId, IEnumerable<EvCircuitSegment> segments)
        {
            // Delete existing records for the session
            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM EvCircuitSegments WHERE SessionId = @SessionId;";
                del.Parameters.AddWithValue("@SessionId", sessionId.ToString());
                del.ExecuteNonQuery();
            }

            // Insert new
            foreach (var s in segments)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                @"
                INSERT INTO EvCircuitSegments (
                    Id, SessionId, Timestamp, DurationSeconds,
                    Kwh, GridKwh, SolarKwh, BatteryKwh
                )
                VALUES (
                    @Id, @SessionId, @Timestamp, @DurationSeconds,
                    @Kwh, @GridKwh, @SolarKwh, @BatteryKwh
                );
                ";

                cmd.Parameters.AddWithValue("@Id", s.Id.ToString());
                cmd.Parameters.AddWithValue("@SessionId", s.SessionId.ToString());
                cmd.Parameters.AddWithValue("@Timestamp", s.Timestamp);
                cmd.Parameters.AddWithValue("@DurationSeconds", s.DurationSeconds);
                cmd.Parameters.AddWithValue("@Kwh", s.Kwh);
                cmd.Parameters.AddWithValue("@GridKwh", s.GridKwh);
                cmd.Parameters.AddWithValue("@SolarKwh", s.SolarKwh);
                cmd.Parameters.AddWithValue("@BatteryKwh", s.BatteryKwh);

                cmd.ExecuteNonQuery();
            }
        }

        // ---------------------------------------------------------
        // Get
        // ---------------------------------------------------------
        public IEnumerable<EvCircuitSegment> GetEvCircuitSegments(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();

            cmd.CommandText =
            @"
            SELECT *
            FROM EvCircuitSegments
            WHERE SessionId = @SessionId
            ORDER BY Timestamp;
            ";

            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());

            using var reader = cmd.ExecuteReader();

            var list = new List<EvCircuitSegment>();
            while (reader.Read())
            {
                list.Add(new EvCircuitSegment
                {
                    Id = Guid.Parse(reader["Id"].ToString()!),
                    SessionId = Guid.Parse(reader["SessionId"].ToString()!),
                    Timestamp = DateTime.Parse(reader["Timestamp"].ToString()!),
                    DurationSeconds = Convert.ToInt32(reader["DurationSeconds"]),
                    Kwh = Convert.ToDecimal(reader["Kwh"]),
                    GridKwh = Convert.ToDecimal(reader["GridKwh"]),
                    SolarKwh = Convert.ToDecimal(reader["SolarKwh"]),
                    BatteryKwh = Convert.ToDecimal(reader["BatteryKwh"])
                });
            }

            return list;
        }

        // ---------------------------------------------------------
        // Delete
        // ---------------------------------------------------------
        public void DeleteEvCircuitSegments(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();

            cmd.CommandText = "DELETE FROM EvCircuitSegments WHERE SessionId = @SessionId;";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // Summary
        // ---------------------------------------------------------
        public EvCircuitSegmentSummary GetEvCircuitSummary(SqliteConnection conn, Guid sessionId)
        {
            var segs = GetEvCircuitSegments(conn, sessionId).ToList();

            if (!segs.Any())
            {
                return new EvCircuitSegmentSummary
                {
                    SessionId = sessionId,
                    TotalKwh = 0,
                    GridKwh = 0,
                    SolarKwh = 0,
                    BatteryKwh = 0,
                    SegmentCount = 0
                };
            }

            return new EvCircuitSegmentSummary
            {
                SessionId = sessionId,
                TotalKwh = segs.Sum(s => s.Kwh),
                GridKwh = segs.Sum(s => s.GridKwh),
                SolarKwh = segs.Sum(s => s.SolarKwh),
                BatteryKwh = segs.Sum(s => s.BatteryKwh),
                SegmentCount = segs.Count,
                StartTime = segs.Min(s => s.Timestamp),
                EndTime = segs.Max(s => s.Timestamp)
            };
        }
    }
}
