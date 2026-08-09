using THMS.Data.Stores;

namespace THMS.Logic.DataCenter
{
    public class EvAttributionDataSourceStatus : IUpdateDataSourceStatus
    {
        private readonly IEnergyDataStore _energyStore;

        public string DataSourceName => "EV Attribution";

        public DateTime? LastRetrieval { get; private set; }

        public bool IsReadyForUpdate {  get; private set; }

        public EvAttributionDataSourceStatus(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
        }

        public void QueryStatus()
        {
            var lastAttribution = _energyStore.GetLatestEvAttribution()?.Timestamp
                ?? DateTime.MinValue;

            LastRetrieval = lastAttribution == DateTime.MinValue ? null : lastAttribution;

            var lastSolarInterval = _energyStore.GetLatestSolarVendorInterval()?.Timestamp
                ?? DateTime.MinValue;

            var lastEvCircuitReading = _energyStore.GetLatestEvCircuitReading()?.Timestamp
                ?? DateTime.MinValue;

            IsReadyForUpdate = lastEvCircuitReading > lastAttribution
                && lastSolarInterval > lastEvCircuitReading;
        }
    }
}
