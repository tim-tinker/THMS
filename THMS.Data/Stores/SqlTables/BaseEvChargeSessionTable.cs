using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class BaseEvChargeSessionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS BaseEvChargeSessions (
                Id TEXT PRIMARY KEY,
                LastOdometer REAL NOT NULL,
                LastSoc REAL NOT NULL,
                StartTime TEXT NOT NULL,
                StartSoc REAL NOT NULL,
                EndSoc REAL NOT NULL,
                KwhAdded REAL NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, BaseEvChargeSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO BaseEvChargeSessions (
                Id, LastOdometer, LastSoc, StartTime, StartSoc, EndSoc, KwhAdded
            )
            VALUES (
                @Id, @LastOdometer, @LastSoc, @StartTime, @StartSoc, @EndSoc, @KwhAdded
            )
            ON CONFLICT(Id) DO UPDATE SET
                LastOdometer = excluded.LastOdometer,
                LastSoc = excluded.LastSoc,
                StartTime = excluded.StartTime,
                StartSoc = excluded.StartSoc,
                EndSoc = excluded.EndSoc,
                KwhAdded = excluded.KwhAdded;
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@LastOdometer", session.LastOdometer);
            cmd.Parameters.AddWithValue("@LastSoc", session.LastSoc);
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@StartSoc", session.StartSoc);
            cmd.Parameters.AddWithValue("@EndSoc", session.EndSoc);
            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);

            cmd.ExecuteNonQuery();
        }

        public BaseEvChargeSession? GetById(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT Id, LastOdometer, LastSoc, StartTime, StartSoc, EndSoc, KwhAdded
            FROM BaseEvChargeSessions
            WHERE Id = @Id;
            ";
            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<BaseEvChargeSession> GetByVehicleAndStartRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT
                b.Id, b.LastOdometer, b.LastSoc, b.StartTime, b.StartSoc, b.EndSoc, b.KwhAdded,
                m.VehicleId, m.VehicleName, m.OdometerMiles, m.EndTime
            FROM BaseEvChargeSessions b
            INNER JOIN MileageRecords m ON m.Id = b.Id
            WHERE m.VehicleId = @VehicleId
              AND m.Type = 'Ev'
              AND b.StartTime >= @Start
              AND b.StartTime <= @End
            ORDER BY b.StartTime;
            ";

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var session = Read(reader);
                session.VehicleId = Guid.Parse(reader.GetString(7));
                session.VehicleName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
                session.OdometerMiles = (decimal)(double)reader.GetDouble(9);
                session.EndTime = reader.GetDateTime(10);
                yield return session;
            }
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM BaseEvChargeSessions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static BaseEvChargeSession Read(SqliteDataReader reader)
        {
            return new BaseEvChargeSession
            {
                Id = Guid.Parse(reader.GetString(0)),
                LastOdometer = (decimal)(double)reader.GetDouble(1),
                LastSoc = (decimal)(double)reader.GetDouble(2),
                StartTime = reader.GetDateTime(3),
                StartSoc = (decimal)(double)reader.GetDouble(4),
                EndSoc = (decimal)(double)reader.GetDouble(5),
                KwhAdded = (decimal)(double)reader.GetDouble(6)
            };
        }
    }
}
