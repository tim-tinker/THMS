using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy
{
    public class EnergyAttributionEngine
    {
        private readonly IEnergyDataStore _store;

        public EnergyAttributionEngine(IEnergyDataStore store)
        {
            _store = store;
        }

        public List<EnergyAttributionResult> ComputeAttribution(DateTime start, DateTime end)
        {
            var solar = _store.GetSolarVendorIntervals()
                              .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                              .ToList();

            var ev = _store.GetEvChargingIntervals()
                           .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                           .ToList();

            var results = new List<EnergyAttributionResult>();

            foreach (var s in solar)
            {
                var timestamp = s.Timestamp;

                var evAt = ev.FirstOrDefault(e => e.Timestamp == timestamp);

                var result = new EnergyAttributionResult
                {
                    Timestamp = timestamp,

                    // EV charging attribution
                    EvChargingWh = evAt?.CircuitUseWh ?? 0m,

                    // Solar production
                    SolarWh = s.EnergyProducedWh,

                    // Battery discharge from vendor data
                    BatteryWh = s.DischargedFromBatteriesWh,

                    // Grid import from vendor data
                    GridWh = s.ImportedFromGridWh,

                    // Partial if solar vendor data missing
                    IsPartial = s == null
                };

                results.Add(result);
            }

            return results;
        }
    }
}
