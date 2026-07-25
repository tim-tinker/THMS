using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores
{
    /// <summary>
    /// SQLite-backed persistence for ICE and EV mileage records.
    /// </summary>
    public class SQLiteVehicleDataStore : IVehicleDataStore
    {
        private readonly string _connectionString;

        public SQLiteVehicleDataStore(string connectionString)
        {
            _connectionString = connectionString;
            Initialize();
        }

        private void Initialize()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            // ICE table
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS IceMileageRecords (
                Id TEXT PRIMARY KEY,
                VehicleId TEXT NOT NULL,
                Date TEXT NOT NULL,
                OdometerMiles REAL NOT NULL,
                GallonsAdded REAL NOT NULL,
                IsFullFillUp INTEGER NOT NULL,
                FuelCost REAL NOT NULL,
                Notes TEXT
            );
            ";
            cmd.ExecuteNonQuery();

            // EV table
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS EvMileageRecords (
                Id TEXT PRIMARY KEY,
                VehicleId TEXT NOT NULL,
                Date TEXT NOT NULL,
                OdometerMiles REAL NOT NULL,
                StartSocPercent REAL NOT NULL,
                EndSocPercent REAL NOT NULL,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                ChargerPowerKw REAL NOT NULL,
                KwhAdded REAL NOT NULL,
                ChargingCost REAL NOT NULL,
                Notes TEXT
            );
            ";
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // ICE MILEAGE RECORDS
        // ---------------------------------------------------------

        public void AddIceMileageRecord(IceMileageRecord record)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO IceMileageRecords
            (Id, VehicleId, Date, OdometerMiles, GallonsAdded, IsFullFillUp, FuelCost, Notes)
            VALUES
            (@Id, @VehicleId, @Date, @OdometerMiles, @GallonsAdded, @IsFullFillUp, @FuelCost, @Notes);
            ";

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", record.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Date", record.Date.ToString("o"));
            cmd.Parameters.AddWithValue("@OdometerMiles", record.OdometerMiles);
            cmd.Parameters.AddWithValue("@GallonsAdded", record.GallonsAdded);
            cmd.Parameters.AddWithValue("@IsFullFillUp", record.IsFullFillUp ? 1 : 0);
            cmd.Parameters.AddWithValue("@FuelCost", record.FuelCost);
            cmd.Parameters.AddWithValue("@Notes", (object?)record.Notes ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT * FROM IceMileageRecords
            WHERE VehicleId = @VehicleId
            ORDER BY Date;
            ";
            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new IceMileageRecord
                {
                    Id = Guid.Parse(reader["Id"].ToString()!),
                    VehicleId = Guid.Parse(reader["VehicleId"].ToString()!),
                    Date = DateTime.Parse(reader["Date"].ToString()!),
                    OdometerMiles = Convert.ToDecimal(reader["OdometerMiles"]),
                    GallonsAdded = Convert.ToDecimal(reader["GallonsAdded"]),
                    IsFullFillUp = Convert.ToInt32(reader["IsFullFillUp"]) == 1,
                    FuelCost = Convert.ToDecimal(reader["FuelCost"]),
                    Notes = reader["Notes"]?.ToString()
                };
            }
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT * FROM IceMileageRecords
            WHERE VehicleId = @VehicleId
              AND Date >= @Start
              AND Date <= @End
            ORDER BY Date;
            ";

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start.ToString("o"));
            cmd.Parameters.AddWithValue("@End", end.ToString("o"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new IceMileageRecord
                {
                    Id = Guid.Parse(reader["Id"].ToString()!),
                    VehicleId = Guid.Parse(reader["VehicleId"].ToString()!),
                    Date = DateTime.Parse(reader["Date"].ToString()!),
                    OdometerMiles = Convert.ToDecimal(reader["OdometerMiles"]),
                    GallonsAdded = Convert.ToDecimal(reader["GallonsAdded"]),
                    IsFullFillUp = Convert.ToInt32(reader["IsFullFillUp"]) == 1,
                    FuelCost = Convert.ToDecimal(reader["FuelCost"]),
                    Notes = reader["Notes"]?.ToString()
                };
            }
        }

        // ---------------------------------------------------------
        // EV MILEAGE RECORDS
        // ---------------------------------------------------------

        public void AddEvMileageRecord(EvMileageRecord record)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO EvMileageRecords
            (Id, VehicleId, Date, OdometerMiles, StartSocPercent, EndSocPercent,
             StartTime, EndTime, ChargerPowerKw, KwhAdded, ChargingCost, Notes)
            VALUES
            (@Id, @VehicleId, @Date, @OdometerMiles, @StartSocPercent, @EndSocPercent,
             @StartTime, @EndTime, @ChargerPowerKw, @KwhAdded, @ChargingCost, @Notes);
            ";

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", record.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Date", record.Date.ToString("o"));
            cmd.Parameters.AddWithValue("@OdometerMiles", record.OdometerMiles);
            cmd.Parameters.AddWithValue("@StartSocPercent", record.StartSocPercent);
            cmd.Parameters.AddWithValue("@EndSocPercent", record.EndSocPercent);
            cmd.Parameters.AddWithValue("@StartTime", record.StartTime.ToString("o"));
            cmd.Parameters.AddWithValue("@EndTime", record.EndTime.ToString("o"));
            cmd.Parameters.AddWithValue("@ChargerPowerKw", record.ChargerPowerKw);
            cmd.Parameters.AddWithValue("@KwhAdded", record.KwhAdded);
            cmd.Parameters.AddWithValue("@ChargingCost", record.ChargingCost);
            cmd.Parameters.AddWithValue("@Notes", (object?)record.Notes ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<EvMileageRecord> GetEvMileageRecords(Guid vehicleId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT * FROM EvMileageRecords
            WHERE VehicleId = @VehicleId
            ORDER BY Date;
            ";
            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new EvMileageRecord
                {
                    Id = Guid.Parse(reader["Id"].ToString()!),
                    VehicleId = Guid.Parse(reader["VehicleId"].ToString()!),
                    Date = DateTime.Parse(reader["Date"].ToString()!),
                    OdometerMiles = Convert.ToDecimal(reader["OdometerMiles"]),
                    StartSocPercent = Convert.ToDecimal(reader["StartSocPercent"]),
                    EndSocPercent = Convert.ToDecimal(reader["EndSocPercent"]),
                    StartTime = DateTime.Parse(reader["StartTime"].ToString()!),
                    EndTime = DateTime.Parse(reader["EndTime"].ToString()!),
                    ChargerPowerKw = Convert.ToDecimal(reader["ChargerPowerKw"]),
                    KwhAdded = Convert.ToDecimal(reader["KwhAdded"]),
                    ChargingCost = Convert.ToDecimal(reader["ChargingCost"]),
                    Notes = reader["Notes"]?.ToString()
                };
            }
        }

        public IEnumerable<EvMileageRecord> GetEvMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT * FROM EvMileageRecords
            WHERE VehicleId = @VehicleId
              AND Date >= @Start
              AND Date <= @End
            ORDER BY Date;
            ";

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start.ToString("o"));
            cmd.Parameters.AddWithValue("@End", end.ToString("o"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                yield return new EvMileageRecord
                {
                    Id = Guid.Parse(reader["Id"].ToString()!),
                    VehicleId = Guid.Parse(reader["VehicleId"].ToString()!),
                    Date = DateTime.Parse(reader["Date"].ToString()!),
                    OdometerMiles = Convert.ToDecimal(reader["OdometerMiles"]),
                    StartSocPercent = Convert.ToDecimal(reader["StartSocPercent"]),
                    EndSocPercent = Convert.ToDecimal(reader["EndSocPercent"]),
                    StartTime = DateTime.Parse(reader["StartTime"].ToString()!),
                    EndTime = DateTime.Parse(reader["EndTime"].ToString()!),
                    ChargerPowerKw = Convert.ToDecimal(reader["ChargerPowerKw"]),
                    KwhAdded = Convert.ToDecimal(reader["KwhAdded"]),
                    ChargingCost = Convert.ToDecimal(reader["ChargingCost"]),
                    Notes = reader["Notes"]?.ToString()
                };
            }
        }

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------

        public void DeleteMileageRecord(Guid recordId)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            // Try ICE first
            cmd.CommandText = "DELETE FROM IceMileageRecords WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", recordId.ToString());
            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
            {
                // Try EV
                cmd.CommandText = "DELETE FROM EvMileageRecords WHERE Id = @Id;";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
