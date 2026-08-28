using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class DataManagerForm : BaseEmbeddedForm
    {
        private IDataManagerControl? _currentControl;

        private Dictionary<string, Control> _controls = [];

        public DataManagerForm()
        {
            InitializeComponent();
            CreateControlDictionary();
        }

        private void CreateControlDictionary()
        {
            AddControl(new SolarIntervalManagerControl(), "Solar");
            AddControl(new HomeCircuitManagerControl(), "Home Circuit");
            AddControl(new HomeCircuitAttributionManagerControl(), "Circuit Attribution");
            AddControl(new EvChargeSessionManagerControl(), "EV Charge Session");
            AddControl(new ElectricContractManagerControl(), "Electric Contracts");
            AddControl(new TransactionManagerControl(), "Accounts and Transactions");
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