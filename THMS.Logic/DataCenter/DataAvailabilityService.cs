using THMS.Logic.ViewModels;

namespace THMS.Logic.DataCenter
{
    public class DataAvailabilityService
    {
        private List<IDataSourceStatus> _dataSourceStatuses = [];

        public DataAvailabilityService()
            : this(
            [
                new SolarDataSourceStatus(),
                new HomeCircuitReadingDataSourceStatus(),
                new EvChargeSessionDataSourceStatus(),
                new HomeCircuitAttributionDataSourceStatus()
            ])
        {
        }

        public DataAvailabilityService(IEnumerable<IDataSourceStatus> dataSourceStatuses)
        {
            _dataSourceStatuses = dataSourceStatuses.ToList();
        }

        public DataCenterViewModel GetAvailability()
        {
            foreach (var item in _dataSourceStatuses) 
            {
                item.QueryStatus();
            }

            var vm = new DataCenterViewModel();
            vm.AddDataSourceStatuses(_dataSourceStatuses);

            return vm;
        }
    }
}
