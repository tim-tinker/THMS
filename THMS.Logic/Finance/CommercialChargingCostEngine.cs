using THMS.Data.Stores;
using THMS.Domain.Finance;

namespace THMS.Logic.Finance
{
    /// <summary>
    /// Computes cost summaries for commercial EV charging (e.g., ChargePoint).
    /// </summary>
    public class CommercialChargingCostEngine
    {
        private readonly IFinanceDataStore _financeStore;
        private readonly IEnergyDataStore _energyStore;

        public CommercialChargingCostEngine(
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore)
        {
            _financeStore = financeStore;
            _energyStore = energyStore;
        }

        public CommercialChargingCostSummary ComputeSummary(DateTime start, DateTime end)
        {
            var costRecords = _financeStore
                .GetCommercialChargingCostRecords()
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .ToList();

            var energyRecords = _energyStore
                .GetEvChargingSessions()
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .ToList();

            decimal totalCost = costRecords.Sum(r => r.Cost);
            decimal totalKwh = energyRecords.Sum(r => r.EvChargingWh) / 1000m;

            decimal avgCostPerKwh = totalKwh > 0 ? totalCost / totalKwh : 0;

            return new CommercialChargingCostSummary
            {
                Start = start,
                End = end,
                TotalCost = totalCost,
                TotalKwh = totalKwh,
                AverageCostPerKwh = avgCostPerKwh
            };
        }
    }

}
