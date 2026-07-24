using THMS.Data.Energy;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy
{
    /// <summary>
    /// Computes attribution of EV charging energy to solar, battery, and grid sources.
    /// Works with partial ingestion and late-arriving data.
    /// </summary>
    public class EnergyAttributionEngine
    {
        private readonly EnergyIntervalStore _store;

        public EnergyAttributionEngine(EnergyIntervalStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Computes attribution for a single timestamp.
        /// </summary>
        public EnergyAttributionResult Attribute(DateTime timestamp)
        {
            var evIntervals = _store.GetEvIntervals(timestamp);
            var home = _store.GetHomeInterval(timestamp);

            decimal evTotal = evIntervals.Sum(e => e.EvChargingWh);

            // If no EV charging occurred, return empty attribution.
            if (evTotal == 0)
            {
                return new EnergyAttributionResult
                {
                    Timestamp = timestamp,
                    EvChargingWh = 0,
                    SolarWh = 0,
                    BatteryWh = 0,
                    GridWh = 0,
                    IsPartial = home == null
                };
            }

            // If solar vendor data is missing, attribution is partial.
            if (home == null)
            {
                return new EnergyAttributionResult
                {
                    Timestamp = timestamp,
                    EvChargingWh = evTotal,
                    SolarWh = 0,
                    BatteryWh = 0,
                    GridWh = evTotal, // assume grid until solar data arrives
                    IsPartial = true
                };
            }

            // Attribution logic:
            // Priority order:
            // 1. SolarConsumedWh
            // 2. BatteryDischargedWh
            // 3. GridImportedWh

            decimal remaining = evTotal;

            decimal solar = Math.Min(remaining, home.SolarConsumedWh);
            remaining -= solar;

            decimal battery = Math.Min(remaining, home.BatteryDischargedWh);
            remaining -= battery;

            decimal grid = Math.Min(remaining, home.GridImportedWh);
            remaining -= grid;

            // If remaining > 0, something is off in the vendor data.
            // Assign remainder to grid.
            grid += remaining;

            return new EnergyAttributionResult
            {
                Timestamp = timestamp,
                EvChargingWh = evTotal,
                SolarWh = solar,
                BatteryWh = battery,
                GridWh = grid,
                IsPartial = false
            };
        }

        /// <summary>
        /// Computes attribution for all timestamps that have any energy data.
        /// </summary>
        public IReadOnlyCollection<EnergyAttributionResult> AttributeAll()
        {
            var results = new List<EnergyAttributionResult>();

            foreach (var ts in _store.GetAllTimestamps())
            {
                results.Add(Attribute(ts));
            }

            return results.AsReadOnly();
        }
    }
}
