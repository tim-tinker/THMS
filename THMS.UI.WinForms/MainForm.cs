using THMS.UI.WinForms;

namespace THMS.UI
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, BaseDashboardForm> _dashboards = new();

        /// <summary>Designer only.</summary>
        public MainForm()
        {
            InitializeComponent();
        }

        public MainForm(
            TransportationDashboardForm transportationDashboard,
            EnergyDashboardForm energyDashboard,
            FinanceDashboardForm financeDashboard,
            VehicleListDashboardForm vehicleListDashboard) : this()
        {
            _dashboards["Transportation"] = transportationDashboard;
            _dashboards["Energy"] = energyDashboard;
            _dashboards["Finance"] = financeDashboard;
            _dashboards["Vehicles"] = vehicleListDashboard;

            foreach (var form in _dashboards.Values)
            {
                form.ConfigureAsEmbeddedDashboard();
                form.Dock = DockStyle.Fill;
                form.Visible = false;
                dashboardHostPanel.Controls.Add(form);
                form.InitializeDashboard();
            }
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
    }
}
