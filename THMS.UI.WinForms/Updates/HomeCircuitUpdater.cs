using THMS.Data.Stores;
using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class HomeCircuitUpdater : IDataSourceUpdater
    {
        private readonly IEnergyDataStore _energyStore;
        public IDataSourceStatus Status { get; private set; }
        public HomeCircuitUpdater(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
            Status = new HomeCircuitReadingDataSourceStatus(energyStore);
        }
        public void UpdateDataSource()
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var orchestrator = new HomeCircuitReadingOrchestrator(_energyStore);
            orchestrator.Update(dialog.FileName);
        }
    }
}
