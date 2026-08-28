using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class SolarDataUpdater : IDataSourceUpdater
    {
        private readonly SolarIntervalOrchestrator _intervalOrchestrator = new();

        public IDataSourceStatus Status { get; private set; } = new SolarDataSourceStatus();

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
