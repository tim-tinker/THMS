using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.UI.WinForms.Controls;
using THMS.UI.WinForms.Updates;

namespace THMS.UI.WinForms
{
    public partial class DataCenterForm : BaseEmbeddedForm
    {
        private const string TabEvCharge = "EV Charge";
        private const string TabHomeCircuit = "Home Circuit";
        private const string TabSolar = "Solar";
        private const string TabCircuitAttribution = "Circuit Attribution";
        private const string TabElectricContracts = "Electric Contracts";

        private static readonly string[] TabLabels =
        [
            TabEvCharge,
            TabHomeCircuit,
            TabSolar,
            TabCircuitAttribution,
            TabElectricContracts
        ];

        private readonly Func<UserControl>[] _tabFactories =
        [
            () => new EvChargeSessionManagerControl(),
            () => new HomeCircuitManagerControl(),
            () => new SolarIntervalManagerControl(),
            () => new HomeCircuitAttributionManagerControl(),
            () => new ElectricContractManagerControl()
        ];

        private readonly EvChargeSessionUpdater _evChargeUpdater = new();
        private readonly EvChargeSessionImportOrchestrator _evChargeImport = new();
        private readonly HomeCircuitReadingOrchestrator _homeCircuitImport = new();
        private readonly SolarIntervalOrchestrator _solarImport = new();
        private readonly ElectricContractUpdater _electricContractUpdater = new();

        private string? _appliedTab;
        private string? _appliedPeriod;

        public DataCenterForm()
        {
            InitializeComponent();
            CreateTabs();
            historyBar.SelectedPeriodChanged += (_, _) => ApplyHistoryToCurrentTab();
            tabs.SelectedIndexChanged += OnTabSelected;
        }

        private void CreateTabs()
        {
            foreach (var label in TabLabels)
            {
                tabs.TabPages.Add(new TabPage(label)
                {
                    Padding = new Padding(4),
                    UseVisualStyleBackColor = false
                });
            }

            tabs.RecalculateItemSize();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !Disposing)
                ApplyHistoryToCurrentTab();
        }

        private void OnTabSelected(object? sender, EventArgs e)
        {
            ConfigureHistoryForCurrentTab();
            if (IsHandleCreated)
                BeginInvoke(ApplyHistoryToCurrentTab);
            else
                ApplyHistoryToCurrentTab();
        }

        private void ConfigureHistoryForCurrentTab()
        {
            if (tabs.SelectedTab?.Text == TabElectricContracts)
                historyBar.SetAllowedPeriods([HistoryPeriodBar.Year, HistoryPeriodBar.Lifetime], HistoryPeriodBar.Year);
            else
                historyBar.SetAllowedPeriods(
                    [HistoryPeriodBar.Month, HistoryPeriodBar.Year, HistoryPeriodBar.Lifetime],
                    HistoryPeriodBar.Month);
        }

        private bool EnsureCurrentTabControl(string period)
        {
            var page = tabs.SelectedTab;
            if (page is null || tabs.SelectedIndex < 0 || tabs.SelectedIndex >= _tabFactories.Length)
                return false;
            if (page.Controls.Count > 0)
                return false;

            var control = _tabFactories[tabs.SelectedIndex]();
            control.Dock = DockStyle.Fill;
            if (control is IDataManagerControl manager)
                manager.SetGridDataSource(period);

            HostGrid(page, control, tabs.SelectedIndex);
            return true;
        }

