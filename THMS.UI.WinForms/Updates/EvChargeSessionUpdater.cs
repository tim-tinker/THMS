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
            var vehicles = _orchestrator.GetEvVehicles().ToList();

            using var selectForm = new VehicleSelectionForm(vehicles);

            if (selectForm.ShowDialog() != DialogResult.OK)
                return;

            var vehicle = selectForm.SelectedVehicle as VehicleEv;
            if (vehicle == null)
                return;

            using var form = new EvChargeSessionForm(vehicle);
            if (DialogResult.OK == form.ShowDialog() && form.SavedSession is not null)
            {
                _orchestrator.Save(form.SavedSession);
            }
        }
    }
}
