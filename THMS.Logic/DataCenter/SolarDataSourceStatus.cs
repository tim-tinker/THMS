using THMS.Data.Stores;

namespace THMS.Logic.DataCenter
{
    public class SolarDataSourceStatus : IPeriodicDataSourceStatus
    {
        private readonly IEnergyDataStore _energyStore;

        public string DataSourceName => "Solar Data";

        public DateTime NextExpectedRetrieval { get; private set; }

        public DateTime? LastRetrieval { get; private set; }

        public SolarDataSourceStatus()
            : this(new DataStoreFactory().GetEnergyStore())
        {
        }

        public SolarDataSourceStatus(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void QueryStatus()
        {
            var lastInterval = _energyStore.GetLatestSolarProductionInterval();

            LastRetrieval= lastInterval?.Timestamp;
            NextExpectedRetrieval = LastRetrieval?.AddMonths(1) ?? DateTime.Today;
        }
    }
}
