using System.Collections.ObjectModel;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Transportation;

namespace THMS.Logic.ViewModels
{
    public class VehicleListViewModel : BaseDashboardViewModel
    {
        private IVehicleDataStore _vehicleStore = null!;
        private IFinanceDataStore _financeStore = null!;
        private IEnergyDataStore _energyStore = null!;
        private TransportationCostAggregator _aggregator = null!;

        public ObservableCollection<VehicleListItemViewModel> Vehicles { get; }
            = new ObservableCollection<VehicleListItemViewModel>();

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public void SetStores(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore,
            TransportationCostAggregator aggregator)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _energyStore = energyStore;
            _aggregator = aggregator;
        }

        public override void Initialize()
        {
            PeriodStart = DateTime.Today.AddDays(-30);
            PeriodEnd = DateTime.Today;
            Load();
        }

        public override void Activate()
        {
            Load();
        }

        private void Load()
        {
            Vehicles.Clear();

            var allVehicles = _vehicleStore.GetAllVehicles();

            foreach (var v in allVehicles)
            {
                var item = new VehicleListItemViewModel(
                    v.Id,
                    v.Name,
                    v is VehicleEv);

                var summary = _aggregator.GetCostSummary(
                    v.Id,
                    PeriodStart,
                    PeriodEnd);

                item.ApplySummary(summary);

                Vehicles.Add(item);
            }
        }

        public void AddVehicle(VehicleBase vehicle)
        {
            _vehicleStore.AddVehicle(vehicle);
        }
    }
}
