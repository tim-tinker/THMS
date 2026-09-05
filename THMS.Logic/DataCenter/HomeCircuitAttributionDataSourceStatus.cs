using THMS.Data.Stores;

namespace THMS.Logic.DataCenter
{
    public class HomeCircuitAttributionDataSourceStatus : IUpdateDataSourceStatus
    {
        private readonly IEnergyDataStore _energyStore;

        public string DataSourceName => "EV Attribution";

        public DateTime? LastRetrieval { get; private set; }

        public bool IsReadyForUpdate {  get; private set; }

        public HomeCircuitAttributionDataSourceStatus()
            : this(new DataStoreFactory().GetEnergyStore())
        {
        }

        public HomeCircuitAttributionDataSourceStatus(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void QueryStatus()
        {
            var lastAttribution = _energyStore.GetLatestHomeCircuitAttribution()?.Timestamp
                ?? DateTime.MinValue;

            LastRetrieval = lastAttribution == DateTime.MinValue ? null : lastAttribution;

            var lastSolarInterval = _energyStore.GetLatestSolarProductionInterval()?.Timestamp
                ?? DateTime.MinValue;

            var lastHomeCircuitReading = _energyStore.GetLatestHomeCircuitReading()?.Timestamp
                ?? DateTime.MinValue;

            IsReadyForUpdate = lastHomeCircuitReading > lastAttribution
                && lastSolarInterval > lastHomeCircuitReading;
        }
    }
}
