using THMS.Data.Stores;
using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class EvCircuitUpdater : IDataSourceUpdater
    {
        private readonly IEnergyDataStore _energyStore;
        public IDataSourceStatus Status { get; private set; }
        public EvCircuitUpdater(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
            Status = new EvCircuitReadingDataSourceStatus(energyStore);
        }
        public void UpdateDataSource()
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var orchestrator = new EvCircuitOrchestrator(_energyStore);
            orchestrator.Update(dialog.FileName);
        }
    }
}
