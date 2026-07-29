using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteVehicleDataStore : IVehicleDataStore
    {
        private readonly string _connectionString;

        private readonly VehicleTable _vehicleTable = new();
        private readonly VehicleIceTable _vehicleIceTable = new();
        private readonly VehicleEvTable _vehicleEvTable = new();
        private readonly IceMileageTable _iceMileageTable = new();
        private readonly EvChargingSessionVehicleDataTable _evVehicleDataTable = new();
        private readonly MileageRecordTable _mileageRecordTable = new();
        private readonly ChargingCostTable _chargingCostTable = new();
        private readonly MaintenanceInvoiceTable _maintenanceInvoiceTable = new();
        private readonly EvChargingSessionTable _evSessionTable = new();

        public SQLiteVehicleDataStore(string connectionString)
        {
            _connectionString = connectionString;
            using var conn = OpenConnection();
            InitializeSchema(conn);
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void InitializeSchema(SqliteConnection conn)
        {
            _vehicleTable.InitializeSchema(conn);
            _vehicleIceTable.InitializeSchema(conn);
            _vehicleEvTable.InitializeSchema(conn);
            _iceMileageTable.InitializeSchema(conn);
            _evVehicleDataTable.InitializeSchema(conn);
            _mileageRecordTable.InitializeSchema(conn);
            _chargingCostTable.InitializeSchema(conn);
            _maintenanceInvoiceTable.InitializeSchema(conn);
            _evSessionTable.InitializeSchema(conn);
        }

        // ---------------------------------------------------------
        // VEHICLES
        // ---------------------------------------------------------

        public void AddVehicle(VehicleBase vehicle)
        {
            using var conn = OpenConnection();

            _vehicleTable.Insert(conn, vehicle);

            if (vehicle is VehicleIce ice)
                _vehicleIceTable.Insert(conn, ice);
            else if (vehicle is VehicleEv ev)
                _vehicleEvTable.Insert(conn, ev);
        }

        public VehicleBase? GetVehicle(Guid id)
        {
            using var conn = OpenConnection();

            var baseInfo = _vehicleTable.GetBase(conn, id);
            if (baseInfo == null)
                return null;

            var (name, make, model, year, vin, type) = baseInfo.Value;

            if (type == nameof(VehicleIce))
            {
                var iceInfo = _vehicleIceTable.Get(conn, id);
                if (iceInfo == null) return null;

                var (fuelTankCapacityGallons, fuelType) = iceInfo.Value;

                return new VehicleIce
                {
                    Id = id,
                    Name = name,
                    Make = make,
                    Model = model,
                    Year = year,
                    Vin = vin,
                    FuelTankCapacityGallons = fuelTankCapacityGallons,
                    FuelType = fuelType
                };
            }

            if (type == nameof(VehicleEv))
            {
                var evInfo = _vehicleEvTable.Get(conn, id);
                if (evInfo == null) return null;

                var (batteryCapacityKwh, chargingPortType) = evInfo.Value;

                return new VehicleEv
                {
                    Id = id,
                    Name = name,
                    Make = make,
                    Model = model,
                    Year = year,
                    Vin = vin,
                    BatteryCapacityKwh = batteryCapacityKwh,
                    ChargingPortType = chargingPortType
                };
            }

            return null;
        }

        public IEnumerable<VehicleBase> GetAllVehicles()
        {
            using var conn = OpenConnection();
            var ids = _vehicleTable.GetAllIds(conn);

            return ids
                .Select(GetVehicle)
                .Where(v => v != null)!;
        }

        // ---------------------------------------------------------
        // ICE MILEAGE
        // ---------------------------------------------------------

        public void AddIceMileageRecord(IceMileageRecord record)
        {
            using var conn = OpenConnection();

            // base
            _mileageRecordTable.Insert(conn, record, "Ice");
            // derived
            _iceMileageTable.Insert(conn, record);
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();

            var baseRows = _mileageRecordTable
                .GetRange(conn, vehicleId, start, end)
                .Where(r => r.VehicleId == vehicleId && r.Type == "Ice")
                .ToList();

            var results = new List<IceMileageRecord>();

            foreach (var (id, vId, date, odo, notes, _) in baseRows)
            {
                var derived = _iceMileageTable.GetById(conn, id);
                if (derived == null) continue;

                var (gallonsAdded, isFullFillUp, fuelCost) = derived.Value;

                results.Add(new IceMileageRecord
                {
                    Id = id,
                    VehicleId = vId,
                    Date = date,
                    OdometerMiles = odo,
                    Notes = notes,
                    GallonsAdded = gallonsAdded,
                    IsFullFillUp = isFullFillUp,
                    FuelCost = fuelCost
                });
            }

            return results.OrderBy(r => r.Date);
        }

        public decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();

            var records = _mileageRecordTable
                .GetRange(conn, vehicleId, start, end)
                .OrderBy(r => r.Date)
                .ToList();

            if (records.Count < 2)
                return 0m;

            var startMiles = records.First().OdometerMiles;
            var endMiles = records.Last().OdometerMiles;

            return endMiles - startMiles;
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSION VEHICLE DATA
        // ---------------------------------------------------------

        public void AddEvChargingSessionVehicleData(EvChargingSessionVehicleData data)
        {
            using var conn = OpenConnection();

            // base
            _mileageRecordTable.Insert(conn, data, "Ev");
            // derived
            _evVehicleDataTable.Insert(conn, data);
        }

        public void UpdateEvChargingSessionVehicleData(EvChargingSessionVehicleData data)
        {
            using var conn = OpenConnection();

            // base (Date, OdometerMiles, Notes may change)
            _mileageRecordTable.Insert(conn, data, "Ev"); // or an UPDATE variant if you prefer
                                                          // derived
            _evVehicleDataTable.Update(conn, data);
        }

        public IEnumerable<EvChargingSessionVehicleData> GetEvChargingSessionVehicleData(Guid id, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();

            foreach (var baseRow in _mileageRecordTable.GetRange(conn, id, start, end))
            {
                if (baseRow.Type == "Ev")
                {
                    var derived = _evVehicleDataTable.GetById(conn, id);
                    if (derived is not null)
                    {
                        var (startTimestamp, startSocPercent, endSocPercent) = derived.Value;

                        yield return new EvChargingSessionVehicleData
                        {
                            Id = id,
                            VehicleId = id,
                            Date = baseRow.Date,
                            OdometerMiles = baseRow.OdometerMiles,
                            Notes = baseRow.Notes,
                            StartTimestamp = startTimestamp,
                            StartSocPercent = startSocPercent,
                            EndSocPercent = endSocPercent
                        };
                    }
                }
            }
        }

        public EvChargingSessionVehicleData? GetEvChargingSessionVehicleDataById(Guid id)
        {
            using var conn = OpenConnection();
            return _evVehicleDataTable.GetById(conn, id) is { } derived
                ? new EvChargingSessionVehicleData
                {
                    Id = id,
                    VehicleId = id,
                    Date = _mileageRecordTable.GetById(conn, id)?.Date ?? default,
                    OdometerMiles = _mileageRecordTable.GetById(conn, id)?.OdometerMiles ?? default,
                    Notes = _mileageRecordTable.GetById(conn, id)?.Notes ?? string.Empty,
                    StartTimestamp = derived.StartTimestamp,
                    StartSocPercent = derived.StartSocPercent,
                    EndSocPercent = derived.EndSocPercent
                }
                : null;
        }


        public void AttachVehicleDataToChargingSession(Guid sessionId, Guid vehicleDataId)
        {
            using var conn = OpenConnection();
            _evSessionTable.AttachVehicleData(conn, sessionId, vehicleDataId);
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS
        // ---------------------------------------------------------

        public void AddEvChargingSession(EvChargingSession session)
        {
            using var conn = OpenConnection();
            _evSessionTable.Insert(conn, session);
        }

        public IEnumerable<EvChargingSession> GetEvChargingSessions(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _evSessionTable.GetByVehicleAndRange(conn, vehicleId, start, end).ToList();
        }

        public IEnumerable<EvChargingSession> GetUnassignedEvChargingSessions()
        {
            using var conn = OpenConnection();
            return _evSessionTable.GetUnassigned(conn).ToList();
        }

        // ---------------------------------------------------------
        // CHARGING COSTS
        // ---------------------------------------------------------

        public void AddChargingCostRecord(ChargingCostRecord record)
        {
            using var conn = OpenConnection();
            _chargingCostTable.Insert(conn, record);
        }

        public IEnumerable<ChargingCostRecord> GetChargingCosts(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _chargingCostTable.GetRange(conn, vehicleId, start, end).ToList();
        }

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------

        public void AddMaintenanceInvoice(MaintenanceInvoiceRecord invoice)
        {
            using var conn = OpenConnection();
            _maintenanceInvoiceTable.Insert(conn, invoice);
        }

        public IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _maintenanceInvoiceTable.GetRange(conn, vehicleId, start, end).ToList();
        }

        public decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();
            return _maintenanceInvoiceTable.GetTotalCost(conn, vehicleId, start, end);
        }
    }
}
