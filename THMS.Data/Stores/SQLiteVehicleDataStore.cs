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
        private readonly MileageRecordTable _mileageRecordTable = new();
        private readonly MaintenanceInvoiceTable _maintenanceInvoiceTable = new();
        private readonly EvChargeSessionTable _evSessionTable = new();

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
            _mileageRecordTable.InitializeSchema(conn);
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
                    ChargePortType = chargingPortType
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

        public IceMileageRecord? GetEarliestIceMileageRecord(Guid vehicleId)
        {
            return GetIceMileageRecords(vehicleId, DateTime.MinValue, DateTime.MaxValue)
                .FirstOrDefault();
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();

            var baseRows = _mileageRecordTable
                .GetRange(conn, vehicleId, start, end)
                .Where(r => r.VehicleId == vehicleId && r.Type == "Ice")
                .ToList();

            var results = new List<IceMileageRecord>();

            foreach (var (id, vId, date, odo, _) in baseRows)
            {
                var derived = _iceMileageTable.GetById(conn, id);
                if (derived == null) continue;

                var (gallonsAdded, isFullFillUp, fuelCost) = derived.Value;

                results.Add(new IceMileageRecord
                {
                    Id = id,
                    VehicleId = vId,
                    EndTime = date,
                    OdometerMiles = odo,
                    GallonsAdded = gallonsAdded,
                    IsFullFillUp = isFullFillUp,
                    FuelCost = fuelCost
                });
            }

            return results.OrderBy(r => r.EndTime);
        }

        public decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();

            var records = _mileageRecordTable
                .GetRange(conn, vehicleId, start, end)
                .OrderBy(r => r.EndTime)
                .ToList();

            if (records.Count < 2)
                return 0m;

            var startMiles = records.First().OdometerMiles;
            var endMiles = records.Last().OdometerMiles;

            return endMiles - startMiles;
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS
        // ---------------------------------------------------------

        public void AddEvChargeSession(EvChargeSession session)
        {
            using var conn = OpenConnection();
            _mileageRecordTable.Insert(conn, session, "Ev");
            _evSessionTable.Insert(conn, session);
        }

        public IEnumerable<EvChargeSession> GetEvChargeSessions(Guid vehicleId, DateTime start, DateTime end)
        {
            using var conn = OpenConnection();

            var mileageRecords = _mileageRecordTable.GetRange(conn, vehicleId, start, end);

            foreach (var record in mileageRecords)
            {
                var ev = _evSessionTable.GetById(conn, record.Id);
                if (ev != null)
                {
                    // copy mileage fields
                    ev.VehicleId = record.VehicleId;
                    ev.OdometerMiles = record.OdometerMiles;
                    ev.EndTime = record.EndTime;

                    yield return ev;
                }
            }
        }

        public EvChargeSession? GetEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            var session = _evSessionTable.GetById(conn, sessionId);
            if (session == null)
                return null;

            // Load mileage fields
            var mileageRecord = _mileageRecordTable.GetById(conn, sessionId);
            if (mileageRecord != null)
            {
                session.VehicleId = mileageRecord.Value.VehicleId;
                session.OdometerMiles = mileageRecord.Value.OdometerMiles;
                session.EndTime = mileageRecord.Value.EndTime;
            }

            return session;
        }

        public void UpdateEvChargeSession(EvChargeSession session)
        {
            using var conn = OpenConnection();
            _mileageRecordTable.Update(conn, session);
            _evSessionTable.Update(conn, session);
        }

        public void DeleteEvChargeSession(Guid sessionId)
        {
            using var conn = OpenConnection();
            _evSessionTable.Delete(conn, sessionId);
            _mileageRecordTable.Delete(conn, sessionId);
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
