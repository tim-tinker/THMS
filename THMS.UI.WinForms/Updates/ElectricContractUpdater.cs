using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.DataCenter;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Updates
{
    public class ElectricContractUpdater : IDataSourceUpdater
    {
        private readonly ElectricContractOrchestrator _orchestrator;

        public IDataSourceStatus Status { get; private set; }

        public ElectricContractUpdater(IFinanceDataStore financeStore)
        {
            Status = new ElectricContractDataSourceStatus(financeStore);
            _orchestrator = new ElectricContractOrchestrator(financeStore);
        }

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
