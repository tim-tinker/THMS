using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class EvChargeSessionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EvChargeSessions (
                Id TEXT PRIMARY KEY,

                LastOdometer REAL NOT NULL,
                LastSoc REAL NOT NULL,

                StartTime TEXT NOT NULL,
                StartSoc REAL NOT NULL,
                EndSoc REAL NOT NULL,
                IsHomeCharge INTEGER NOT NULL,

                KwhAdded REAL NOT NULL,
                BatteryKwhAdded REAL NOT NULL,
                SessionCost REAL NOT NULL,

                GridKwh REAL NOT NULL,
                SolarKwh REAL NOT NULL,
                BatteryKwh REAL NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // INSERT
        // ---------------------------------------------------------
        public void Insert(SqliteConnection conn, EvChargeSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO EvChargeSessions (
                Id,
                LastOdometer, LastSoc,
                StartTime, StartSoc, EndSoc, IsHomeCharge,
                KwhAdded, BatteryKwhAdded, SessionCost,
                GridKwh, SolarKwh, BatteryKwh
            )
            VALUES (
                @Id,
                @LastOdometer, @LastSoc,
                @StartTime, @StartSoc, @EndSoc, @IsHomeCharge,
                @KwhAdded, @BatteryKwhAdded, @SessionCost,
                @GridKwh, @SolarKwh, @BatteryKwh
            );
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());

            cmd.Parameters.AddWithValue("@LastOdometer", session.LastOdometer);
            cmd.Parameters.AddWithValue("@LastSoc", session.LastSoc);

            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@StartSoc", session.StartSoc);
            cmd.Parameters.AddWithValue("@EndSoc", session.EndSoc);
            cmd.Parameters.AddWithValue("@IsHomeCharge", session.IsHomeCharge ? 1 : 0);

            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@BatteryKwhAdded", session.BatteryKwhAdded);
            cmd.Parameters.AddWithValue("@SessionCost", session.SessionCost);

            cmd.Parameters.AddWithValue("@GridKwh", session.GridKwh);
            cmd.Parameters.AddWithValue("@SolarKwh", session.SolarKwh);
            cmd.Parameters.AddWithValue("@BatteryKwh", session.BatteryKwh);

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // GET BY VEHICLE + DATE RANGE
        // ---------------------------------------------------------
        public IEnumerable<EvChargeSession> GetByVehicleAndRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT *
            FROM EvChargeSessions
            WHERE VehicleId = @VehicleId
              AND StartTime >= @Start
              AND StartTime <= @End
            ORDER BY StartTime;
            ";

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();

            var list = new List<EvChargeSession>();
            while (reader.Read())
                list.Add(Read(reader));

            return list;
        }

        // ---------------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------------
        public EvChargeSession? GetById(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM EvChargeSessions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        // ---------------------------------------------------------
        // UPDATE
        // ---------------------------------------------------------
        public void Update(SqliteConnection conn, EvChargeSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            UPDATE EvChargeSessions SET

                LastOdometer = @LastOdometer,
                LastSoc = @LastSoc,

                StartTime = @StartTime,
                StartSoc = @StartSoc,
                EndSoc = @EndSoc,
                IsHomeCharge = @IsHomeCharge,

                KwhAdded = @KwhAdded,
                BatteryKwhAdded = @BatteryKwhAdded,
                SessionCost = @SessionCost,

                GridKwh = @GridKwh,
                SolarKwh = @SolarKwh,
                BatteryKwh = @BatteryKwh

            WHERE Id = @Id;
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());

            cmd.Parameters.AddWithValue("@LastOdometer", session.LastOdometer);
            cmd.Parameters.AddWithValue("@LastSoc", session.LastSoc);

            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@StartSoc", session.StartSoc);
            cmd.Parameters.AddWithValue("@EndSoc", session.EndSoc);
            cmd.Parameters.AddWithValue("@IsHomeCharge", session.IsHomeCharge ? 1 : 0);

            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@BatteryKwhAdded", session.BatteryKwhAdded);
            cmd.Parameters.AddWithValue("@SessionCost", session.SessionCost);

            cmd.Parameters.AddWithValue("@GridKwh", session.GridKwh);
            cmd.Parameters.AddWithValue("@SolarKwh", session.SolarKwh);
            cmd.Parameters.AddWithValue("@BatteryKwh", session.BatteryKwh);

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------
        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM EvChargeSessions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // READ HELPER
        // ---------------------------------------------------------
        private EvChargeSession Read(SqliteDataReader reader)
        {
            return new EvChargeSession
            {
                Id = Guid.Parse(reader["Id"].ToString()!),

                LastOdometer = Convert.ToDecimal(reader["LastOdometer"]),
                LastSoc = Convert.ToDecimal(reader["LastSoc"]),

                StartTime = DateTime.Parse(reader["StartTime"].ToString()!),
                StartSoc = Convert.ToDecimal(reader["StartSoc"]),
                EndSoc = Convert.ToDecimal(reader["EndSoc"]),
                IsHomeCharge = Convert.ToInt32(reader["IsHomeCharge"]) == 1,

                KwhAdded = Convert.ToDecimal(reader["KwhAdded"]),
                BatteryKwhAdded = Convert.ToDecimal(reader["BatteryKwhAdded"]),
                SessionCost = Convert.ToDecimal(reader["SessionCost"]),

                GridKwh = Convert.ToDecimal(reader["GridKwh"]),
                SolarKwh = Convert.ToDecimal(reader["SolarKwh"]),
                BatteryKwh = Convert.ToDecimal(reader["BatteryKwh"])
            };
        }
    }
}
