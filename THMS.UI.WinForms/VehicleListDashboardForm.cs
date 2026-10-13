using THMS.Logic.ViewModels;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class VehicleListDashboardForm : BaseDashboardForm
    {
        private VehicleListViewModel ViewModel { get; set; } = null!;
        private bool _suspendHistory;
        private int _loadGeneration;

        protected VehicleListItemViewModel? SelectedVehicle =>
            vehicleGrid.SelectedRows.Count > 0
                ? vehicleGrid.SelectedRows[0].DataBoundItem as VehicleListItemViewModel
                : null;

        public VehicleListDashboardForm()
        {
            InitializeComponent();
            DataGridViewUtil.EnableDoubleBuffering(vehicleGrid);
            historyBar.SelectedPeriodChanged += (_, _) =>
            {
                if (ViewModel is null || _suspendHistory)
                    return;

                _ = LoadVehiclesAsync();
            };
        }

        public override void InitializeDashboard()
        {
            ViewModel = new VehicleListViewModel();
            _suspendHistory = true;
            historyBar.SelectPeriod(ViewModel.HistoryPeriod);
            _suspendHistory = false;
            vehicleGrid.DataBindingComplete += (_, _) => HideVehicleIdColumn();
            vehicleGrid.DataSource = ViewModel.Vehicles;
            HideVehicleIdColumn();
        }

        private void HideVehicleIdColumn()
        {
            if (vehicleGrid.Columns[nameof(VehicleListItemViewModel.VehicleId)] is DataGridViewColumn column)
                column.Visible = false;
        }

        public override void RefreshDashboard() => _ = LoadVehiclesAsync();

        private async Task LoadVehiclesAsync()
        {
            if (ViewModel is null)
                return;

            var generation = Interlocked.Increment(ref _loadGeneration);
            var period = historyBar.SelectedPeriod;
            List<VehicleListItemViewModel> items;
            try
            {
                items = await Task.Run(() => ViewModel.Query(period));
            }
            catch (Exception ex)
            {
                if (!IsDisposed && generation == _loadGeneration && IsHandleCreated)
                    BeginInvoke(() => MessageBox.Show(this, ex.Message, "Vehicles",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning));
                return;
            }

            if (IsDisposed || generation != _loadGeneration)
                return;
            if (InvokeRequired)
                BeginInvoke(Apply);
            else
                Apply();

            void Apply()
            {
                if (IsDisposed || generation != _loadGeneration)
                    return;
                ViewModel.Replace(items);
            }
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
                var form = new VehicleDetailForm(SelectedVehicle.VehicleId, historyBar.SelectedPeriod);

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
