using System;
using System.Collections.ObjectModel;
using System.Linq;
using THMS.Data.Stores;

namespace THMS.Logic.ViewModels
{
    public class GasPurchaseListViewModel
    {
        private readonly IFinanceDataStore _financeStore;

        public Guid VehicleId { get; }

        public ObservableCollection<GasPurchaseViewModel> Purchases { get; }
            = new ObservableCollection<GasPurchaseViewModel>();

        public GasPurchaseListViewModel(IFinanceDataStore financeStore, Guid vehicleId)
        {
            _financeStore = financeStore;
            VehicleId = vehicleId;

            Load();
        }

        public void Load()
        {
            Purchases.Clear();

            var items = _financeStore.GetGasPurchases(
                VehicleId,
                DateTime.MinValue,
                DateTime.MaxValue);

            foreach (var p in items)
                Purchases.Add(new GasPurchaseViewModel(_financeStore, p));
        }

        public GasPurchaseViewModel CreateNew()
        {
            return new GasPurchaseViewModel(_financeStore, VehicleId);
        }
    }
}
