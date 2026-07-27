using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores.InMemory
{
    public class InMemoryVehicleDataStore : IVehicleDataStore
    {
        private readonly List<VehicleBase> _vehicles = new();
        private readonly List<IceMileageRecord> _iceMileageRecords = new();
        private readonly List<EvChargingSession> _evChargingSessions = new();
        private readonly List<EvChargingSessionVehicleData> _evChargingSessionVehicleData = new();
        private readonly List<ChargingCostRecord> _chargingCostRecords = new();
        private readonly List<GasPurchase> _fuelReceipts = new();
        private readonly List<MaintenanceInvoiceRecord> _maintenanceInvoices = new();

        // ---------------------------------------------------------
        // VEHICLES
        // ---------------------------------------------------------

        public void AddVehicle(VehicleBase vehicle)
        {
            _vehicles.Add(vehicle);
        }

        public VehicleBase? GetVehicle(Guid id)
        {
            return _vehicles.FirstOrDefault(v => v.Id == id);
        }

        public IEnumerable<VehicleBase> GetAllVehicles()
        {
            return _vehicles;
        }

        // ---------------------------------------------------------
        // ICE MILEAGE
        // ---------------------------------------------------------

        public void AddIceMileageRecord(IceMileageRecord record)
        {
            _iceMileageRecords.Add(record);
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            return _iceMileageRecords
                .Where(r => r.VehicleId == vehicleId && r.Date >= start && r.Date <= end)
                .OrderBy(r => r.Date);
        }

        public decimal GetMilesDrivenInPeriod(DateTime start, DateTime end)
        {
            // ICE mileage records (Date = mileage event time, OdometerMiles = odometer)
            var iceRecords = _iceMileageRecords
                .Where(r => r.Date >= start && r.Date <= end);

            // EV mileage records (Date = end of charging session, OdometerMiles = odometer)
            var evRecords = _evChargingSessionVehicleData
                .Where(r => r.Date >= start && r.Date <= end)
                .Where(r => r.OdometerMiles.HasValue); // ignore sessions without odometer

            // Combine ICE + EV as MileageRecordBase
            var allRecords = iceRecords
                .Cast<MileageRecordBase>()
                .Concat(evRecords)
                .OrderBy(r => r.Date)
                .ToList();

            if (allRecords.Count < 2)
                return 0m;

            var startMiles = allRecords.First().OdometerMiles;
            var endMiles = allRecords.Last().OdometerMiles;

            return endMiles - startMiles;
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSIONS
        // ---------------------------------------------------------

        public void AddEvChargingSession(EvChargingSession session)
        {
            _evChargingSessions.Add(session);
        }

        public IEnumerable<EvChargingSession> GetEvChargingSessions(Guid vehicleId, DateTime start, DateTime end)
        {
            return _evChargingSessions
                .Where(s => s.VehicleDataId == vehicleId && s.StartTime >= start && s.EndTime <= end)
                .OrderBy(s => s.StartTime);
        }

        public IEnumerable<EvChargingSession> GetUnassignedEvChargingSessions()
        {
            return _evChargingSessions
                .Where(s => s.VehicleDataId == null || s.VehicleDataId == Guid.Empty)
                .OrderBy(s => s.StartTime);
        }

        // ---------------------------------------------------------
        // EV CHARGING SESSION VEHICLE DATA
        // ---------------------------------------------------------

        public void AddEvChargingSessionVehicleData(EvChargingSessionVehicleData data)
        {
            _evChargingSessionVehicleData.Add(data);
        }

        public void UpdateEvChargingSessionVehicleData(EvChargingSessionVehicleData data)
        {
            var existing = _evChargingSessionVehicleData.FirstOrDefault(d => d.Id == data.Id);
            if (existing is null) return;

            _evChargingSessionVehicleData.Remove(existing);
            _evChargingSessionVehicleData.Add(data);
        }

        public EvChargingSessionVehicleData? GetEvChargingSessionVehicleData(Guid id)
        {
            return _evChargingSessionVehicleData.FirstOrDefault(d => d.Id == id);
        }

        public void AttachVehicleDataToChargingSession(Guid sessionId, Guid vehicleDataId)
        {
            var session = _evChargingSessions.FirstOrDefault(s => s.Id == sessionId);
            if (session is null) return;

            session.VehicleDataId = vehicleDataId;
        }

        // ---------------------------------------------------------
        // EV CHARGING COST RECORDS
        // ---------------------------------------------------------

        public void AddChargingCostRecord(ChargingCostRecord record)
        {
            _chargingCostRecords.Add(record);
        }

        public IEnumerable<ChargingCostRecord> GetChargingCosts(DateTime start, DateTime end)
        {
            return _chargingCostRecords
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .OrderBy(r => r.Timestamp);
        }

        public decimal GetChargingCostInPeriod(DateTime start, DateTime end)
        {
            return _chargingCostRecords
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .Sum(r => r.Cost);
        }

        // ---------------------------------------------------------
        // FUEL RECEIPTS
        // ---------------------------------------------------------

        public void AddFuelReceipt(GasPurchase purchase)
        {
            _fuelReceipts.Add(purchase);
        }

        public IEnumerable<GasPurchase> GetFuelReceipts(DateTime start, DateTime end)
        {
            return _fuelReceipts
                .Where(r => r.Date >= start && r.Date <= end)
                .OrderBy(r => r.Date);
        }

        public decimal GetFuelCostInPeriod(DateTime start, DateTime end)
        {
            return _fuelReceipts
                .Where(r => r.Date >= start && r.Date <= end)
                .Sum(r => r.FuelCost);
        }

        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------

        public void AddMaintenanceInvoice(MaintenanceInvoiceRecord invoice)
        {
            _maintenanceInvoices.Add(invoice);
        }

        public IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(DateTime start, DateTime end)
        {
            return _maintenanceInvoices
                .Where(r => r.Date >= start && r.Date <= end)
                .OrderBy(r => r.Date);
        }

        public decimal GetMaintenanceCostInPeriod(DateTime start, DateTime end)
        {
            return _maintenanceInvoices
                .Where(r => r.Date >= start && r.Date <= end)
                .Sum(r => r.Cost);
        }
    }
}
