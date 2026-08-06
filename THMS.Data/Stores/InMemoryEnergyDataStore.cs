using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores
{
    public class InMemoryEnergyDataStore : IEnergyDataStore
    {
        private readonly List<EvCircuitReading> _circuitReadings = new();
        private readonly List<SolarVendorInterval> _solarVendorIntervals = new();
        private readonly List<EvCommercialChargingSession> _commercialSessions = new();
        private readonly List<CommercialChargingCostRecord> _commercialCosts = new();

        public InMemoryEnergyDataStore()
        {
            // ---------------------------------------------------------
            // HOME CIRCUIT READINGS
            // ---------------------------------------------------------
            AddEvCircuitReading(new EvCircuitReading
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Today.AddDays(-3).AddHours(20),
                KiloWattHours = 18500,
                CircuitId = "Garage-240V"
            });

            AddEvCircuitReading(new EvCircuitReading
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Today.AddDays(-3).AddHours(21),
                KiloWattHours = 18200,
                CircuitId = "Garage-240V"
            });

            // ---------------------------------------------------------
            // HOME SOLAR VENDOR INTERVALS
            // ---------------------------------------------------------

            AddSolarVendorInterval(new SolarVendorInterval
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Today.AddHours(9),
                EnergyProducedWh = 1200,
                EnergyConsumedWh = 900,
                ExportedToGridWh = 200,
                ImportedFromGridWh = 0,
                StoredInBatteriesWh = 100,
                DischargedFromBatteriesWh = 0
            });

            AddSolarVendorInterval(new SolarVendorInterval
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Today.AddHours(20),
                EnergyProducedWh = 0,
                EnergyConsumedWh = 1100,
                ExportedToGridWh = 0,
                ImportedFromGridWh = 600,
                StoredInBatteriesWh = 0,
                DischargedFromBatteriesWh = 500
            });

            // ---------------------------------------------------------
            // COMMERCIAL CHARGING SESSIONS
            // ---------------------------------------------------------
            AddEvCommercialChargingSession(new EvCommercialChargingSession
            {
                Id = Guid.NewGuid(),
                StartTime = DateTime.Today.AddDays(-2).AddHours(14),
                EndTime = DateTime.Today.AddDays(-2).AddHours(15),
                KwhAdded = 22.0m,
                ChargingCost = 11.99m,
                VendorSessionId = "EA-2026-07-22-ABC123",
                Location = "Electrify America - Katy"
            });

            AddEvCommercialChargingSession(new EvCommercialChargingSession
            {
                Id = Guid.NewGuid(),
                StartTime = DateTime.Today.AddDays(-5).AddHours(10),
                EndTime = DateTime.Today.AddDays(-5).AddHours(11),
                KwhAdded = 18.0m,
                ChargingCost = 9.50m,
                VendorSessionId = "CP-2026-07-20-XYZ789",
                Location = "ChargePoint - Richmond"
            });
        }

        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        public void AddEvCircuitReading(EvCircuitReading reading)
        {
            _circuitReadings.Add(reading);
        }

        public IEnumerable<EvCircuitReading> GetEvCircuitReadings(DateTime start, DateTime end)
        {
            return _circuitReadings
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .OrderBy(r => r.Timestamp);
        }

        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        public void AddSolarVendorInterval(SolarVendorInterval interval)
        {
            _solarVendorIntervals.Add(interval);
        }

        public IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(DateTime start, DateTime end)
        {
            return _solarVendorIntervals
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .OrderBy(r => r.Timestamp);
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        public void AddEvCommercialChargingSession(EvCommercialChargingSession session)
        {
            _commercialSessions.Add(session);
        }

        public IEnumerable<EvCommercialChargingSession> GetEvCommercialChargingSessions(
            DateTime start,
            DateTime end)
        {
            return _commercialSessions
                .Where(s => s.StartTime >= start && s.EndTime <= end)
                .OrderBy(s => s.StartTime);
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING COST RECORDS
        // ---------------------------------------------------------

        public void AddCommercialChargingCostRecord(CommercialChargingCostRecord record)
        {
            _commercialCosts.Add(record);
        }

        public IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecords(
            DateTime start,
            DateTime end)
        {
            return _commercialCosts
                .Where(c => c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date);
        }

        public IEnumerable<CommercialChargingCostRecord> GetCommercialChargingCostRecordsByVendor(
            string vendor,
            DateTime start,
            DateTime end)
        {
            return _commercialCosts
                .Where(c => c.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date);
        }

        // ----------------------------------------------------------
        // HOME EV CIRCUIT SEGMENTS
        // ----------------------------------------------------------


        private readonly Dictionary<Guid, List<EvCircuitSegment>> _segments
            = new Dictionary<Guid, List<EvCircuitSegment>>();

        public void SaveEvCircuitSegments(Guid sessionId, IEnumerable<EvCircuitSegment> segments)
        {
            _segments[sessionId] = segments.ToList();
        }

        public IEnumerable<EvCircuitSegment> GetEvCircuitSegments(Guid sessionId)
        {
            return _segments.TryGetValue(sessionId, out var list)
                ? list
                : Enumerable.Empty<EvCircuitSegment>();
        }

        public void DeleteEvCircuitSegments(Guid sessionId)
        {
            _segments.Remove(sessionId);
        }

        public EvCircuitSegmentSummary GetEvCircuitSummary(Guid sessionId)
        {
            var segs = GetEvCircuitSegments(sessionId).ToList();

            if (!segs.Any())
            {
                return new EvCircuitSegmentSummary  
                {
                    SessionId = sessionId,
                    TotalKwh = 0,
                    GridKwh = 0,
                    SolarKwh = 0,
                    BatteryKwh = 0,
                    SegmentCount = 0
                };
            }

            return new EvCircuitSegmentSummary
            {
                SessionId = sessionId,
                TotalKwh = segs.Sum(s => s.Kwh),
                GridKwh = segs.Sum(s => s.GridKwh),
                SolarKwh = segs.Sum(s => s.SolarKwh),
                BatteryKwh = segs.Sum(s => s.BatteryKwh),
                SegmentCount = segs.Count,
                StartTime = segs.Min(s => s.Timestamp),
                EndTime = segs.Max(s => s.Timestamp)
            };
        }
    }
}
