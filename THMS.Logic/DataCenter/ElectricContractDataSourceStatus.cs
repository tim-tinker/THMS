using THMS.Data.Stores;

namespace THMS.Logic.DataCenter
{
    public class ElectricContractDataSourceStatus : IUpdateDataSourceStatus
    {
        private readonly IFinanceDataStore _financeStore;

        public string DataSourceName => "Electric Contract";

        public DateTime? LastRetrieval { get; private set; }

        public bool IsReadyForUpdate { get; private set; }

        public ElectricContractDataSourceStatus(IFinanceDataStore financeStore)
        {
            _financeStore = financeStore;
        }

        public void QueryStatus()
        {
            var lastContract = _financeStore.GetLatestElectricContract();

            LastRetrieval = lastContract?.EndDate;

            IsReadyForUpdate = (lastContract?.EndDate ?? DateTime.MinValue) < DateTime.Today;
        }
    }
}
