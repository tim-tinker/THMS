using THMS.Data.Stores;
using THMS.Domain.Finance;

namespace THMS.Logic.Finance
{
    /// <summary>
    /// Computes cost summaries for commercial EV charging (e.g., ChargePoint).
    /// </summary>
    public class CommercialChargeCostEngine
    {
        private IFinanceDataStore? _financeStore;
        private IEnergyDataStore? _energyStore;

        public CommercialChargeCostEngine() { }

        public CommercialChargeCostEngine(
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore)
        {
            _financeStore = financeStore;
            _energyStore = energyStore;
        }

        public void SetStores(
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore)
        {
            _financeStore = financeStore;
            _energyStore = energyStore;
        }

        public CommercialChargeCostSummary ComputeSummary(DateTime start, DateTime end)
        {
            var costRecords = _financeStore
                .GetCommercialChargeCostRecords(start, end)
                .ToList();

            var energyRecords = _energyStore
                .GetEvCommercialChargeSessions(start, end)
                .ToList();

            decimal totalCost = costRecords.Sum(r => r.Cost);
            decimal totalKwh = energyRecords.Sum(r => r.KwhAdded) / 1000m;

            decimal avgCostPerKwh = totalKwh > 0 ? totalCost / totalKwh : 0;

            return new CommercialChargeCostSummary
            {
                Start = start,
                End = end,
                TotalCost = totalCost,
                TotalKwh = totalKwh,
            };
        }
    }

}
