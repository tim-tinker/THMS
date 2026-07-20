using TPFS.Domain;
using TPFS.Logic.ViewModels;

namespace TPFS.UI.WinForms;

public partial class MainForm : Form
{
    private readonly TransportationDashboardViewModel _vm;
    private TransportationDashboardForm _transportationDashboard;

    public MainForm()
    {
        InitializeComponent();
        InitializeModules();

        _vm = new TransportationDashboardViewModel();

        vehicleListBox.DataSource = _vm.Vehicles;
        vehicleListBox.DisplayMember = "Name";

        vehicleListBox.SelectedIndexChanged += (s, e) =>
        {
            _vm.SelectedVehicle = vehicleListBox.SelectedItem as Vehicle;
            UpdateVehicleDetails();
            UpdateChart();
        };

        if (vehicleListBox.Items.Count > 0)
        {
            vehicleListBox.SelectedIndex = 0;
        }
    }

    private void InitializeModules()
    {
        // Create dashboard form
        _transportationDashboard = new TransportationDashboardForm
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill,
            Visible = false        // IMPORTANT: hide until selected
        };

        // Add to the content panel
        contentPanel.Controls.Add(_transportationDashboard);
    }

    private void btnTransportation_Click(object sender, EventArgs e)
    {
        // Show dashboard
        _transportationDashboard.Show();
        _transportationDashboard.BringToFront();
    }

    private void UpdateVehicleDetails()
    {
        if (_vm.SelectedVehicle == null) return;

        lblVehicleName.Text = _vm.SelectedVehicle.Name;
        lblAnnualCost.Text = $"Annual Cost: {_vm.SelectedVehicle.AnnualCost:C}";

        var e = _vm.SelectedVehicle.Energy;
        lblEnergyHome.Text = $"Home Charging: {e.HomeCharging}%";
        lblEnergyPublic.Text = $"Public Charging: {e.PublicCharging}%";
        lblEnergyRegen.Text = $"Regen: {e.Regen}%";
    }

    private void UpdateChart()
    {
        if (_vm.SelectedVehicle == null) return;

        costChart.Series[0].Points.Clear();

        foreach (var mc in _vm.SelectedVehicle.MonthlyCosts)
        {
            costChart.Series[0].Points.AddXY(mc.Month, mc.Cost);
        }
    }
}
