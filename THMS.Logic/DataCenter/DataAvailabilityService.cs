using THMS.Data.Stores;
using THMS.Logic.ViewModels;

namespace THMS.Logic.DataCenter
{
    public class DataAvailabilityService
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly IVehicleDataStore _vehicleStore;

        private List<IDataSourceStatus> _dataSourceStatuses = [];

        public DataAvailabilityService(
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore,
            IVehicleDataStore vehicleStore)
        {
            _energyStore = energyStore;
            _financeStore = financeStore;
            _vehicleStore = vehicleStore;

            _dataSourceStatuses.Add(new SolarDataSourceStatus(_energyStore));
            _dataSourceStatuses.Add(new EvCircuitReadingDataSourceStatus(_energyStore));
            _dataSourceStatuses.Add(new EvChargeSessionDataSourceStatus(_vehicleStore));
            _dataSourceStatuses.Add(new EvAttributionDataSourceStatus(_energyStore));
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
