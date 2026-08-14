using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.DataCenter;

namespace THMS.UI.WinForms.Updates
{
    public class EvChargeSessionUpdater : IDataSourceUpdater
    {
        private readonly IVehicleDataStore _vehicleStore;

        public IDataSourceStatus Status { get; private set; }

        public EvChargeSessionUpdater(IVehicleDataStore energyStore)
        {
            _vehicleStore = energyStore;
            Status = new EvChargeSessionDataSourceStatus(energyStore);
        }

        public void UpdateDataSource()
        {
            var vehicles = _vehicleStore.GetAllVehicles()
                .OfType<VehicleEv>()        // or VehicleIce, or any T
                .ToList();

            using var selectForm = new VehicleSelectionForm(vehicles);

            if (selectForm.ShowDialog() != DialogResult.OK)
                return;

            var vehicle = selectForm.SelectedVehicle as VehicleEv;
            if (vehicle == null)
                return;

            using var form = new EvChargeSessionForm(_vehicleStore, vehicle);
            form.ShowDialog();
        }
    }
}
