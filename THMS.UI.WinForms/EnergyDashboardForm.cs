using THMS.Data.Stores;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms
{
    public partial class EnergyDashboardForm : BaseDashboardForm
    {
        private readonly IEnergyDataStore? _energyDataStore;
        private EnergyDashboardViewModel ViewModel { get; set; } = null!;

        /// <summary>Designer only.</summary>
        public EnergyDashboardForm()
        {
            InitializeComponent();
        }

        public EnergyDashboardForm(IEnergyDataStore energyDataStore) : this()
        {
            _energyDataStore = energyDataStore;
        }

        public override void InitializeDashboard()
        {
            // Store is available for engines/importers you'll wire later.
            ViewModel = new EnergyDashboardViewModel();
            BindControlsToViewModel();
            ViewModel.Initialize();
            RefreshDashboard();
        }

        private void BindControlsToViewModel()
        {
            dtpStart.Value = ViewModel.StartDate;
            dtpEnd.Value = ViewModel.EndDate;
            btnRefresh.Click += (_, _) => RefreshDashboard();
        }

        public override void RefreshDashboard()
        {
            ViewModel.StartDate = dtpStart.Value;
            ViewModel.EndDate = dtpEnd.Value;
            BindEnergyGrid();
        }

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

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDashboard();
        }
    }
}
