using THMS.Data.Energy;
using THMS.Domain.Energy;

namespace THMS.Logic.Energy
{
    /// <summary>
    /// Aggregates interval-level energy and cost data into monthly summaries.
    /// </summary>
    public class EnergyAggregationEngine
    {
        private readonly EnergyIntervalStore _store;

        public EnergyAggregationEngine(EnergyIntervalStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Aggregates attribution + cost results into monthly summaries.
        /// </summary>
        public IReadOnlyCollection<MonthlyEnergySummary> Aggregate(
            IReadOnlyCollection<EnergyAttributionResult> attributionResults,
            IReadOnlyCollection<EnergyCostResult> costResults)
        {
            var summaries = new Dictionary<(int Year, int Month), MonthlyEnergySummary>();

            foreach (var attr in attributionResults)
            {
                var cost = costResults.First(c => c.Timestamp == attr.Timestamp);

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
                summary.EvChargingWh += attr.EvChargingWh;
                summary.EvChargingSolarWh += attr.SolarWh;
                summary.EvChargingBatteryWh += attr.BatteryWh;
                summary.EvChargingGridWh += attr.GridWh;

                // Cost totals
                summary.SolarAvoidedCost += cost.SolarAvoidedCost;
                summary.BatteryValue += cost.BatteryValue;
                summary.GridCost += cost.GridCost;
                summary.CommercialChargingCost += cost.CommercialChargingCost;

                // Partial flag
                if (attr.IsPartial || cost.IsPartial)
                    summary.IsPartial = true;
            }

            // Add home energy flows (solar vendor data)
            foreach (var home in _store.GetHomeIntervals())
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

                summary.SolarProducedWh += home.SolarProducedWh;
                summary.SolarConsumedWh += home.SolarConsumedWh;
                summary.GridImportedWh += home.GridImportedWh;
                summary.GridExportedWh += home.GridExportedWh;
                summary.BatteryStoredWh += home.BatteryStoredWh;
                summary.BatteryDischargedWh += home.BatteryDischargedWh;
            }

            return summaries.Values.ToList().AsReadOnly();
        }
    }
}
