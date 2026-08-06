using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy
{
    /// <summary>
    /// This class breaks down 
    /// </summary>
    public class EnergyAttributionEngine
    {
        private readonly IEnergyDataStore _store;

        public EnergyAttributionEngine(IEnergyDataStore store)
        {
            _store = store;
        }

        public List<EnergyAttributionResult> ComputeAttribution(DateTime start, DateTime end)
        {
            var solar = _store.GetSolarVendorIntervals(start, end)
                              .ToList();

            var ev = _store.GetEvCircuitReadings(start, end)
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
                    EvChargingWh = evAt?.KiloWattHours ?? 0m,

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
