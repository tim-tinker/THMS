using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class TransportationDashboardForm : BaseDashboardForm
    {
        private readonly IVehicleDataStore? _vehicleDataStore;
        private TransportationDashboardViewModel ViewModel { get; set; } = null!;

        /// <summary>Designer only.</summary>
        public TransportationDashboardForm()
        {
            InitializeComponent();
        }

        public TransportationDashboardForm(IVehicleDataStore vehicleDataStore) : this()
        {
            _vehicleDataStore = vehicleDataStore;
        }

        public override void InitializeDashboard()
        {
            if (_vehicleDataStore is null)
                throw new InvalidOperationException("IVehicleDataStore was not provided. Resolve this form from DI at runtime.");

            ViewModel = new TransportationDashboardViewModel(_vehicleDataStore);
            BindControlsToViewModel();
            ViewModel.Initialize();
            RefreshDashboard();
        }

        private void BindControlsToViewModel()
        {
            vehicleListBox.DataSource = ViewModel.Vehicles;
            vehicleListBox.DisplayMember = "Name";

            if (ViewModel.Vehicles.Any())
                vehicleListBox.SelectedIndex = 0;

            vehicleListBox.SelectedIndexChanged += VehicleListBox_SelectedIndexChanged;
        }

        public override void RefreshDashboard()
        {
            if (ViewModel.SelectedVehicle == null)
                return;

            var now = DateTime.Now;
            ViewModel.Refresh(now.Year, now.Month);

            UpdateVehicleDetails();
            UpdateChart();
        }

        private void VehicleListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ViewModel.SelectedVehicle = vehicleListBox.SelectedItem as VehicleBase;
            RefreshDashboard();
        }

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
                lblMonthlyCostPerMile.Text = $"Cost/Mile: {summary.CostPerMile:C}";
            }
        }

        private void UpdateChart()
        {
            var summary = ViewModel.MonthlySummary;
            if (summary == null)
                return;

            var series = costChart.Series["MonthlyCost"];
            series.Points.Clear();
        }
    }
}
