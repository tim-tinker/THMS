using THMS.Data.Stores;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class VehicleListDashboardForm : BaseDashboardForm
    {
        private readonly IVehicleDataStore? _vehicleDataStore;
        private readonly IFinanceDataStore? _financeDataStore;
        private readonly IEnergyDataStore? _energyDataStore;
        private VehicleListViewModel ViewModel { get; set; } = null!;

        /// <summary>Designer only.</summary>
        public VehicleListDashboardForm()
        {
            InitializeComponent();
        }

        public VehicleListDashboardForm(
            IVehicleDataStore vehicleDataStore,
            IFinanceDataStore financeDataStore,
            IEnergyDataStore energyDataStore) : this()
        {
            _vehicleDataStore = vehicleDataStore;
            _financeDataStore = financeDataStore;
            _energyDataStore = energyDataStore;
        }

        public override void InitializeDashboard()
        {
            if (_vehicleDataStore is null || _financeDataStore is null || _energyDataStore is null)
                throw new InvalidOperationException("Data stores were not provided. Resolve this form from DI at runtime.");

            ViewModel = new VehicleListViewModel();
            ViewModel.SetStores(_vehicleDataStore, _financeDataStore, _energyDataStore);
            ViewModel.Initialize();
            RefreshDashboard();
        }

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
