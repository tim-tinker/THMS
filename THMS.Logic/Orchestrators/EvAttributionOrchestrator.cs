using THMS.Data.Stores;
using THMS.Logic.Energy;

namespace THMS.Logic.Orchestrators
{
    public class EvAttributionOrchestrator
    {
        private IEnergyDataStore _energyStore;

        public EvAttributionOrchestrator(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void Update()
        {
            var start = DateTime.MinValue;
            var end = DateTime.Now;
            var engine = new EvAttributionEngine(_energyStore);
            engine.Compute(start, end);

            foreach (var result in engine.Results)
            {
                _energyStore.UpsertEvAttribution(result);
            }
        }
    }
}
