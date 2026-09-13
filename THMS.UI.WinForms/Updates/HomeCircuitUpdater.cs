using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class HomeCircuitUpdater : IDataSourceUpdater
    {
        public IDataSourceStatus Status { get; private set; } = new HomeCircuitReadingDataSourceStatus();

        public void UpdateDataSource()
        {
            using var dialog = new OpenFileDialog()
            {
                Title = "Select Home Circuit Reading File(s)",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            var orchestrator = new HomeCircuitReadingOrchestrator();
            orchestrator.Update(dialog.FileNames);
        }
    }
}
