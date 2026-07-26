using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class VehicleListViewModel
    {
        private readonly TransportationDataStore _store;
        public TransportationDataStore Store => _store;

        public VehicleListViewModel(TransportationDataStore store)
        {
            _store = store;
            Refresh();
        }

        public List<VehicleBase> Vehicles { get; private set; } = new();
        public VehicleBase? SelectedVehicle { get; set; }

        public void Refresh()
        {
            Vehicles = _store.GetAllVehicles().ToList();
        }

        public void AddVehicle(VehicleBase vehicle)
        {
            _store.AddVehicle(vehicle);
            Refresh();
        }
    }
}
