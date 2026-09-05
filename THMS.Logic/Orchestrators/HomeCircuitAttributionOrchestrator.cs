using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Logic.Energy;

namespace THMS.Logic.Orchestrators
{
    public class HomeCircuitAttributionOrchestrator : BaseOrchestrator
    {
        private readonly IEnergyDataStore _energyStore;

        public HomeCircuitAttributionOrchestrator()
            : this(new DataStoreFactory().GetEnergyStore())
        {
        }

        public HomeCircuitAttributionOrchestrator(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void Update()
        {
            var start = DateTime.MinValue;
            var end = DateTime.Now;
            var engine = new HomeCircuitAttributionEngine(_energyStore);
            engine.Compute(start, end);

            foreach (var result in engine.Results)
            {
                _energyStore.UpsertHomeCircuitAttribution(result);
            }
        }

        public IEnumerable<HomeCircuitAttribution> GetHomeCircuitAttributions(string period)
        {
            var readings = Array.Empty<HomeCircuitAttribution>();
            var latest = _energyStore.GetLatestHomeCircuitReading();
            if (latest is not null)
            {
                var end = latest.Timestamp;
                var start = GetStartDate(end, period);
                readings = _energyStore.GetHomeCircuitAttribution(start, end).ToArray();
            }

            return readings;
        }

    }
}
