using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Transportation;

namespace THMS.Logic.ViewModels
{
    public class VehicleListViewModel : BaseDashboardViewModel
    {
        private IVehicleDataStore _vehicleStore = null!;
        private IFinanceDataStore _financeStore = null!;
        private TransportationCostAggregator _aggregator = null!;

        public BindingList<VehicleListItemViewModel> Vehicles { get; } = new();

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public void SetStores(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _aggregator = new TransportationCostAggregator(vehicleStore, financeStore);
        }

        public override void Initialize()
        {
            PeriodStart = DateTime.Today.AddDays(-30);

            // Store queries filter on Date <= PeriodEnd, so this has to be the end
            // of today rather than midnight or entries made today are excluded.
            PeriodEnd = DateTime.Today.AddDays(1).AddTicks(-1);
            Load();
        }

        public override void Activate()
        {
            Load();
        }

        private void Load()
        {
            if (_vehicleStore == null || _financeStore == null || _aggregator == null)
            {
                throw new InvalidOperationException("Stores and aggregator must be set before loading data.");
            }

            Vehicles.Clear();

            var allVehicles = _vehicleStore.GetAllVehicles();

            foreach (var v in allVehicles)
            {
                Vehicles.Add(CreateListItem(v));
            }
        }

        public void AddVehicle(VehicleBase vehicle)
        {
            _vehicleStore.UpsertVehicle(vehicle);
            Vehicles.Add(CreateListItem(vehicle));
        }

        private VehicleListItemViewModel CreateListItem(VehicleBase vehicle)
        {
            var item = new VehicleListItemViewModel(
                vehicle.Id,
                vehicle.Name,
                vehicle is VehicleEv);

            var summary = _aggregator.GetCostSummary(
                vehicle.Id,
                PeriodStart,
                PeriodEnd);

            item.ApplySummary(summary);
            return item;
        }
    }
}
