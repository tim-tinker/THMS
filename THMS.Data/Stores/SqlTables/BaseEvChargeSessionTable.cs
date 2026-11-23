using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class BaseEvChargeSessionTable
    {
        private readonly EvBatteryChargeTable _charges = new();

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
            SqliteSchema.EnsureColumn(conn, "BaseEvChargeSessions", "KwhAddedIsCalculated", "INTEGER NOT NULL DEFAULT 0");
            _charges.InitializeSchema(conn);
        }

        public void UpsertCharge(SqliteConnection conn, Guid sessionId, EvBatteryCharge charge) =>
            _charges.Upsert(conn, sessionId, charge);

        public void DeleteCharge(SqliteConnection conn, Guid sessionId) =>
            _charges.Delete(conn, sessionId);

        public void Upsert(SqliteConnection conn, BaseEvChargeSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO BaseEvChargeSessions (
                Id, LastOdometer, LastSoc, StartTime, StartSoc, EndSoc, KwhAdded, KwhAddedIsCalculated
            )
            VALUES (
                @Id, @LastOdometer, @LastSoc, @StartTime, @StartSoc, @EndSoc, @KwhAdded, @KwhAddedIsCalculated
            )
            ON CONFLICT(Id) DO UPDATE SET
                LastOdometer = excluded.LastOdometer,
                LastSoc = excluded.LastSoc,
                StartTime = excluded.StartTime,
                StartSoc = excluded.StartSoc,
                EndSoc = excluded.EndSoc,
                KwhAdded = excluded.KwhAdded,
                KwhAddedIsCalculated = excluded.KwhAddedIsCalculated;
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@LastOdometer", session.LastOdometer);
            cmd.Parameters.AddWithValue("@LastSoc", session.LastSoc);
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@StartSoc", session.StartSoc);
            cmd.Parameters.AddWithValue("@EndSoc", session.EndSoc);
            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@KwhAddedIsCalculated", session.KwhAddedIsCalculated ? 1 : 0);

            cmd.ExecuteNonQuery();
        }

        public BaseEvChargeSession? GetById(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT
                b.Id, b.LastOdometer, b.LastSoc, b.StartTime, b.StartSoc, b.EndSoc, b.KwhAdded, b.KwhAddedIsCalculated,
                c.KwhAdded, c.IsCalculated
            FROM BaseEvChargeSessions b
            LEFT JOIN EvBatteryCharges c ON c.SessionId = b.Id
            WHERE b.Id = @Id;
            ";
            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var session = Read(reader);
            ApplyStoredCharge(reader, session, 8, 9);
            return session;
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
                b.Id, b.LastOdometer, b.LastSoc, b.StartTime, b.StartSoc, b.EndSoc, b.KwhAdded, b.KwhAddedIsCalculated,
                m.VehicleId, m.VehicleName, m.OdometerMiles, m.EndTime,
                c.KwhAdded, c.IsCalculated
            FROM BaseEvChargeSessions b
            INNER JOIN MileageRecords m ON m.Id = b.Id
            LEFT JOIN EvBatteryCharges c ON c.SessionId = b.Id
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
                session.VehicleId = Guid.Parse(reader.GetString(8));
                session.VehicleName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
                session.OdometerMiles = (decimal)(double)reader.GetDouble(10);
                session.EndTime = reader.GetDateTime(11);
                ApplyStoredCharge(reader, session, 12, 13);
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
                KwhAdded = (decimal)(double)reader.GetDouble(6),
                KwhAddedIsCalculated = !reader.IsDBNull(7) && reader.GetInt64(7) != 0
            };
        }

        private static void ApplyStoredCharge(
            SqliteDataReader reader,
            BaseEvChargeSession session,
            int kwhIndex,
            int calculatedIndex)
        {
            if (reader.IsDBNull(kwhIndex))
                return;

            session.BatteryCharge = new EvBatteryCharge
            {
                KwhAdded = (decimal)(double)reader.GetDouble(kwhIndex),
                IsCalculated = !reader.IsDBNull(calculatedIndex) && reader.GetInt64(calculatedIndex) != 0
            };
        }
    }
}
