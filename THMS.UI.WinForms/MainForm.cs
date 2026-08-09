using THMS.UI.WinForms;

namespace THMS.UI
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, BaseDashboardForm> _dashboards = new();
        private DataCenterForm _dataCenterForm;

        /// <summary>Designer only.</summary>
        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(
            TransportationDashboardForm transportationDashboard,
            EnergyDashboardForm energyDashboard,
            FinanceDashboardForm financeDashboard,
            VehicleListDashboardForm vehicleListDashboard,
            DataCenterForm dataCenterForm
            ) 
            : this()
        {
            _dashboards["Transportation"] = transportationDashboard;
            _dashboards["Energy"] = energyDashboard;
            _dashboards["Finance"] = financeDashboard;
            _dashboards["Vehicles"] = vehicleListDashboard;
            _dataCenterForm = dataCenterForm;

            foreach (var form in _dashboards.Values)
            {
                form.ConfigureAsEmbeddedDashboard();
                form.Visible = false;
                dashboardHostPanel.Controls.Add(form);
                form.InitializeDashboard();
            }

            _dataCenterForm.ConfigureAsEmbeddedForm();
            dashboardHostPanel.Controls.Add(_dataCenterForm);
        }

        private void ShowModule(string moduleName)
        {
            foreach (var form in _dashboards.Values)
                form.Visible = false;

            var dashboard = _dashboards[moduleName];
            dashboard.Visible = true;
            dashboard.RefreshDashboard();
        }

        private void btnTransportation_Click(object sender, EventArgs e)
        {
            ShowModule("Transportation");
        }

        private void btnEnergy_Click(object sender, EventArgs e)
        {
            ShowModule("Energy");
        }

        private void btnFinance_Click(object sender, EventArgs e)
        {
            ShowModule("Finance");
        }

        private void OnClickVehicles(object sender, EventArgs e)
        {
            ShowModule("Vehicles");
        }

        private void OnClickDataCenter(object sender, EventArgs e)
        {
            ShowFormInMainPanel(_dataCenterForm);
        }

        private void ShowFormInMainPanel(Form form)
        {
            foreach (var dashboard in _dashboards.Values)
                dashboard.Visible = false;

            form.Visible = true;
        }
    }
}
