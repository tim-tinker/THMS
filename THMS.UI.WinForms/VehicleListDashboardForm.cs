using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class VehicleListDashboardForm : BaseDashboardForm<VehicleListViewModel>
    {
        public VehicleListDashboardForm()
        {
            InitializeComponent();
        }

        // ---------------------------------------------------------
        // Bind controls to the ViewModel (called once by MainForm)
        // ---------------------------------------------------------
        protected override void BindControlsToViewModel()
        {
            LoadVehicles();
        }

        // ---------------------------------------------------------
        // Refresh dashboard (called by MainForm + user interactions)
        // ---------------------------------------------------------
        public override void RefreshDashboard()
        {
            ViewModel.Activate();
            LoadVehicles();
        }

        private void LoadVehicles()
        {
            vehicleGrid.DataSource = ViewModel.Vehicles
                .Select(v => new
                {
                    v.VehicleId,
                    v.Name,
                    v.IsEv,
                    v.CostPerMile,
                    v.TotalMiles,
                    v.TotalCost
                })
                .ToList();
        }

        private void vehicleGrid_SelectionChanged(object sender, EventArgs e)
        {
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            var form = new AddVehicleForm(ViewModel);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadVehicles();
            }
        }

        private void btnDetails_Click(object sender, EventArgs e)
        {
        }
    }
}
