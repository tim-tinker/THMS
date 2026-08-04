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

        protected VehicleListItemViewModel? SelectedVehicle =>
            vehicleGrid.SelectedRows.Count > 0
                ? vehicleGrid.SelectedRows[0].DataBoundItem as VehicleListItemViewModel
                : null;

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
                var form = new VehicleDetailForm(
                    _vehicleDataStore,
                    SelectedVehicle.VehicleId);

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
