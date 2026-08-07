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
        private readonly List<EvChargeSession> _evChargeSessions = new();
        private readonly List<ChargeCostRecord> _chargingCostRecords = new();
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
                .OrderBy(r => r.EndTime)
                .FirstOrDefault();
        }

        public IEnumerable<IceMileageRecord> GetIceMileageRecords(Guid vehicleId, DateTime start, DateTime end)
        {
            return _iceMileageRecords
                .Where(r => r.VehicleId == vehicleId && r.EndTime >= start && r.EndTime <= end)
                .OrderBy(r => r.EndTime);
        }

        public decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            // ICE mileage records (Date = mileage event time, OdometerMiles = odometer)
            var iceRecords = _iceMileageRecords
                .Where(r => vehicleId == r.VehicleId && r.EndTime >= start && r.EndTime <= end);

            // EV mileage records (Date = end of charging session, OdometerMiles = odometer)
            // ignore sessions without odometer
            var evRecords = _evChargeSessions
                .Where(r => vehicleId == r.VehicleId && r.StartTime >= start && r.EndTime <= end && 0 < r.OdometerMiles);

            // Combine ICE + EV as MileageRecordBase
            var allRecords = iceRecords
                .Cast<MileageRecordBase>()
                .Concat(evRecords)
                .OrderBy(r => r.EndTime)
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

        public void AddEvChargeSession(EvChargeSession session)
        {
            _evChargeSessions.Add(session);
        }

        public EvChargeSession? GetEvChargeSession(Guid sessionId)
        {
            return _evChargeSessions.FirstOrDefault(s => s.Id == sessionId);
        }

        public IEnumerable<EvChargeSession> GetEvChargeSessions(
            Guid vehicleId,
            DateTime start,
            DateTime end)
        {
            return _evChargeSessions
                .Where(s => s.VehicleId == vehicleId &&
                            s.StartTime >= start &&
                            s.StartTime <= end)
                .OrderBy(s => s.StartTime);
        }

        public void UpdateEvChargeSession(EvChargeSession session)
        {
            var existing = _evChargeSessions.FirstOrDefault(s => s.Id == session.Id);
            if (existing == null)
                return;

            existing.StartTime = session.StartTime;
            existing.EndTime = session.EndTime;
            existing.StartSoc = session.StartSoc;
            existing.EndSoc = session.EndSoc;
            existing.LastOdometer = session.LastOdometer;
            existing.LastSoc = session.LastSoc;
            existing.OdometerMiles = session.OdometerMiles;
            existing.KwhAdded = session.KwhAdded;
            existing.BatteryKwhAdded = session.BatteryKwhAdded;
            existing.IsHomeCharge = session.IsHomeCharge;
            existing.SessionCost = session.SessionCost;

            // Energy attribution
            existing.GridKwh = session.GridKwh;
            existing.SolarKwh = session.SolarKwh;
            existing.BatteryKwh = session.BatteryKwh;
        }

        public void DeleteEvChargeSession(Guid sessionId)
        {
            var existing = _evChargeSessions.FirstOrDefault(s => s.Id == sessionId);
            if (existing != null)
                _evChargeSessions.Remove(existing);
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
