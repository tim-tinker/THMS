using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores
{
    public class InMemoryVehicleDataStore : IVehicleDataStore
    {
        private readonly List<VehicleBase> _vehicles = new();
        private readonly List<IceMileageRecord> _iceMileageRecords = new();
        private readonly List<EvChargingSession> _evChargingSessions = new();
        private readonly List<EvChargingSessionVehicleData> _evChargingSessionVehicleData = new();
        private readonly List<ChargingCostRecord> _chargingCostRecords = new();
        private readonly List<MaintenanceInvoiceRecord> _maintenanceInvoices = new();

        public InMemoryVehicleDataStore()
        {
            _vehicles.Add(new VehicleEv
            {
                Id = Guid.NewGuid(),
                Make = "Ford",
                Model = "Mustang Mach-E",
                Year = 2023,
                Vin = "3FMTK3R74PMA89745",
                BatteryCapacityKwh = 92,
                Name = "Tim's",
            });

            _vehicles.Add(new VehicleIce
            {
                Id = Guid.NewGuid(),
                Make = "Ford",
                Model = "Escape SEL",
                Year = 2018,
                Vin = "1FMCU9HD5JUA71357",
                Name = "Julie's",
            });
        }

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

        public IceMileageRecord? GetEarliestIceMileageRecord(Guid vehicleId)
        {
            return _iceMileageRecords
                .Where(r => r.VehicleId == vehicleId)
                .OrderBy(r => r.Date)
                .FirstOrDefault();
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            return _iceMileageRecords
                .Where(r => r.VehicleId == vehicleId && r.Date >= start && r.Date <= end)
                .OrderBy(r => r.Date);
        }

        public decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            // ICE mileage records (Date = mileage event time, OdometerMiles = odometer)
            var iceRecords = _iceMileageRecords
                .Where(r => vehicleId == r.VehicleId && r.Date >= start && r.Date <= end);

            // EV mileage records (Date = end of charging session, OdometerMiles = odometer)
            // ignore sessions without odometer
            var evRecords = _evChargingSessionVehicleData
                .Where(r => vehicleId == r.VehicleId && r.Date >= start && r.Date <= end && r.OdometerMiles.HasValue);

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

        public EvChargingSession? GetEvChargingSession(Guid sessionId)
        {
            return _evChargingSessions.FirstOrDefault(s => s.Id == sessionId);
        }

        public IEnumerable<EvChargingSession> GetEvChargingSessions(
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            return _evChargingSessions
                .Where(s => s.VehicleId == vehicleId &&
                            s.StartTime >= start &&
                            s.StartTime <= end)
                .OrderBy(s => s.StartTime);
        }

        public void UpdateEvChargingSession(EvChargingSession session)
        {
            var existing = _evChargingSessions.FirstOrDefault(s => s.Id == session.Id);
            if (existing == null)
                return;

            existing.StartTime = session.StartTime;
            existing.EndTime = session.EndTime;
            existing.StartSoc = session.StartSoc;
            existing.EndSoc = session.EndSoc;
            existing.OdometerMiles = session.OdometerMiles;
            existing.KwhAdded = session.KwhAdded;
            existing.IsHomeCharging = session.IsHomeCharging;
            existing.ChargingCost = session.ChargingCost;

            // Energy attribution
            existing.GridKwh = session.GridKwh;
            existing.SolarKwh = session.SolarKwh;
            existing.BatteryKwh = session.BatteryKwh;
        }

        public void DeleteEvChargingSession(Guid sessionId)
        {
            var existing = _evChargingSessions.FirstOrDefault(s => s.Id == sessionId);
            if (existing != null)
                _evChargingSessions.Remove(existing);
        }
        // ---------------------------------------------------------
        // MAINTENANCE
        // ---------------------------------------------------------

        public void AddMaintenanceInvoice(MaintenanceInvoiceRecord invoice)
        {
            _maintenanceInvoices.Add(invoice);
        }

        public IEnumerable<MaintenanceInvoiceRecord> GetMaintenanceInvoices(Guid vehicleId, DateTime start, DateTime end)
        {
            return _maintenanceInvoices
                .Where(r => vehicleId == r.VehicleId && r.Date >= start && r.Date <= end)
                .OrderBy(r => r.Date);
        }

        public decimal GetMaintenanceCostInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            return _maintenanceInvoices
                .Where(r => vehicleId == r.VehicleId && r.Date >= start && r.Date <= end)
                .Sum(r => r.Cost);
        }
    }
}
