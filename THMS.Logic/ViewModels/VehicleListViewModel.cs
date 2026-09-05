using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Transportation;

namespace THMS.Logic.ViewModels
{
    public class VehicleListViewModel : BaseDashboardViewModel
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly TransportationCostAggregator _aggregator;

        public BindingList<VehicleListItemViewModel> Vehicles { get; } = new();

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public VehicleListViewModel()
            : this(
                new DataStoreFactory().GetVehicleStore(),
                new DataStoreFactory().GetFinanceStore())
        {
        }

        public VehicleListViewModel(IVehicleDataStore vehicleStore, IFinanceDataStore financeStore)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _aggregator = new TransportationCostAggregator(_vehicleStore, _financeStore);
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
