using THMS.Data.Stores;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class DataManagerForm : BaseEmbeddedForm
    {
        private readonly IEnergyDataStore _energyStore;
        private IDataManagerControl? _currentControl;

        public DataManagerForm(IEnergyDataStore energyStore)
        {
            _energyStore = energyStore;
            InitializeComponent();
        }

        private void OnClickClose(object sender, EventArgs e)
        {
            Close();
        }

        private void OnClickSolarType(object sender, EventArgs e)
        {
            var control = new SolarIntervalManagerControl(_energyStore) { Dock = DockStyle.Fill };
            _currentControl = control;

            panelHost.Controls.Add(control);
            control.BringToFront();
        }

        private void OnClickHomeCircuitType(object sender, EventArgs e)
        {
            var control = new HomeCircuitManagerControl(_energyStore) { Dock = DockStyle.Fill };
            _currentControl = control;

            panelHost.Controls.Add(control);
            control.BringToFront();
        }

        private void OnClickHomeCircuitAttribution(object sender, EventArgs e)
        {
            var control = new HomeCircuitAttributionManagerControl(_energyStore) { Dock = DockStyle.Fill };
            _currentControl = control;

            panelHost.Controls.Add(control);
            control.BringToFront();
        }

        private void OnClickEvChargeSessionType(object sender, EventArgs e)
        {

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
