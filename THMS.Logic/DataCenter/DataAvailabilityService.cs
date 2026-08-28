using THMS.Logic.ViewModels;

namespace THMS.Logic.DataCenter
{
    public class DataAvailabilityService
    {
        private List<IDataSourceStatus> _dataSourceStatuses = [];

        public DataAvailabilityService()
        {
            _dataSourceStatuses.Add(new SolarDataSourceStatus());
            _dataSourceStatuses.Add(new HomeCircuitReadingDataSourceStatus());
            _dataSourceStatuses.Add(new EvChargeSessionDataSourceStatus());
            _dataSourceStatuses.Add(new HomeCircuitAttributionDataSourceStatus());
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
