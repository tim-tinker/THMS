using System;
using System.Collections.ObjectModel;
using System.Linq;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Transportation;
using THMS.Logic.ViewModels;

namespace THMS.UI.ViewModels
{
    public class VehicleListViewModel
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly IEnergyDataStore _energyStore;
        private readonly TransportationCostAggregator _aggregator;

        public ObservableCollection<VehicleListItemViewModel> Vehicles { get; }
            = new ObservableCollection<VehicleListItemViewModel>();

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public VehicleListViewModel(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore,
            TransportationCostAggregator aggregator)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _energyStore = energyStore;
            _aggregator = aggregator;

            PeriodStart = DateTime.Today.AddDays(-30);
            PeriodEnd = DateTime.Today;

            Load();
        }

        public void Load()
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

        public void Refresh()
        {
            Load();
        }
    }
}
