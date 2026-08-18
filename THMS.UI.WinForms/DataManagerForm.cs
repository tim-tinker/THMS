using THMS.Data.Stores;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class DataManagerForm : BaseEmbeddedForm
    {
        private readonly IEnergyDataStore _energyStore;
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IFinanceDataStore _financeStore;
        private IDataManagerControl? _currentControl;

        private Dictionary<string, Control> _controls = [];

        public DataManagerForm(IEnergyDataStore energyStore, IVehicleDataStore vehicleStore, IFinanceDataStore financeStore)
        {
            InitializeComponent();
            _energyStore = energyStore;
            _vehicleStore = vehicleStore;
            _financeStore = financeStore;
            CreateControlDictionary();
        }

        private void CreateControlDictionary()
        {
            _controls["Solar"] = new SolarIntervalManagerControl(_energyStore) { Dock = DockStyle.Fill };
            _controls["Circuit"] = new HomeCircuitManagerControl(_energyStore) { Dock = DockStyle.Fill };
            _controls["Attribution"] = new HomeCircuitAttributionManagerControl(_energyStore) { Dock = DockStyle.Fill };
            _controls["Session"] = new EvChargeSessionManagerControl(_vehicleStore, _energyStore, _financeStore) { Dock = DockStyle.Fill };
            _controls["ElectricContract"] = new ElectricContractManagerControl(_financeStore) { Dock = DockStyle.Fill };
        }

        private void OnClickClose(object sender, EventArgs e)
        {
            Close();
        }

        private void ClearControls()
        {
            panelHost.Controls.Remove(_currentControl as Control);
            _currentControl = null;
        }

        private void OnClickTypeMenuItem(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            var controlName = menuItem?.Tag.ToString();
            if (string.IsNullOrEmpty(controlName)) return;

            ClearControls();

            var control = _controls[controlName];
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
    }
}
