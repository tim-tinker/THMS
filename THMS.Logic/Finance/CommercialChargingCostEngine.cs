using THMS.Data.Stores;
using THMS.Domain.Finance;

namespace THMS.Logic.Finance
{
    /// <summary>
    /// Computes cost summaries for commercial EV charging (e.g., ChargePoint).
    /// </summary>
    public class CommercialChargingCostEngine
    {
        private IFinanceDataStore? _financeStore;
        private IEnergyDataStore? _energyStore;

        public CommercialChargingCostEngine() { }

        public CommercialChargingCostEngine(
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

        public CommercialChargingCostSummary ComputeSummary(DateTime start, DateTime end)
        {
            var costRecords = _financeStore
                .GetCommercialChargingCostRecords(start, end)
                .ToList();

            var energyRecords = _energyStore
                .GetEvCommercialChargingSessions(start, end)
                .ToList();

            decimal totalCost = costRecords.Sum(r => r.Cost);
            decimal totalKwh = energyRecords.Sum(r => r.KwhAdded) / 1000m;

            decimal avgCostPerKwh = totalKwh > 0 ? totalCost / totalKwh : 0;

            return new CommercialChargingCostSummary
            {
                Start = start,
                End = end,
                TotalCost = totalCost,
                TotalKwh = totalKwh,
            };
        }
    }

}
