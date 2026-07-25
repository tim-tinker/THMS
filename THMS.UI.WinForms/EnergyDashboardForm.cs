using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms
{
    public partial class EnergyDashboardForm
        : BaseDashboardForm<EnergyDashboardViewModel>
    {
        public EnergyDashboardForm()
        {
            InitializeComponent();
        }

        // ---------------------------------------------------------
        // Bind controls to the ViewModel (called once by MainForm)
        // ---------------------------------------------------------
        protected override void BindControlsToViewModel()
        {
            dtpStart.Value = ViewModel.StartDate;
            dtpEnd.Value = ViewModel.EndDate;

            btnRefresh.Click += (s, e) => RefreshDashboard();
        }

        // ---------------------------------------------------------
        // Refresh dashboard (called by MainForm + user interactions)
        // ---------------------------------------------------------
        public override void RefreshDashboard()
        {
            // Update ViewModel date range
            ViewModel.StartDate = dtpStart.Value;
            ViewModel.EndDate = dtpEnd.Value;

            // Later: call your energy engines here
            // For now: ViewModel.EnergyData is already populated externally

            BindEnergyGrid();
        }

        // ---------------------------------------------------------
        // Helper: bind grid to ViewModel data
        // ---------------------------------------------------------
        private void BindEnergyGrid()
        {
            var data = ViewModel.EnergyData
                .Where(e => e.Timestamp >= ViewModel.StartDate &&
                            e.Timestamp <= ViewModel.EndDate)
                .Select(e => new
                {
                    e.Timestamp,
                    e.SolarWh,
                    e.GridWh,
                    e.BatteryWh,
                    e.EvChargingWh,
                    e.IsPartial
                })
                .ToList();

            energyGrid.DataSource = data;
        }

        // ---------------------------------------------------------
        // User clicked Refresh button
        // ---------------------------------------------------------
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDashboard();
        }
    }
}
