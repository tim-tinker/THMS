using THMS.Data.Stores;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class DataManagerForm : BaseEmbeddedForm
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;
        private readonly IAccountDataStore _accountStore;
        private readonly ITransactionDataStore _transactionStore;
        private IDataManagerControl? _currentControl;

        private Dictionary<string, Control> _controls = [];

        public DataManagerForm(IEnergyDataStore energyStore, IVehicleDataStore vehicleStore, IFinanceDataStore financeStore, 
            IAccountDataStore accountStore, ITransactionDataStore transactionStore)
        {
            InitializeComponent();
            _energyStore = energyStore;
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            _accountStore = accountStore;
            _transactionStore = transactionStore;
            CreateControlDictionary();
        }

        private void CreateControlDictionary()
        {
            AddControl(new SolarIntervalManagerControl(_energyStore), "Solar");
            AddControl(new HomeCircuitManagerControl(_energyStore), "Home Circuit");
            AddControl(new HomeCircuitAttributionManagerControl(_energyStore), "Circuit Attribution");
            AddControl(new EvChargeSessionManagerControl(_vehicleStore, _energyStore, _financeStore), "EV Charge Session");
            AddControl(new ElectricContractManagerControl(_financeStore), "Electric Contracts");
            AddControl(new TransactionManagerControl(_transactionStore, _accountStore), "Accounts and Transactions");
        }

        private void AddControl(UserControl control, string label)
        {
            control.Dock = DockStyle.Fill;
            _controls[label] = control;
            var menuItem = dataTypeToolStripMenuItem.DropDownItems.Add(label);
            menuItem.Click += OnClickTypeMenuItem;
        }

        private void ClearControls()
        {
            panelHost.Controls.Remove(_currentControl as Control);
            _currentControl = null;
        }

        private void OnClickTypeMenuItem(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            var menuLabel = menuItem?.Text;
            if (string.IsNullOrEmpty(menuLabel)) return;

            ClearControls();

            var control = _controls[menuLabel];
            _currentControl = control as IDataManagerControl;

            panelHost.Controls.Add(control);
            control.BringToFront();
        }

        private void OnClickViewMonth(object sender, EventArgs e)
        {
            _currentControl?.SetGridDataSource("Month");
        }

        private void OnClickViewYear(object sender, EventArgs e)
        {
            _currentControl?.SetGridDataSource("Year");
        }

        private void OnClickViewLifetime(object sender, EventArgs e)
        {
            _currentControl?.SetGridDataSource("Lifetime");
        }

        private void OnClickEditAddAction(object sender, EventArgs e)
        {

        }

        private void OnClickEditEditAction(object sender, EventArgs e)
        {

        }

        private void OnClickEditDeleteAction(object sender, EventArgs e)
        {

        }

        private void OnClickClose(object sender, EventArgs e)
        {
            Close();
        }
    }
}