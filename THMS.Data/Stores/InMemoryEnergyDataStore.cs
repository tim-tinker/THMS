using System;
using System.Collections.Generic;
using System.Linq;
using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Energy;
using THMS.Domain.Finance;

namespace THMS.Data.Stores
{
    public class InMemoryEnergyDataStore : IEnergyDataStore
    {
        private readonly List<EvCircuitReading> _circuitReadings = new();
        private readonly List<EvCommercialChargeSession> _commercialSessions = new();
        private readonly List<CommercialChargeCostRecord> _commercialCosts = new();
        private readonly InMemorySolarVendorIntervalStore _solarStore = new();
        private readonly InMemoryEvAttributionStore _evAttrStore = new();
        private readonly InMemoryBatterySocStore _batterySocStore = new();

        public InMemoryEnergyDataStore()
        {
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
            _solarStore.Add(interval);
        }

        public IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(DateTime start, DateTime end)
        {
            return _solarStore.GetRange(start, end);
        }

        // ---------------------------------------------------------
        // EV ATTRIBUTION
        // ---------------------------------------------------------
        public IReadOnlyCollection<EnergyAttributionResult> GetEvAttribution(DateTime start, DateTime end)
        {
            return _evAttrStore.GetRange(start, end);
        }

        // ---------------------------------------------------------
        // BATTERY SOC TIMELINE
        // ---------------------------------------------------------
        public IReadOnlyCollection<BatterySocRecord> GetBatterySocTimeline(DateTime start, DateTime end)
        {
            return _batterySocStore.GetRange(start, end);
        }
        // ---------------------------------------------------------
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        public void AddEvCommercialChargeSession(EvCommercialChargeSession session)
        {
            _commercialSessions.Add(session);
        }

        public IEnumerable<EvCommercialChargeSession> GetEvCommercialChargeSessions(
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

        public void AddCommercialChargeCostRecord(CommercialChargeCostRecord record)
        {
            _commercialCosts.Add(record);
        }

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecords(
            DateTime start,
            DateTime end)
        {
            return _commercialCosts
                .Where(c => c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date);
        }

        public IEnumerable<CommercialChargeCostRecord> GetCommercialChargeCostRecordsByVendor(
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
