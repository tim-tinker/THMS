using System;
using System.Linq;
using System.Windows.Forms;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class TransportationDashboardForm
        : BaseDashboardForm<TransportationDashboardViewModel>
    {
        public TransportationDashboardForm()
        {
            InitializeComponent();
        }

        protected override void BindControlsToViewModel()
        {
            vehicleListBox.DataSource = ViewModel.Vehicles;
            vehicleListBox.DisplayMember = "Name";

            if (ViewModel.Vehicles.Any())
                vehicleListBox.SelectedIndex = 0;

            vehicleListBox.SelectedIndexChanged += VehicleListBox_SelectedIndexChanged;
        }

        // ---------------------------------------------------------
        // Refresh dashboard (called by MainForm + user interactions)
        // ---------------------------------------------------------
        public override void RefreshDashboard()
        {
            if (ViewModel.SelectedVehicle == null)
                return;

            var now = DateTime.Now;
            ViewModel.Refresh(now.Year, now.Month);

            UpdateVehicleDetails();
            UpdateChart();
        }

        // ---------------------------------------------------------
        // User selects a vehicle
        // ---------------------------------------------------------
        private void VehicleListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ViewModel.SelectedVehicle = vehicleListBox.SelectedItem as Vehicle;
            RefreshDashboard();
        }

        // ---------------------------------------------------------
        // Helper: update labels
        // ---------------------------------------------------------
        private void UpdateVehicleDetails()
        {
            var vehicle = ViewModel.SelectedVehicle;
            if (vehicle == null)
                return;

            lblVehicleName.Text = vehicle.Name;
            lblLifetimeCostPerMile.Text = $"Lifetime Cost/Mile: {ViewModel.LifetimeCostPerMile:C}";

            var summary = ViewModel.MonthlySummary;
            if (summary != null)
            {
                lblMonthlyMiles.Text = $"Miles: {summary.MilesDriven}";
                //lblMonthlyCost.Text = $"Cost: {summary.Cost:C}";
                lblMonthlyCostPerMile.Text = $"Cost/Mile: {summary.CostPerMile:C}";
            }
        }

        // ---------------------------------------------------------
        // Helper: update chart
        // ---------------------------------------------------------
        private void UpdateChart()
        {
            var summary = ViewModel.MonthlySummary;
            if (summary == null)
                return;

            var series = costChart.Series["MonthlyCost"];
            series.Points.Clear();

            //foreach (var entry in summary.Entries)
            //    series.Points.AddXY(entry.MonthName, entry.Cost);
        }
    }
}
