using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class EvChargingSessionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EvChargingSessions (
                Id TEXT PRIMARY KEY,
                VehicleId TEXT NOT NULL,

                LastOdometer REAL NOT NULL,
                LastSoc REAL NOT NULL,

                OdometerMiles REAL NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                StartSoc REAL NOT NULL,
                EndSoc REAL NOT NULL,
                IsHomeCharging INTEGER NOT NULL,

                KwhAdded REAL NOT NULL,
                ChargingCost REAL NOT NULL,

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
        public void Insert(SqliteConnection conn, EvChargingSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO EvChargingSessions (
                Id, VehicleId,
                LastOdometer, LastSoc,
                OdometerMiles, StartTime, EndTime,
                StartSoc, EndSoc, IsHomeCharging,
                KwhAdded, ChargingCost,
                GridKwh, SolarKwh, BatteryKwh
            )
            VALUES (
                @Id, @VehicleId,
                @LastOdometer, @LastSoc,
                @OdometerMiles, @StartTime, @EndTime,
                @StartSoc, @EndSoc, @IsHomeCharging,
                @KwhAdded, @ChargingCost,
                @GridKwh, @SolarKwh, @BatteryKwh
            );
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", session.VehicleId.ToString());

            cmd.Parameters.AddWithValue("@LastOdometer", session.LastOdometer);
            cmd.Parameters.AddWithValue("@LastSoc", session.LastSoc);

            cmd.Parameters.AddWithValue("@OdometerMiles", session.OdometerMiles);
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", session.EndTime);
            cmd.Parameters.AddWithValue("@StartSoc", session.StartSoc);
            cmd.Parameters.AddWithValue("@EndSoc", session.EndSoc);
            cmd.Parameters.AddWithValue("@IsHomeCharging", session.IsHomeCharging ? 1 : 0);

            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@ChargingCost", session.ChargingCost);

            cmd.Parameters.AddWithValue("@GridKwh", session.GridKwh);
            cmd.Parameters.AddWithValue("@SolarKwh", session.SolarKwh);
            cmd.Parameters.AddWithValue("@BatteryKwh", session.BatteryKwh);

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // GET BY VEHICLE + DATE RANGE
        // ---------------------------------------------------------
        public IEnumerable<EvChargingSession> GetByVehicleAndRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT *
            FROM EvChargingSessions
            WHERE VehicleId = @VehicleId
              AND StartTime >= @Start
              AND StartTime <= @End
            ORDER BY StartTime;
            ";

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();

            var list = new List<EvChargingSession>();
            while (reader.Read())
                list.Add(Read(reader));

            return list;
        }

        // ---------------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------------
        public EvChargingSession? GetById(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM EvChargingSessions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        // ---------------------------------------------------------
        // UPDATE
        // ---------------------------------------------------------
        public void Update(SqliteConnection conn, EvChargingSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            UPDATE EvChargingSessions SET
                VehicleId = @VehicleId,

                LastOdometer = @LastOdometer,
                LastSoc = @LastSoc,

                OdometerMiles = @OdometerMiles,
                StartTime = @StartTime,
                EndTime = @EndTime,
                StartSoc = @StartSoc,
                EndSoc = @EndSoc,
                IsHomeCharging = @IsHomeCharging,

                KwhAdded = @KwhAdded,
                ChargingCost = @ChargingCost,

                GridKwh = @GridKwh,
                SolarKwh = @SolarKwh,
                BatteryKwh = @BatteryKwh

            WHERE Id = @Id;
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", session.VehicleId.ToString());

            cmd.Parameters.AddWithValue("@LastOdometer", session.LastOdometer);
            cmd.Parameters.AddWithValue("@LastSoc", session.LastSoc);

            cmd.Parameters.AddWithValue("@OdometerMiles", session.OdometerMiles);
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", session.EndTime);
            cmd.Parameters.AddWithValue("@StartSoc", session.StartSoc);
            cmd.Parameters.AddWithValue("@EndSoc", session.EndSoc);
            cmd.Parameters.AddWithValue("@IsHomeCharging", session.IsHomeCharging ? 1 : 0);

            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@ChargingCost", session.ChargingCost);

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
            cmd.CommandText = "DELETE FROM EvChargingSessions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // READ HELPER
        // ---------------------------------------------------------
        private EvChargingSession Read(SqliteDataReader reader)
        {
            return new EvChargingSession
            {
                Id = Guid.Parse(reader["Id"].ToString()!),
                VehicleId = Guid.Parse(reader["VehicleId"].ToString()!),

                LastOdometer = Convert.ToDecimal(reader["LastOdometer"]),
                LastSoc = Convert.ToDecimal(reader["LastSoc"]),

                OdometerMiles = Convert.ToDecimal(reader["OdometerMiles"]),
                StartTime = DateTime.Parse(reader["StartTime"].ToString()!),
                EndTime = DateTime.Parse(reader["EndTime"].ToString()!),
                StartSoc = Convert.ToDecimal(reader["StartSoc"]),
                EndSoc = Convert.ToDecimal(reader["EndSoc"]),
                IsHomeCharging = Convert.ToInt32(reader["IsHomeCharging"]) == 1,

                KwhAdded = Convert.ToDecimal(reader["KwhAdded"]),
                ChargingCost = Convert.ToDecimal(reader["ChargingCost"]),

                GridKwh = Convert.ToDecimal(reader["GridKwh"]),
                SolarKwh = Convert.ToDecimal(reader["SolarKwh"]),
                BatteryKwh = Convert.ToDecimal(reader["BatteryKwh"])
            };
        }
    }
}
