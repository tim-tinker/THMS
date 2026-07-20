using TPFS.Domain;
using TPFS.Logic.ViewModels;

namespace TPFS.UI.WinForms
{
    public partial class TransportationDashboardForm : Form
    {
        private TransportationDashboardViewModel _vm;

        public TransportationDashboardForm()
        {
            InitializeComponent();
        }

        public void BindViewModel(TransportationDashboardViewModel vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));

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
                series.Points.AddXY(mc.Month, mc.Cost);
            }
        }
    }
}
