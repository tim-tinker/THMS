using THMS.Data.Stores;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy
{
    /// <summary>
    /// Aggregates interval-level energy and cost data into monthly summaries.
    /// </summary>
    public class EnergyAggregationEngine
    {
        private readonly IEnergyDataStore _store;

        public EnergyAggregationEngine(IEnergyDataStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Aggregates attribution + cost results into monthly summaries.
        /// </summary>
        public IReadOnlyCollection<MonthlyEnergySummary> Aggregate(
            IReadOnlyCollection<HomeCircuitAttribution> attributionResults,
            IReadOnlyCollection<EnergyCostResult> costResults,
            DateTime start,
            DateTime end)
        {
            var summaries = new Dictionary<(int Year, int Month), MonthlyEnergySummary>();

            // ---------------------------------------------------------
            // 1. EV CHARGING ATTRIBUTION + COSTS
            // ---------------------------------------------------------
            var filteredAttr = attributionResults
                .Where(a => a.Timestamp >= start && a.Timestamp <= end);

            var filteredCost = costResults
                .Where(c => c.Timestamp >= start && c.Timestamp <= end)
                .ToDictionary(c => c.Timestamp);

            foreach (var attr in filteredAttr)
            {
                if (!filteredCost.TryGetValue(attr.Timestamp, out var cost))
                    continue; // or throw — depends on your data guarantees

                int year = attr.Timestamp.Year;
                int month = attr.Timestamp.Month;
                var key = (year, month);

                if (!summaries.ContainsKey(key))
                {
                    summaries[key] = new MonthlyEnergySummary
                    {
                        Year = year,
                        Month = month
                    };
                }

                var summary = summaries[key];

                // Energy totals
                summary.EvChargeWh += attr.TotalWh;
                summary.EvChargeSolarWh += attr.SolarWh;
                summary.EvChargeBatteryWh += attr.BatteryWh;
                summary.EvChargeGridWh += attr.GridWh;

                // Cost totals
                summary.SolarAvoidedCost += cost.SolarAvoidedCost;
                summary.BatteryValue += cost.BatteryValue;
                summary.GridCost += cost.GridCost;
                summary.CommercialChargeCost += cost.CommercialChargeCost;
            }

            // ---------------------------------------------------------
            // 2. HOME ENERGY FLOWS (SOLAR VENDOR INTERVALS)
            // ---------------------------------------------------------
            var homeIntervals = _store.GetSolarProductionIntervals(start, end);

            foreach (var home in homeIntervals)
            {
                var key = (home.Timestamp.Year, home.Timestamp.Month);

                if (!summaries.ContainsKey(key))
                {
                    summaries[key] = new MonthlyEnergySummary
                    {
                        Year = key.Year,
                        Month = key.Month
                    };
                }

                var summary = summaries[key];

                summary.SolarProducedWh += home.EnergyProducedWh;
                summary.SolarConsumedWh += home.EnergyConsumedWh;
                summary.GridImportedWh += home.ImportedFromGridWh;
                summary.GridExportedWh += home.ExportedToGridWh;
                summary.BatteryStoredWh += home.StoredInBatteriesWh;
                summary.BatteryDischargedWh += home.DischargedFromBatteriesWh;
            }

            // ---------------------------------------------------------
            // 3. Return sorted summaries
            // ---------------------------------------------------------
            return summaries.Values
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToList()
                .AsReadOnly();
        }
    }
}
