using System.ComponentModel;
using THMS.Logic.ViewModels;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class VehicleListDashboardForm : BaseDashboardForm
    {
        private VehicleListViewModel ViewModel { get; set; } = null!;
        private bool _binding;
        private bool _vehicleSplitApplied;
        private bool _monthSplitApplied;
        private int _loadGeneration;
        private Guid? _shownVehicleId;
        private DateTime? _shownMonth;

        protected VehicleListItemViewModel? SelectedVehicle =>
            vehicleGrid.CurrentRow?.DataBoundItem as VehicleListItemViewModel;

        private VehicleMonthContribution? SelectedMonth =>
            monthlyGrid.CurrentRow?.DataBoundItem as VehicleMonthContribution;

        public VehicleListDashboardForm()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(splitVehicles);
            SplitContainerUtil.MakeSplitterVisible(splitMonthly);
            DataGridViewUtil.EnableDoubleBuffering(vehicleGrid);
            DataGridViewUtil.EnableDoubleBuffering(monthlyGrid);
            DataGridViewUtil.EnableDoubleBuffering(contributionGrid);
            vehicleGrid.SelectionChanged += (_, _) => ShowSelectedVehicle();
            monthlyGrid.SelectionChanged += (_, _) => ShowSelectedMonth();
            monthlyGrid.DataBindingComplete += (_, _) => ConfigureMonthColumns();
            contributionGrid.DataBindingComplete += (_, _) => ConfigureContributionColumns();
            splitVehicles.SizeChanged += (_, _) => ApplySplitLayout();
            splitMonthly.SizeChanged += (_, _) => ApplySplitLayout();
            Load += (_, _) => ApplySplitLayout();
        }

        public override void InitializeDashboard()
        {
            ViewModel = new VehicleListViewModel();
            vehicleGrid.DataBindingComplete += (_, _) => ConfigureVehicleColumns();
            vehicleGrid.DataSource = ViewModel.Vehicles;
            ConfigureVehicleColumns();
        }

        private void ConfigureVehicleColumns()
        {
            if (vehicleGrid.Columns[nameof(VehicleListItemViewModel.VehicleId)] is DataGridViewColumn id)
                id.Visible = false;

            SetHeader(vehicleGrid, nameof(VehicleListItemViewModel.IsEv), "Is EV");
            SetHeader(vehicleGrid, nameof(VehicleListItemViewModel.Mpg), "MPG");
            SetHeader(vehicleGrid, nameof(VehicleListItemViewModel.CostPerMile), "Cost/Mile");
            SetHeader(vehicleGrid, nameof(VehicleListItemViewModel.TotalCost), "Total Cost");
            SetHeader(vehicleGrid, nameof(VehicleListItemViewModel.TotalMiles), "Total Miles");
            SetHeader(vehicleGrid, nameof(VehicleListItemViewModel.Gallons), "Gallons");
            PlaceColumns(
                vehicleGrid,
                nameof(VehicleListItemViewModel.Name),
                nameof(VehicleListItemViewModel.IsEv),
                nameof(VehicleListItemViewModel.Mpg),
                nameof(VehicleListItemViewModel.CostPerMile),
                nameof(VehicleListItemViewModel.TotalCost),
                nameof(VehicleListItemViewModel.TotalMiles),
                nameof(VehicleListItemViewModel.Gallons));

            FormatNumber(vehicleGrid, nameof(VehicleListItemViewModel.Mpg), "0.0");
            FormatNumber(vehicleGrid, nameof(VehicleListItemViewModel.CostPerMile), "0.0000");
            FormatNumber(vehicleGrid, nameof(VehicleListItemViewModel.TotalCost), "0.00");
            FormatNumber(vehicleGrid, nameof(VehicleListItemViewModel.Gallons), "0.000");
        }

        private void ConfigureMonthColumns()
        {
            SetHeader(monthlyGrid, nameof(VehicleMonthContribution.Month), "Month");
            SetHeader(monthlyGrid, nameof(VehicleMonthContribution.Mpg), "MPG");
            SetHeader(monthlyGrid, nameof(VehicleMonthContribution.CostPerMile), "Cost/Mile");
            SetHeader(monthlyGrid, nameof(VehicleMonthContribution.TotalCost), "Total Cost");
            SetHeader(monthlyGrid, nameof(VehicleMonthContribution.TotalMiles), "Total Miles");
            SetHeader(monthlyGrid, nameof(VehicleMonthContribution.Gallons), "Gallons");
            PlaceColumns(
                monthlyGrid,
                nameof(VehicleMonthContribution.Month),
                nameof(VehicleMonthContribution.Mpg),
                nameof(VehicleMonthContribution.CostPerMile),
                nameof(VehicleMonthContribution.TotalCost),
                nameof(VehicleMonthContribution.TotalMiles),
                nameof(VehicleMonthContribution.Gallons));

            FormatNumber(monthlyGrid, nameof(VehicleMonthContribution.Month), "y");
            FormatNumber(monthlyGrid, nameof(VehicleMonthContribution.Mpg), "0.0");
            FormatNumber(monthlyGrid, nameof(VehicleMonthContribution.CostPerMile), "0.0000");
            FormatNumber(monthlyGrid, nameof(VehicleMonthContribution.TotalCost), "0.00");
            FormatNumber(monthlyGrid, nameof(VehicleMonthContribution.TotalMiles), "0.0");
            FormatNumber(monthlyGrid, nameof(VehicleMonthContribution.Gallons), "0.000");
        }

        private void ConfigureContributionColumns()
        {
            SetHeader(contributionGrid, nameof(EvChargeSessionContribution.EndTime), "End");
            SetHeader(contributionGrid, nameof(EvChargeSessionContribution.Mpg), "MPG");
            SetHeader(contributionGrid, nameof(EvChargeSessionContribution.CostPerMile), "Cost/Mile");
            SetHeader(contributionGrid, nameof(EvChargeSessionContribution.IsHome), "Home");
            SetHeader(contributionGrid, nameof(EvChargeSessionContribution.Cost), "Cost");
            SetHeader(contributionGrid, nameof(EvChargeSessionContribution.Miles), "Miles");
            SetHeader(contributionGrid, nameof(EvChargeSessionContribution.Gallons), "Gallons");
            SetHeader(contributionGrid, nameof(IceFillUpContribution.Date), "Date");
            if (contributionGrid.Columns[nameof(EvChargeSessionContribution.EndTime)] is not null)
            {
                PlaceColumns(
                    contributionGrid,
                    nameof(EvChargeSessionContribution.EndTime),
                    nameof(EvChargeSessionContribution.Mpg),
                    nameof(EvChargeSessionContribution.CostPerMile),
                    nameof(EvChargeSessionContribution.IsHome),
                    nameof(EvChargeSessionContribution.Cost),
                    nameof(EvChargeSessionContribution.Miles),
                    nameof(EvChargeSessionContribution.Gallons));
            }

            FormatContribution(nameof(EvChargeSessionContribution.EndTime), "g");
            FormatContribution(nameof(EvChargeSessionContribution.Mpg), "0.0");
            FormatContribution(nameof(EvChargeSessionContribution.CostPerMile), "0.0000");
            FormatContribution(nameof(EvChargeSessionContribution.Cost), "0.00");
            FormatContribution(nameof(EvChargeSessionContribution.Miles), "0.0");
            FormatContribution(nameof(EvChargeSessionContribution.Gallons), "0.000");
            FormatContribution(nameof(IceFillUpContribution.Date), "d");
        }

        private static void PlaceColumns(DataGridView grid, params string[] propertyNames)
        {
            var index = 0;
            foreach (var name in propertyNames)
            {
                if (grid.Columns[name] is DataGridViewColumn column)
                    column.DisplayIndex = index++;
            }
        }

        private static void FormatNumber(DataGridView grid, string propertyName, string format)
        {
            if (grid.Columns[propertyName] is not DataGridViewColumn column)
                return;

            column.DefaultCellStyle.Format = format;
            if (format is not ("g" or "d" or "y"))
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private static void SetHeader(DataGridView grid, string propertyName, string header)
        {
            if (grid.Columns[propertyName] is DataGridViewColumn column)
                column.HeaderText = header;
        }

        private void FormatContribution(string propertyName, string format)
        {
            FormatNumber(contributionGrid, propertyName, format);
        }

        private void ApplySplitLayout()
        {
            if (!_vehicleSplitApplied
                && SplitContainerUtil.TrySetLeadingRatio(splitVehicles, leadingParts: 1, trailingParts: 4))
                _vehicleSplitApplied = true;
            if (!_monthSplitApplied
                && SplitContainerUtil.TrySetLeadingRatio(splitMonthly, leadingParts: 1, trailingParts: 1))
                _monthSplitApplied = true;
        }

        private void ShowSelectedVehicle()
        {
            if (_binding || SelectedVehicle is not VehicleListItemViewModel vehicle)
                return;

            BindMonths(IndexOf(vehicle.VehicleId), preferredMonth: null, force: false);
        }

        private void ShowSelectedMonth()
        {
            if (_binding || SelectedMonth is not VehicleMonthContribution month)
                return;

            BindDetails(month, force: false);
        }

        private int IndexOf(Guid? vehicleId)
        {
            if (vehicleId is Guid id)
            {
                for (var i = 0; i < ViewModel.Vehicles.Count; i++)
                {
                    if (ViewModel.Vehicles[i].VehicleId == id)
                        return i;
                }
            }

            return 0;
        }

        private static int IndexOfMonth(IReadOnlyList<VehicleMonthContribution> months, DateTime? month)
        {
            if (month is DateTime selected)
            {
                for (var i = 0; i < months.Count; i++)
                {
                    if (months[i].Month == selected)
                        return i;
                }
            }

            return 0;
        }

        private void BindMonths(int vehicleIndex, DateTime? preferredMonth, bool force)
        {
            var vehicle = vehicleIndex >= 0 && vehicleIndex < ViewModel.Vehicles.Count
                ? ViewModel.Vehicles[vehicleIndex]
                : null;
            if (!force && _shownVehicleId == vehicle?.VehicleId)
                return;

            _shownVehicleId = vehicle?.VehicleId;
            var months = vehicle?.Months ?? [];
            _binding = true;
            try
            {
                monthlyGrid.DataSource = new BindingList<VehicleMonthContribution>(months.ToList());
            }
            finally
            {
                _binding = false;
            }

            var index = IndexOfMonth(months, preferredMonth);
            BindDetails(index < months.Count ? months[index] : null, force: true);
            DataGridViewUtil.SelectCellWhenReady(monthlyGrid, index, 0);
        }

        private void BindDetails(VehicleMonthContribution? month, bool force)
        {
            if (!force && _shownMonth == month?.Month)
                return;

            _shownMonth = month?.Month;
            var vehicle = _shownVehicleId is Guid id
                ? ViewModel.Vehicles.FirstOrDefault(item => item.VehicleId == id)
                : null;
            contributionGrid.DataSource = (vehicle, month) switch
            {
                ({ IsEv: true }, { } selected) =>
                    new BindingList<EvChargeSessionContribution>(selected.ChargeSessions.ToList()),
                ({ }, { } selected) =>
                    new BindingList<IceFillUpContribution>(selected.FillUps.ToList()),
                _ => null
            };
        }

        public override void RefreshDashboard() => _ = LoadVehiclesAsync();

        private async Task LoadVehiclesAsync()
        {
            if (ViewModel is null)
                return;

            var generation = Interlocked.Increment(ref _loadGeneration);
            List<VehicleListItemViewModel> items;
            try
            {
                items = await Task.Run(() => ViewModel.Query("Lifetime"));
            }
            catch (Exception ex)
            {
                if (!IsDisposed && generation == _loadGeneration && IsHandleCreated)
                    BeginInvoke(() => ExceptionDialog.Show(this, "Vehicles", ex));
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

                var selectedId = SelectedVehicle?.VehicleId;
                var selectedMonth = SelectedMonth?.Month;
                _binding = true;
                try
                {
                    ViewModel.Replace(items);
                }
                finally
                {
                    _binding = false;
                }

                var index = IndexOf(selectedId);
                BindMonths(index, selectedMonth, force: true);
                DataGridViewUtil.SelectCellWhenReady(vehicleGrid, index, 0);
            }
        }

        private void btnAddVehicle_Click(object sender, EventArgs e)
        {
            var count = ViewModel.Vehicles.Count;
            using var form = new AddVehicleForm(ViewModel);
            form.ShowDialog(this);
            if (ViewModel.Vehicles.Count > count)
            {
                var index = ViewModel.Vehicles.Count - 1;
                BindMonths(index, preferredMonth: null, force: true);
                DataGridViewUtil.SelectCellWhenReady(vehicleGrid, index, 0);
            }
        }

        private void btnDetails_Click(object sender, EventArgs e)
        {
            if (SelectedVehicle != null)
            {
                var form = new VehicleDetailForm(SelectedVehicle.VehicleId, "Lifetime");

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
