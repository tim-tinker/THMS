using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Energy;

namespace THMS.Data.Stores
{
    public class InMemoryEnergyDataStore : IEnergyDataStore
    {
        private readonly InMemoryHomeCircuitReadingStore _circuitReadingsStore = new();
        private readonly InMemorySolarProductionIntervalStore _solarStore = new();
        private readonly InMemoryHomeCircuitAttributionStore _evAttrStore = new();

        // ---------------------------------------------------------
        // HOME CHARGING CIRCUIT READINGS
        // ---------------------------------------------------------

        public void UpsertHomeCircuitReading(HomeCircuitReading reading)
        {
            _circuitReadingsStore.Upsert(reading);
        }

        public IEnumerable<HomeCircuitReading> GetHomeCircuitReadings(DateTime start, DateTime end)
        {
            return _circuitReadingsStore.GetRange(start, end);
        }

        public HomeCircuitReading? GetLatestHomeCircuitReading()
        {
            return _circuitReadingsStore.GetLatest();
        }

        // ---------------------------------------------------------
        // HOME SOLAR VENDOR INTERVALS
        // ---------------------------------------------------------

        public void UpsertSolarProductionInterval(SolarProductionInterval interval)
        {
            _solarStore.Upsert(interval);
        }

        public IEnumerable<SolarProductionInterval> GetSolarProductionIntervals(DateTime start, DateTime end)
        {
            return _solarStore.GetRange(start, end);
        }

        public SolarProductionInterval? GetLatestSolarProductionInterval()
        {
            return _solarStore.GetLatest();
        }

        // ---------------------------------------------------------
        // EV ATTRIBUTION
        // ---------------------------------------------------------

        public void UpsertHomeCircuitAttribution(HomeCircuitAttribution result)
        {
            _evAttrStore.Upsert(result);
        }

        public IReadOnlyCollection<HomeCircuitAttribution> GetHomeCircuitAttribution(DateTime start, DateTime end)
        {
            return _evAttrStore.GetRange(start, end);
        }

        public HomeCircuitAttribution? GetLatestHomeCircuitAttribution()
        {
            return _evAttrStore.GetLatest();
        }
    }
}
