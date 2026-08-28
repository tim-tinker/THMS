using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class ElectricContractUpdater : IDataSourceUpdater
    {
        private readonly ElectricContractOrchestrator _orchestrator = new();

        public IDataSourceStatus Status { get; private set; } = new ElectricContractDataSourceStatus();

        public void UpdateDataSource()
        {
            using var dataEntryForm = new ElectricContractDataEntryForm();

            if (DialogResult.OK == dataEntryForm.ShowDialog() && dataEntryForm.Contract is not null)
            {
                _orchestrator.Save(dataEntryForm.Contract);
            }
        }
    }
}
