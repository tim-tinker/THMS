using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteVehicleDataStore : IVehicleDataStore
    {
        private readonly string _connectionString;

        public SQLiteVehicleDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // ---------------------------------------------------------
        // VEHICLES
        // ---------------------------------------------------------

        public VehicleBase? GetVehicle(Guid vehicleId)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, Type, Name, Make, Model, Year, Vin,
                       BatteryCapacityKwh, ChargingPortType,
                       FuelTankCapacityGallons, FuelType
                FROM Vehicles
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", vehicleId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var type = reader.GetString(reader.GetOrdinal("Type"));

            if (type == "Ev")
            {
                return new VehicleEv
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(2),
                    Make = reader.GetString(3),
                    Model = reader.GetString(4),
                    Year = reader.GetInt32(5),
                    Vin = reader.GetString(6),
                    BatteryCapacityKwh = reader.GetDecimal(7),
                    ChargingPortType = reader.GetString(8)
                };
            }
            else
            {
                return new VehicleIce
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(2),
                    Make = reader.GetString(3),
                    Model = reader.GetString(4),
                    Year = reader.GetInt32(5),
                    Vin = reader.GetString(6),
                    FuelTankCapacityGallons = reader.GetDecimal(9),
                    FuelType = reader.GetString(10)
                };
            }
        }

        public IEnumerable<VehicleBase> GetAllVehicles()
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, Type, Name, Make, Model, Year, Vin,
                       BatteryCapacityKwh, ChargingPortType,
                       FuelTankCapacityGallons, FuelType
                FROM Vehicles;", conn);

            using var reader = cmd.ExecuteReader();
            var list = new List<VehicleBase>();

            while (reader.Read())
            {
                var type = reader.GetString(reader.GetOrdinal("Type"));

                if (type == "Ev")
                {
                    list.Add(new VehicleEv
                    {
                        Id = Guid.Parse(reader.GetString(0)),
                        Name = reader.GetString(2),
                        Make = reader.GetString(3),
                        Model = reader.GetString(4),
                        Year = reader.GetInt32(5),
                        Vin = reader.GetString(6),
                        BatteryCapacityKwh = reader.GetDecimal(7),
                        ChargingPortType = reader.GetString(8)
                    });
                }
                else
                {
                    list.Add(new VehicleIce
                    {
                        Id = Guid.Parse(reader.GetString(0)),
                        Name = reader.GetString(2),
                        Make = reader.GetString(3),
                        Model = reader.GetString(4),
                        Year = reader.GetInt32(5),
                        Vin = reader.GetString(6),
                        FuelTankCapacityGallons = reader.GetDecimal(9),
                        FuelType = reader.GetString(10)
                    });
                }
            }

            return list;
        }

        public void AddVehicle(VehicleBase vehicle)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO Vehicles
                (Id, Type, Name, Make, Model, Year, Vin,
                 BatteryCapacityKwh, ChargingPortType,
                 FuelTankCapacityGallons, FuelType)
                VALUES
                (@Id, @Type, @Name, @Make, @Model, @Year, @Vin,
                 @BatteryCapacityKwh, @ChargingPortType,
                 @FuelTankCapacityGallons, @FuelType);", conn);

            cmd.Parameters.AddWithValue("@Id", vehicle.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", vehicle.Name);
            cmd.Parameters.AddWithValue("@Make", vehicle.Make);
            cmd.Parameters.AddWithValue("@Model", vehicle.Model);
            cmd.Parameters.AddWithValue("@Year", vehicle.Year);
            cmd.Parameters.AddWithValue("@Vin", vehicle.Vin);

            if (vehicle is VehicleEv ev)
            {
                cmd.Parameters.AddWithValue("@Type", "Ev");
                cmd.Parameters.AddWithValue("@BatteryCapacityKwh", ev.BatteryCapacityKwh);
                cmd.Parameters.AddWithValue("@ChargingPortType", ev.ChargingPortType);
                cmd.Parameters.AddWithValue("@FuelTankCapacityGallons", DBNull.Value);
                cmd.Parameters.AddWithValue("@FuelType", DBNull.Value);
            }
            else if (vehicle is VehicleIce ice)
            {
                cmd.Parameters.AddWithValue("@Type", "Ice");
                cmd.Parameters.AddWithValue("@BatteryCapacityKwh", DBNull.Value);
                cmd.Parameters.AddWithValue("@ChargingPortType", DBNull.Value);
                cmd.Parameters.AddWithValue("@FuelTankCapacityGallons", ice.FuelTankCapacityGallons);
                cmd.Parameters.AddWithValue("@FuelType", ice.FuelType);
            }

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // ICE MILEAGE RECORDS
        // ---------------------------------------------------------

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, VehicleId, Date, OdometerMiles, GallonsAdded, Cost
                FROM IceMileageRecords
                WHERE VehicleId = @VehicleId
                AND Date >= @Start AND Date <= @End
                ORDER BY Date;", conn);

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<IceMileageRecord>();

            while (reader.Read())
            {
                list.Add(new IceMileageRecord
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    VehicleId = Guid.Parse(reader.GetString(1)),
                    Date = reader.GetDateTime(2),
                    OdometerMiles = reader.GetDecimal(3),
                    GallonsAdded = reader.GetDecimal(4),
                    FuelCost = reader.GetDecimal(5)
                });
            }

            return list;
        }

        public void AddIceMileageRecord(IceMileageRecord record)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO IceMileageRecords
                (Id, VehicleId, Date, OdometerMiles, GallonsAdded, Cost)
                VALUES
                (@Id, @VehicleId, @Date, @OdometerMiles, @GallonsAdded, @Cost);", conn);

            cmd.Parameters.AddWithValue("@Id", record.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", record.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@Date", record.Date);
            cmd.Parameters.AddWithValue("@OdometerMiles", record.OdometerMiles);
            cmd.Parameters.AddWithValue("@GallonsAdded", record.GallonsAdded);
            cmd.Parameters.AddWithValue("@Cost", record.FuelCost);

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS
        // ---------------------------------------------------------

        public IEnumerable<EvChargingSession> GetEvChargingSessions(
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT s.Id, s.StartTime, s.EndTime, s.KwhAdded,
                       s.IsHomeCharging, s.ChargingCost, s.VehicleDataId
                FROM EvChargingSessions s
                JOIN EvChargingSessionVehicleData vd
                    ON s.VehicleDataId = vd.Id
                WHERE vd.VehicleId = @VehicleId
                AND s.StartTime >= @Start AND s.EndTime <= @End
                ORDER BY s.StartTime;", conn);

            cmd.Parameters.AddWithValue("@VehicleId", vehicleId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvChargingSession>();

            while (reader.Read())
            {
                list.Add(new EvChargingSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartTime = reader.GetDateTime(1),
                    EndTime = reader.GetDateTime(2),
                    KwhAdded = reader.GetDecimal(3),
                    IsHomeCharging = reader.GetBoolean(4),
                    ChargingCost = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    VehicleDataId = reader.IsDBNull(6)
                        ? null
                        : Guid.Parse(reader.GetString(6))
                });
            }

            return list;
        }

        public IEnumerable<EvChargingSession> GetAllEvChargingSessions()
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartTime, EndTime, KwhAdded,
                       IsHomeCharging, ChargingCost, VehicleDataId
                FROM EvChargingSessions
                ORDER BY StartTime;", conn);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvChargingSession>();

            while (reader.Read())
            {
                list.Add(new EvChargingSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartTime = reader.GetDateTime(1),
                    EndTime = reader.GetDateTime(2),
                    KwhAdded = reader.GetDecimal(3),
                    IsHomeCharging = reader.GetBoolean(4),
                    ChargingCost = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    VehicleDataId = reader.IsDBNull(6)
                        ? null
                        : Guid.Parse(reader.GetString(6))
                });
            }

            return list;
        }

        public void AddEvChargingSession(EvChargingSession session)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO EvChargingSessions
                (Id, StartTime, EndTime, KwhAdded,
                 IsHomeCharging, ChargingCost, VehicleDataId)
                VALUES
                (@Id, @StartTime, @EndTime, @KwhAdded,
                 @IsHomeCharging, @ChargingCost, @VehicleDataId);", conn);

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@StartTime", session.StartTime);
            cmd.Parameters.AddWithValue("@EndTime", session.EndTime);
            cmd.Parameters.AddWithValue("@KwhAdded", session.KwhAdded);
            cmd.Parameters.AddWithValue("@IsHomeCharging", session.IsHomeCharging);

            cmd.Parameters.AddWithValue("@ChargingCost",
                session.ChargingCost.HasValue ? session.ChargingCost.Value : DBNull.Value);

            cmd.Parameters.AddWithValue("@VehicleDataId",
                session.VehicleDataId.HasValue ? session.VehicleDataId.Value.ToString() : DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSION VEHICLE DATA
        // ---------------------------------------------------------

        public EvChargingSessionVehicleData? GetEvChargingSessionVehicleData(Guid id)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                SELECT Id, VehicleId, StartSocPercent,
                       EndSocPercent, OdometerMiles
                FROM EvChargingSessionVehicleData
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new EvChargingSessionVehicleData
            {
                Id = Guid.Parse(reader.GetString(0)),
                VehicleId = Guid.Parse(reader.GetString(1)),
                StartSocPercent = reader.GetDecimal(2),
                EndSocPercent = reader.GetDecimal(3),
                OdometerMiles = reader.GetDecimal(4)
            };
        }

        public void AddEvChargingSessionVehicleData(EvChargingSessionVehicleData data)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                INSERT INTO EvChargingSessionVehicleData
                (Id, VehicleId, StartSocPercent,
                 EndSocPercent, OdometerMiles)
                VALUES
                (@Id, @VehicleId, @StartSocPercent,
                 @EndSocPercent, @OdometerMiles);", conn);

            cmd.Parameters.AddWithValue("@Id", data.Id.ToString());
            cmd.Parameters.AddWithValue("@VehicleId", data.VehicleId.ToString());
            cmd.Parameters.AddWithValue("@StartSocPercent", data.StartSocPercent);
            cmd.Parameters.AddWithValue("@EndSocPercent", data.EndSocPercent);
            cmd.Parameters.AddWithValue("@OdometerMiles", data.OdometerMiles);

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // SESSION ENRICHMENT WORKFLOW
        // ---------------------------------------------------------

        public void AttachVehicleDataToChargingSession(Guid sessionId, Guid vehicleDataId)
        {
            using var conn = OpenConnection();
            using var cmd = new SqliteCommand(@"
                UPDATE EvChargingSessions
                SET VehicleDataId = @VehicleDataId
                WHERE Id = @SessionId;", conn);

            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.Parameters.AddWithValue("@VehicleDataId", vehicleDataId.ToString());

            cmd.ExecuteNonQuery();
        }
    }
}
