using THMS.Domain.Transportation;
using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class EvChargeSessionUpdater : IDataSourceUpdater
    {
        private readonly EvChargeSessionOrchestrator _orchestrator = new();

        public IDataSourceStatus Status { get; private set; } = new EvChargeSessionDataSourceStatus();

        public void UpdateDataSource()
        {
            if (ResolveEvVehicle() is not VehicleEv vehicle)
                return;

            using var form = new EvChargeSessionForm(vehicle);
            if (DialogResult.OK == form.ShowDialog() && form.SavedSession is not null)
            {
                _orchestrator.Save(form.SavedSession);
            }
        }

        private VehicleEv? ResolveEvVehicle()
        {
            var vehicles = _orchestrator.GetEvVehicles().ToList();
            if (vehicles.Count == 0)
            {
                MessageBox.Show("Add an EV before adding a charge session.",
                    "Add EV Charge Session", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (vehicles.Count == 1)
                return vehicles[0];

            using var selectForm = new VehicleSelectionForm(vehicles);
            if (selectForm.ShowDialog() != DialogResult.OK)
                return null;

            return selectForm.SelectedVehicle as VehicleEv;
        }
    }
}
