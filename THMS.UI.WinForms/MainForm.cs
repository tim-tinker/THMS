using THMS.UI.WinForms;

namespace THMS.UI
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, BaseDashboardForm> _dashboards;

        public MainForm()
        {
            InitializeComponent();
            _dashboards = new Dictionary<string, BaseDashboardForm>();

            InitializeModules();
        }

        // ---------------------------------------------------------
        // Create dashboard forms + view models
        // ---------------------------------------------------------
        private void InitializeModules()
        {
            _dashboards["Transportation"] = new TransportationDashboardForm();
            _dashboards["Energy"] = new EnergyDashboardForm();
            _dashboards["Finance"] = new FinanceDashboardForm();
            _dashboards["Vehicles"] = new VehicleListDashboardForm();

            foreach (var form in _dashboards.Values)
            {
                form.Visible = false;
                dashboardHostPanel.Controls.Add(form);
            }
        }

        // ---------------------------------------------------------
        // Show a dashboard when user clicks a navigation button
        // ---------------------------------------------------------
        private void ShowModule(string moduleName)
        {
            foreach (var form in _dashboards.Values)
                form.Visible = false;

            var dashboard = _dashboards[moduleName];
            dashboard.Visible = true;
            dashboard.RefreshDashboard();
        }

        // ---------------------------------------------------------
        // Navigation button handlers
        // ---------------------------------------------------------
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
    }
}
