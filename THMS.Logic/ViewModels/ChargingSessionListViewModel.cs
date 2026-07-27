using System;
using System.Collections.ObjectModel;
using System.Linq;
using THMS.Data.Stores;

namespace THMS.Logic.ViewModels
{
    public class ChargingSessionListViewModel
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly IEnergyDataStore _energyStore;

        public Guid VehicleId { get; }

        public ObservableCollection<ChargingSessionViewModel> Sessions { get; }
            = new ObservableCollection<ChargingSessionViewModel>();

        public ChargingSessionListViewModel(
            IVehicleDataStore vehicleStore,
            IFinanceDataStore financeStore,
            IEnergyDataStore energyStore,
            Guid vehicleId)
        {
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _energyStore = energyStore;
            VehicleId = vehicleId;

            Load();
        }

        public void Load()
        {
            Sessions.Clear();

            var items = _vehicleStore.GetEvChargingSessions(
                VehicleId,
                DateTime.MinValue,
                DateTime.MaxValue);

            foreach (var s in items)
            {
                Sessions.Add(new ChargingSessionViewModel(
                    _vehicleStore,
                    _financeStore,
                    _energyStore,
                    s));
            }
        }
    }
}
