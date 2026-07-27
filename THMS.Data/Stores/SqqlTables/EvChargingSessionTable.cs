using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class EvChargingSessionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS EvChargingSessions (
                    Id TEXT PRIMARY KEY,
                    VehicleDataId TEXT,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Insert(SqliteConnection conn, EvChargingSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO EvChargingSessions
                (Id, VehicleDataId, StartTime, EndTime)
                VALUES
                (@Id, @VehicleDataId, @StartTime, @EndTime);";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleDataId",
                session.VehicleDataId == null || session.VehicleDataId == Guid.Empty
                    ? (object?)DBNull.Value
                    : session.VehicleDataId.ToString());
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", session.EndTime);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<EvChargingSession> GetByVehicleAndRange(SqliteConnection conn, Guid vehicleId, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, VehicleDataId, StartTime, EndTime
                FROM EvChargingSessions
                WHERE VehicleDataId = @VehicleDataId
                  AND StartTime >= @Start AND EndTime <= @End
                ORDER BY StartTime;";
            cmd.Parameters.AddWithValue("@VehicleDataId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new EvChargingSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    VehicleDataId = reader.IsDBNull(1) ? Guid.Empty : Guid.Parse(reader.GetString(1)),
                    StartTime = reader.GetDateTime(2),
                    EndTime = reader.GetDateTime(3)
                };
            }
        }

        public IEnumerable<EvChargingSession> GetUnassigned(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, VehicleDataId, StartTime, EndTime
                FROM EvChargingSessions
                WHERE VehicleDataId IS NULL OR VehicleDataId = ''
                ORDER BY StartTime;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new EvChargingSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    VehicleDataId = Guid.Empty,
                    StartTime = reader.GetDateTime(2),
                    EndTime = reader.GetDateTime(3)
                };
            }
        }

        public void AttachVehicleData(SqliteConnection conn, Guid sessionId, Guid vehicleDataId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE EvChargingSessions
                SET VehicleDataId = @VehicleDataId
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());
            cmd.Parameters.AddWithValue("@VehicleDataId", vehicleDataId.ToString());

            cmd.ExecuteNonQuery();
        }
    }
}
