using THMS.Data.Stores;

namespace THMS.Logic.DataCenter
{
    public class EvChargeSessionDataSourceStatus : IDataSourceStatus
    {
        private readonly IVehicleDataStore _vehicleStore;

        public string DataSourceName => "EV Charge Sessions";

        public DateTime? LastRetrieval { get; private set; }

        public EvChargeSessionDataSourceStatus(IVehicleDataStore vehicleStore)
        {
            _vehicleStore = vehicleStore;
        }

        public void QueryStatus()
        {
            var lastReading = _vehicleStore.GetLatestBaseEvChargeSession();

            LastRetrieval = lastReading?.EndTime;
        }

    }
}
