using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Finance;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class ElectricContractManagerControl : UserControl, IDataManagerControl
    {
        private IFinanceDataStore _financeStore;
        private ElectricContractOrchestrator _orchestrator;
        private string _currentPeriod;
        private BindingList<ElectricContract> _contracts;

        public ElectricContractManagerControl()
        {
            InitializeComponent();
            _gridContracts.AutoGenerateColumns = false;
        }

        public ElectricContractManagerControl(IFinanceDataStore financeStore)
            : this()
        {
            _financeStore = financeStore;
            _orchestrator = new ElectricContractOrchestrator(financeStore);
        }

        // ---------------------------------------------------------
        // IDataManagerControl implementation
        // ---------------------------------------------------------
        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            _currentPeriod = period;
            _contracts = new BindingList<ElectricContract>(_orchestrator.GetElectricContracts(period).ToList());
            _gridContracts.DataSource = _contracts;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            SetGridDataSource("Month");
        }

        private void OnVisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                SetGridDataSource(_currentPeriod);
            }
        }
    }
}
