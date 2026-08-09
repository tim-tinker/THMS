using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Energy;

namespace THMS.Data.Stores
{
    public class InMemoryEnergyDataStore : IEnergyDataStore
    {
        private readonly InMemoryEvCircuitReadingStore _circuitReadingsStore = new();
        private readonly InMemoryEvCommercialChargeSessionStore _commercialSessionsStore = new();
        private readonly InMemoryEvCircuitSegmentsStore _circuitSegmentsStore = new();
        private readonly InMemorySolarVendorIntervalStore _solarStore = new();
        private readonly InMemoryEvAttributionStore _evAttrStore = new();

        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        public void UpsertEvCircuitReading(EvCircuitReading reading)
        {
            _circuitReadingsStore.Upsert(reading);
        }

        public IEnumerable<EvCircuitReading> GetEvCircuitReadings(DateTime start, DateTime end)
        {
            return _circuitReadingsStore.GetRange(start, end);
        }

        public EvCircuitReading? GetLatestEvCircuitReading()
        {
            return _circuitReadingsStore.GetLatest();
        }

        // ---------------------------------------------------------
        // HOME SOLAR VENDOR INTERVALS
        // ---------------------------------------------------------

        public void UpsertSolarVendorInterval(SolarVendorInterval interval)
        {
            _solarStore.Upsert(interval);
        }

        public IEnumerable<SolarVendorInterval> GetSolarVendorIntervals(DateTime start, DateTime end)
        {
            return _solarStore.GetRange(start, end);
        }

        public SolarVendorInterval? GetLatestSolarVendorInterval()
        {
            return _solarStore.GetLatest();
        }

        // ---------------------------------------------------------
        // EV ATTRIBUTION
        // ---------------------------------------------------------

        public void UpsertEvAttribution(EnergyAttributionResult result)
        {
            _evAttrStore.Upsert(result);
        }

        public IReadOnlyCollection<EnergyAttributionResult> GetEvAttribution(DateTime start, DateTime end)
        {
            return _evAttrStore.GetRange(start, end);
        }

        public EnergyAttributionResult? GetLatestEvAttribution()
        {
            return _evAttrStore.GetLatest();
        }

        // ---------------------------------------------------------
        // COMMERCIAL CHARGING SESSIONS
        // ---------------------------------------------------------

        public void UpsertEvCommercialChargeSession(EvCommercialChargeSession session)
        {
            _commercialSessionsStore.Upsert(session);
        }

        public IEnumerable<EvCommercialChargeSession> GetEvCommercialChargeSessions(
            DateTime start,
            DateTime end)
        {
            return _commercialSessionsStore.GetRange(start, end);
        }

        // ----------------------------------------------------------
        // HOME EV CIRCUIT SEGMENTS
        // ----------------------------------------------------------

        public void SaveEvCircuitSegments(Guid sessionId, IEnumerable<EvCircuitSegment> segments)
        {
            _circuitSegmentsStore.Save(sessionId, segments);
        }

        public IEnumerable<EvCircuitSegment> GetEvCircuitSegments(Guid sessionId)
        {
            return _circuitSegmentsStore.Get(sessionId);
        }

        public void DeleteEvCircuitSegments(Guid sessionId)
        {
            _circuitSegmentsStore.Delete(sessionId);
        }

        public EvCircuitSegmentSummary GetEvCircuitSummary(Guid sessionId)
        {
            return _circuitSegmentsStore.GetSummary(sessionId);
        }
    }
}
