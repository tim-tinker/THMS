using THMS.Domain.Transportation;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class TransportationDashboardForm : BaseDashboardForm
    {
        private TransportationDashboardViewModel _vm;

        public TransportationDashboardForm()
        {
            InitializeComponent();
        }

        protected override void OnBindViewModel(BaseDashboardViewModel viewModel)
        {
            _vm = viewModel as TransportationDashboardViewModel ?? throw new ArgumentException("Invalid ViewModel Type");

            vehicleListBox.DataSource = _vm.Vehicles;
            vehicleListBox.DisplayMember = "Name";

            if (vehicleListBox.Items.Count > 0)
            {
                vehicleListBox.SelectedIndex = 0;
            }
        }

        private void VehicleListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _vm.SelectedVehicle = vehicleListBox.SelectedItem as Vehicle;
            UpdateVehicleDetails();
            UpdateChart();
        }

        public override void OnActivated()
        {
            // Optional: refresh UI when user switches to this dashboard
            UpdateVehicleDetails();
            UpdateChart();
        }
        private void UpdateVehicleDetails()
        {
            if (_vm?.SelectedVehicle == null) return;

            lblVehicleName.Text = _vm.SelectedVehicle.Name;
            lblAnnualCost.Text = $"Annual Cost: {_vm.SelectedVehicle.AnnualCost:C}";

            var e = _vm.SelectedVehicle.Energy;
            lblEnergyHome.Text = $"Home Charging: {e.HomeCharging}%";
            lblEnergyPublic.Text = $"Public Charging: {e.PublicCharging}%";
            lblEnergyRegen.Text = $"Regen: {e.Regen}%";
        }

        private void UpdateChart()
        {
            if (_vm?.SelectedVehicle == null) return;

            var series = costChart.Series["MonthlyCost"];
            series.Points.Clear();

            foreach (var mc in _vm.SelectedVehicle.MonthlyCosts)
            {
                series.Points.AddXY(mc.Month, mc.Amount);
            }
        }
    }
}
