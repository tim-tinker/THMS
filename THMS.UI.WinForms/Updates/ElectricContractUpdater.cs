using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.DataCenter;

namespace THMS.UI.WinForms.Updates
{
    public class ElectricContractUpdater : IDataSourceUpdater
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IEnergyDataStore _energyStore;
        private readonly IFinanceDataStore _financeStore;

        public IDataSourceStatus Status { get; private set; }

        public ElectricContractUpdater(IVehicleDataStore vehicleStore, IEnergyDataStore energyStore, IFinanceDataStore financeStore)
        {
            _vehicleStore = vehicleStore;
            _energyStore = energyStore;
            _financeStore = financeStore;
            Status = new ElectricContractDataSourceStatus(financeStore);
        }

        public void UpdateDataSource()
        {
            using var dataEntryForm = new ElectricContractDataEntryForm();

            if (dataEntryForm.ShowDialog() != DialogResult.OK)
                return;

            var contract = dataEntryForm.Contract;
            if (contract == null)
                return;
        }
    }
}
