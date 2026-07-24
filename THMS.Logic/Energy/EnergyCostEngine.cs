using THMS.Data.Energy;
using THMS.Domain.Energy;
using THMS.Domain.Finance.Billing;

namespace THMS.Logic.Energy
{
    /// <summary>
    /// Computes cost attribution for EV charging using billing economics.
    /// Handles partial billing data and late-arriving bills.
    /// </summary>
    public class EnergyCostEngine
    {
        private readonly EnergyIntervalStore _store;
        private readonly IReadOnlyCollection<ElectricUtilityBillCostInterval> _billing;

        public EnergyCostEngine(
            EnergyIntervalStore store,
            IReadOnlyCollection<ElectricUtilityBillCostInterval> billingIntervals)
        {
            _store = store;
            _billing = billingIntervals;
        }

        /// <summary>
        /// Finds the billing interval that covers the given timestamp.
        /// Returns null if billing data has not yet been ingested.
        /// </summary>
        private ElectricUtilityBillCostInterval? FindBilling(DateTime timestamp)
        {
            return _billing.FirstOrDefault(b => b.Covers(timestamp));
        }

        /// <summary>
        /// Computes cost attribution for a single timestamp.
        /// </summary>
        public EnergyCostResult ComputeCost(EnergyAttributionResult attribution)
        {
            var evIntervals = _store.GetEvIntervals(attribution.Timestamp);
            var billing = FindBilling(attribution.Timestamp);

            // Sum commercial charging cost for this timestamp
            decimal commercialCost = evIntervals
                .Where(e => e.IsCommercialCharging)
                .Sum(e => e.CommercialChargingCost ?? 0);

            // If no billing data, cost attribution is partial
            if (billing == null)
            {
                return new EnergyCostResult
                {
                    Timestamp = attribution.Timestamp,
                    EvChargingWh = attribution.EvChargingWh,
                    SolarAvoidedCost = 0,
                    BatteryValue = 0,
                    GridCost = commercialCost, // only commercial cost known
                    CommercialChargingCost = commercialCost,
                    IsPartial = true
                };
            }

            // Compute effective rates
            decimal totalImportWh = attribution.GridWh;
            decimal effectiveImportRate = totalImportWh > 0
                ? billing.GridImportCost / totalImportWh
                : 0;

            decimal totalExportWh = attribution.SolarWh; // avoided cost
            decimal effectiveExportRate = totalExportWh > 0
                ? billing.GridExportCredit / totalExportWh
                : 0;

            // Compute cost attribution
            decimal solarAvoidedCost = attribution.SolarWh * effectiveImportRate;
            decimal batteryValue = attribution.BatteryWh * effectiveImportRate;
            decimal gridCost = attribution.GridWh * effectiveImportRate;

            return new EnergyCostResult
            {
                Timestamp = attribution.Timestamp,
                EvChargingWh = attribution.EvChargingWh,
                SolarAvoidedCost = solarAvoidedCost,
                BatteryValue = batteryValue,
                GridCost = gridCost + commercialCost,
                CommercialChargingCost = commercialCost,
                IsPartial = false
            };
        }

        /// <summary>
        /// Computes cost attribution for all timestamps.
        /// </summary>
        public IReadOnlyCollection<EnergyCostResult> ComputeAllCosts(
            IReadOnlyCollection<EnergyAttributionResult> attributionResults)
        {
            var results = new List<EnergyCostResult>();

            foreach (var attr in attributionResults)
            {
                results.Add(ComputeCost(attr));
            }

            return results.AsReadOnly();
        }
    }
}
