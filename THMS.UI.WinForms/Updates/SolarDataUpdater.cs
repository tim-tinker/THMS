using System.Windows.Forms.Design;
using THMS.Data.Stores;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.DataCenter;
using THMS.Logic.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class SolarDataUpdater : IDataSourceUpdater
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly SolarIntervalOrchestrator _intervalOrchestrator;

        public IDataSourceStatus Status { get; private set; }

        public SolarDataUpdater(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
            _intervalOrchestrator = new SolarIntervalOrchestrator(energyStore);
            Status = new SolarDataSourceStatus(energyStore);
        }

        public void UpdateDataSource()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Import Enphase Solar Data"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _intervalOrchestrator.Update(dialog.FileName);

                if (string.IsNullOrEmpty(_intervalOrchestrator.ErrorMessage))
                {
                    MessageBox.Show($"{_intervalOrchestrator.IntervalCount} solar data imported successfully for {_intervalOrchestrator.StartDate.Date} to {_intervalOrchestrator.EndDate.Date}.", "THMS",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Error importing solar data: {_intervalOrchestrator.ErrorMessage}", "THMS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
