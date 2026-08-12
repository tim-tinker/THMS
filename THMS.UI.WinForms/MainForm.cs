using THMS.UI.WinForms;

namespace THMS.UI
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, BaseDashboardForm> _dashboards = [];
        private readonly Dictionary<string, BaseEmbeddedForm> _embeddedForms = [];

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
            DataCenterForm dataCenterForm,
            DataManagerForm dataManagerForm
            ) 
            : this()
        {
            _dashboards["Transportation"] = transportationDashboard;
            _dashboards["Energy"] = energyDashboard;
            _dashboards["Finance"] = financeDashboard;
            _dashboards["Vehicles"] = vehicleListDashboard;
            _embeddedForms["Data Center"] = dataCenterForm;
            _embeddedForms["Data Manager"] = dataManagerForm;

            foreach (var form in _dashboards.Values)
            {
                form.ConfigureAsEmbeddedDashboard();
                form.Visible = false;
                dashboardHostPanel.Controls.Add(form);
                form.InitializeDashboard();
            }

            foreach (var form in _embeddedForms.Values)
            {
                form.ConfigureAsEmbeddedForm();
                form.Visible=false;
                dashboardHostPanel.Controls.Add(form);
            }
        }

        private void ShowModule(string moduleName)
        {
            HideAllEmbeddedForms();

            var dashboard = _dashboards[moduleName];
            dashboard.Visible = true;
            dashboard.RefreshDashboard();
        }

        private void HideAllEmbeddedForms()
        {
            foreach (var form in _dashboards.Values)
            {
                form.Visible = false;
            }

            foreach(var form in _embeddedForms.Values)
            {
                form.Visible = false;
            }
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
            ShowFormInMainPanel("Data Center");
        }

        private void OnClickDataManager(object sender, EventArgs e)
        {
            ShowFormInMainPanel("Data Manager");
        }

        private void ShowFormInMainPanel(string formName)
        {
            HideAllEmbeddedForms();

            var dashboard = _embeddedForms[formName];
            dashboard.Visible = true;
        }
    }
}
