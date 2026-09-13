using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class ElectricContractUpdater : IDataSourceUpdater
    {
        private readonly ElectricContractOrchestrator _orchestrator = new();

        public IDataSourceStatus Status { get; private set; } = new ElectricContractDataSourceStatus();

        public void UpdateDataSource() => UpdateDataSource(Form.ActiveForm);

        public void UpdateDataSource(IWin32Window? owner)
        {
            using var dataEntryForm = new ElectricContractDataEntryForm();
            var result = owner is null
                ? dataEntryForm.ShowDialog()
                : dataEntryForm.ShowDialog(owner);

            if (result == DialogResult.OK && dataEntryForm.Contract is not null)
                _orchestrator.Save(dataEntryForm.Contract);
        }
    }
}
