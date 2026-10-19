using THMS.Data.Stores;
using THMS.Domain.Finance;

namespace THMS.Logic.Orchestrators
{
    public class ElectricContractOrchestrator : BaseOrchestrator
    {
        private readonly IFinanceDataStore _financeStore;

        public ElectricContractOrchestrator()
            : this(new DataStoreFactory().GetFinanceStore())
        {
        }

        public ElectricContractOrchestrator(IFinanceDataStore financeStore)
        {
            _financeStore = financeStore;
        }

        public void Save(ElectricContract contract)
        {
            if (contract.Id == Guid.Empty)
                contract.Id = Guid.NewGuid();

            contract.StartDate = contract.StartDate.Date;
            contract.EndDate = contract.EndDate.Date;

            _financeStore.UpsertElectricContract(contract);
        }

        // Lists contracts whose start–end range covers the selected calendar period.
        public IEnumerable<ElectricContract> GetElectricContracts(string period)
        {
            var (start, end) = CoverageRange(period);
            return _financeStore.GetElectricContracts(start, end)
                .OrderByDescending(c => c.StartDate);
        }

        private static (DateTime Start, DateTime End) CoverageRange(string period)
        {
            var today = DateTime.Today;
            return period switch
            {
                "Year" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31)),
                "Lifetime" => (DateTime.MinValue, DateTime.MaxValue),
                _ => CalendarMonth(today)
            };
        }

        private static (DateTime Start, DateTime End) CalendarMonth(DateTime day)
        {
            var start = new DateTime(day.Year, day.Month, 1);
            return (start, start.AddMonths(1).AddDays(-1));
        }

    }
}