        private void HostGrid(TabPage page, UserControl grid, int tabIndex)
        {
            var buttons = CreateButtons(tabIndex);
            if (buttons.Length > 0)
            {
                var bar = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = true,
                    Padding = new Padding(8)
                };
                foreach (var button in buttons)
                    bar.Controls.Add(button);

                page.Controls.Add(grid);
                page.Controls.Add(bar);
            }
            else
            {
                page.Controls.Add(grid);
            }
        }

        private Control[] CreateButtons(int tabIndex)
        {
            return TabLabels[tabIndex] switch
            {
                TabEvCharge =>
                [
                    ActionButton("Add", () => _evChargeUpdater.UpdateDataSource()),
                    ActionButton("Import", OnEvChargeImport, reload: false)
                ],
                TabHomeCircuit =>
                [
                    ActionButton("Import", OnHomeCircuitImport, reload: false)
                ],
                TabSolar =>
                [
                    ActionButton("Import", OnSolarImport, reload: false)
                ],
                TabElectricContracts =>
                [
                    ActionButton("Add", () => _electricContractUpdater.UpdateDataSource(TopLevelControl))
                ],
                _ => []
            };
        }

        private ThmsButton ActionButton(string text, Action action, bool reload = true)
        {
            var button = new ThmsButton { Text = text };
            button.Click += (_, _) =>
            {
                action();
                if (reload)
                    ReloadCurrentTab();
            };
            return button;
        }

        private void OnEvChargeImport()
        {
            if (ResolveEvVehicle() is not VehicleEv vehicle)
                return;

            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select EV charge session spreadsheet",
                Multiselect = true
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var rows = _evChargeImport.LoadSessionsFromFiles(fileDialog.FileNames, vehicle);
                if (rows.Count == 0)
                {
                    MessageBox.Show(this, "The selected file(s) did not contain any complete charge sessions.",
                        "Import EV Charge Sessions", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new EvChargeSessionImportPreviewDialog(rows, _evChargeImport);
                if (preview.ShowDialog(this) != DialogResult.OK)
                    return;

                ReloadCurrentTab();
                SetCurrentImportStatus(ImportStatusText.Imported(
                    preview.Result, "EV charge session", "EV charge sessions", includeTime: true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file(s).\n{ex.Message}",
                    "Import EV Charge Sessions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private VehicleEv? ResolveEvVehicle()
        {
            var vehicles = _evChargeImport.GetEvVehicles();
            if (vehicles.Count == 0)
            {
                MessageBox.Show(this, "Add an EV before importing charge sessions.",
                    "Import EV Charge Sessions", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (vehicles.Count == 1)
                return vehicles[0];

            using var selectForm = new VehicleSelectionForm(vehicles);
            if (selectForm.ShowDialog(this) != DialogResult.OK)
                return null;

            return selectForm.SelectedVehicle as VehicleEv;
        }

        private void OnHomeCircuitImport()
        {
            using var fileDialog = new OpenFileDialog
            {
                Title = "Select Home Circuit Reading File(s)",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Multiselect = true
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var rows = _homeCircuitImport.LoadReadingsFromFiles(fileDialog.FileNames);
                if (rows.Count == 0)
                {
                    MessageBox.Show(this, "The selected file(s) did not contain any circuit intervals.",
                        "Import Circuit Intervals", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new HomeCircuitImportPreviewDialog(rows, _homeCircuitImport);
                if (preview.ShowDialog(this) != DialogResult.OK)
                    return;

                ReloadCurrentTab();
                SetCurrentImportStatus(ImportStatusText.Imported(
                    preview.Result, "circuit interval", "circuit intervals", includeTime: true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file(s).\n{ex.Message}",
                    "Import Circuit Intervals", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnSolarImport()
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Enphase Solar Data File(s)",
                Multiselect = true
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var rows = _solarImport.LoadIntervalsFromFiles(fileDialog.FileNames);
                if (rows.Count == 0)
                {
                    MessageBox.Show(this, "The selected file(s) did not contain any solar intervals.",
                        "Import Solar Intervals", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new SolarIntervalImportPreviewDialog(rows, _solarImport);
                if (preview.ShowDialog(this) != DialogResult.OK)
                    return;

                ReloadCurrentTab();
                SetCurrentImportStatus(ImportStatusText.Imported(
                    preview.Result, "solar interval", "solar intervals", includeTime: true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file(s).\n{ex.Message}",
                    "Import Solar Intervals", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetCurrentImportStatus(string message)
        {
            switch (FindManager(tabs.SelectedTab))
            {
                case EvChargeSessionManagerControl ev:
                    ev.SetImportStatus(message);
                    break;
                case HomeCircuitManagerControl circuit:
                    circuit.SetImportStatus(message);
                    break;
                case SolarIntervalManagerControl solar:
                    solar.SetImportStatus(message);
                    break;
            }
        }

        private void ReloadCurrentTab()
        {
            _appliedTab = null;
            ApplyHistoryToCurrentTab();
        }

        private void ApplyHistoryToCurrentTab()
        {
            ConfigureHistoryForCurrentTab();
            var period = historyBar.SelectedPeriod;
            var created = EnsureCurrentTabControl(period);
            var page = tabs.SelectedTab;
            var manager = FindManager(page);
            if (page is null || manager is null)
                return;

            if (!created
                && _appliedTab == page.Text
                && _appliedPeriod == period)
            {
                return;
            }

            if (!created)
                manager.SetGridDataSource(period);

            _appliedTab = page.Text;
            _appliedPeriod = period;
        }

        private static IDataManagerControl? FindManager(TabPage? page)
        {
            if (page is null)
                return null;

            foreach (Control child in page.Controls)
            {
                if (child is IDataManagerControl manager)
                    return manager;
            }

            return null;
        }
    }
}
