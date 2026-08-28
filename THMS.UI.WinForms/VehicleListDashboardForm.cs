using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class VehicleListDashboardForm : BaseDashboardForm
    {
        private VehicleListViewModel ViewModel { get; set; } = null!;

        protected VehicleListItemViewModel? SelectedVehicle =>
            vehicleGrid.SelectedRows.Count > 0
                ? vehicleGrid.SelectedRows[0].DataBoundItem as VehicleListItemViewModel
                : null;

        public VehicleListDashboardForm()
        {
            InitializeComponent();
        }

        public override void InitializeDashboard()
        {
            ViewModel = new VehicleListViewModel();
            ViewModel.Initialize();
            vehicleGrid.DataSource = ViewModel.Vehicles;
            RefreshDashboard();
        }

        public override void RefreshDashboard()
        {
            // BindingList notifies the grid on Clear/Add; no DataSource reset needed.
            ViewModel.Activate();
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            using var form = new AddVehicleForm(ViewModel);
            form.ShowDialog(this);
            // AddVehicle appends to Vehicles; the grid updates via BindingList.
        }

        private void btnDetails_Click(object sender, EventArgs e)
        {
            if (SelectedVehicle != null)
            {
                var form = new VehicleDetailForm(SelectedVehicle.VehicleId);

                form.ShowDialog(this);

                // The detail form can add mileage and charging records, so the
                // summaries have to be recomputed even when it is just closed.
                RefreshDashboard();
            }
            else
            {
                MessageBox.Show("Please select a vehicle.");
            }
        }
    }
}
