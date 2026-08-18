using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Domain.Transportation;

namespace THMS.Logic.Orchestrators
{
    public class ElectricContractOrchestrator : BaseOrchestrator
    {
        private readonly IFinanceDataStore _financeStore;

        public ElectricContractOrchestrator(IFinanceDataStore financeStore)
        {
            _financeStore = financeStore;
        }

        public void Save(ElectricContract contract)
        {
            if (contract.Id == Guid.Empty)
                contract.Id = Guid.NewGuid();

            _financeStore.UpsertElectricContract(contract);
        }

        // ---------------------------------------------------------
        // GET SESSIONS (period-based)
        // ---------------------------------------------------------
        public IEnumerable<ElectricContract> GetElectricContracts(string period)
        {
            var latest = _financeStore.GetLatestElectricContract();
            if (latest is null)
                return Array.Empty<ElectricContract>();

            var end = latest.EndDate;
            var start = GetStartDate(end, period);

            return _financeStore.GetElectricContracts(start, end);
        }

    }
}
