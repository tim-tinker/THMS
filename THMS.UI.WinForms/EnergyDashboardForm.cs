using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using THMS.Data.Stores;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.Energy;
using THMS.Logic.ViewModels.Energy;
using THMS.UI.WinForms.Charts;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class EnergyDashboardForm : BaseDashboardForm
    {
        private readonly IEnergyDataStore? _energyDataStore;

        private EnergyDashboardViewModel _vm { get; set; } = null!;
        private bool _initialized;
        private bool _loading;

        public EnergyDashboardForm()
        {
            InitializeComponent();
            InitializeDynamicLayout();
        }

        public EnergyDashboardForm(IEnergyDataStore energyDataStore)
            : this()
        {
            _energyDataStore = energyDataStore ?? throw new ArgumentNullException(nameof(energyDataStore));
        }

        private void InitializeDynamicLayout()
        {
            InitializeSummaryPanel();

            ConfigureDayTab();
            ConfigurePeriodTab(_tabWeek);
            ConfigurePeriodTab(_tabMonth);
            ConfigurePeriodTab(_tabYear);
        }

        private void InitializeSummaryPanel()
        {
            _panelSummary.Height = 88;

            _tableSummary.SuspendLayout();
            _tableSummary.Controls.Clear();
            _tableSummary.ColumnStyles.Clear();
            _tableSummary.ColumnCount = 8;
            for (int i = 0; i < 8; i++)
                _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));

            _tableSummary.Controls.Add(CreateMetricCell("Produced", _labelProduced), 0, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Consumed", _labelConsumed), 1, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Imported", _labelImported), 2, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Exported", _labelExported), 3, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Battery", _labelBattery), 4, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Net", _labelNet), 5, 0);
            _tableSummary.Controls.Add(CreateMetricCell("EV Charging", _labelEvCharging), 6, 0);

            AddLoadSolarDataButton();
            _tableSummary.ResumeLayout();
        }

        private static Control CreateMetricCell(string caption, Label valueLabel)
        {
            var cell = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 2, 0),
            };
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var captionLabel = new Label
            {
                Text = caption,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.BottomLeft,
            };

            valueLabel.AutoSize = false;
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Margin = Padding.Empty;
            valueLabel.TextAlign = ContentAlignment.TopLeft;
            valueLabel.Text = "0.0 kWh";

            cell.Controls.Add(captionLabel, 0, 0);
            cell.Controls.Add(valueLabel, 0, 1);
            return cell;
        }

        private void AddLoadSolarDataButton()
        {
            // WinForms themed buttons do not render newlines in Text reliably.
            // Keep the caption in Tag and draw it ourselves.
            var btn = new Button
            {
                Text = string.Empty,
                Tag = "Load" + Environment.NewLine + "Solar Data",
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                UseVisualStyleBackColor = true,
            };

            btn.Paint += OnPaintLoadSolarDataButton;
            btn.Click += OnClickLoadSolarData;
            _tableSummary.Controls.Add(btn, 7, 0);
        }

        private void ConfigureDayTab()
        {
            var vcr = new VcrControl() { Tag = "Day", Dock = DockStyle.Top };
            vcr.MoveBackward += OnMoveTabBackward;
            vcr.MoveForward += OnMoveTabForward;
            vcr.DateSelected += OnTabDateSelected;

            // Insert VCR at the top of the Day tab
            _tabDay.Controls.Add(vcr);
        }

        // ============================================================
        // WEEK / MONTH / YEAR TABS
        // ============================================================
        void ConfigurePeriodTab(TabPage tab)
        {
            tab.Padding = new Padding(10);

            var vcr = new VcrControl() { Tag = tab.Text, Dock = DockStyle.Top };
            vcr.MoveBackward += OnMoveTabBackward;
            vcr.MoveForward += OnMoveTabForward;
            vcr.DateSelected += OnTabDateSelected;

            _panelPeriodChart = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "_panelPeriodChart"
            };

            _panelPeriodBreakdown = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "_panelPeriodBreakdown",
                MinimumSize = new Size(0, 200),
                AutoScroll = true
            };

            tab.Controls.Add(_panelPeriodBreakdown);
            tab.Controls.Add(_panelPeriodChart);
            tab.Controls.Add(vcr);
        }

        public override void InitializeDashboard()
        {
            if (_initialized) return;

            if (_energyDataStore is null)
                throw new InvalidOperationException("Data stores were not provided. Resolve this form from DI at runtime.");

            _vm = new EnergyDashboardViewModel();
            _vm.SetStores(_energyDataStore);
            RefreshDashboard();

            _initialized = true;
        }

        public override void RefreshDashboard()
        {
            _vm.Refresh();

            LoadSummaryBar();
            LoadDayTab();
            LoadWeekTab();
            LoadMonthTab();
            LoadYearTab();
            LoadCustomTab();

        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            _loading = true;

            try
            {
                RefreshDashboard();
            }
            finally
            {
                _loading = false;
            }
        }

        // ============================================================
        // SUMMARY BAR
        // ============================================================
        private void LoadSummaryBar()
        {
            _labelProduced.Text = $"{_vm.Summary.ProducedKwh:N1} kWh";
            _labelConsumed.Text = $"{_vm.Summary.ConsumedKwh:N1} kWh";
            _labelImported.Text = $"{_vm.Summary.GridImportKwh:N1} kWh";
            _labelExported.Text = $"{_vm.Summary.GridExportKwh:N1} kWh";
            _labelBattery.Text = $"{_vm.Summary.BatteryNetKwh:N1} kWh";
            _labelNet.Text = $"{_vm.Summary.NetImportKwh:N1} kWh";
            _labelEvCharging.Text = $"{_vm.Summary.EvChargingKwh:N1} kWh";
        }

        // ============================================================
        // DAY TAB
        // ============================================================
        private void LoadDayTab()
        {
            // Chart rendering will be added later
            RenderDayEnergyFlowChart();
            RenderDayBreakdown();
        }

        private void RenderDayEnergyFlowChart()
        {
            // Placeholder: chart control will be added later
            _panelDayEnergyFlow.Controls.Clear();

            var chart = EnergyChartFactory.CreateDayChart(_vm.Day);
            chart.Dock = DockStyle.Fill;

            _panelDayEnergyFlow.Controls.Add(chart);
        }

        private void RenderDayBreakdown()
        {
            _panelDayBreakdown.Controls.Clear();

            var breakdown = new EnergyBreakdownControl(_vm.Day);
            breakdown.Dock = DockStyle.Fill;

            _panelDayBreakdown.Controls.Add(breakdown);
        }

        // ============================================================
        // WEEK / MONTH / YEAR TABS
        // ============================================================
        private void LoadWeekTab()
        {
            RenderPeriodChart(_tabWeek, _vm.Week);
            RenderPeriodBreakdown(_tabWeek, _vm.Week);
        }

        private void LoadMonthTab()
        {
            RenderPeriodChart(_tabMonth, _vm.Month);
            RenderPeriodBreakdown(_tabMonth, _vm.Month);
        }

        private void LoadYearTab()
        {
            RenderPeriodChart(_tabYear, _vm.Year);
            RenderPeriodBreakdown(_tabYear, _vm.Year);
        }

        private void RenderPeriodChart(TabPage tab, EnergyPeriodViewModel period)
        {
            var chartPanel = FindPanel(tab, "_panelPeriodChart");
            chartPanel.Controls.Clear();

            var chart = EnergyChartFactory.CreatePeriodBarChart(period);
            chart.Dock = DockStyle.Fill;

            chartPanel.Controls.Add(chart);
        }

        private void RenderPeriodBreakdown(TabPage tab, EnergyPeriodViewModel period)
        {
            var breakdownPanel = FindPanel(tab, "_panelPeriodBreakdown");
            breakdownPanel.Controls.Clear();

            var breakdown = new EnergyBreakdownControl(period);
            breakdown.Dock = DockStyle.Fill;

            breakdownPanel.Controls.Add(breakdown);
        }

        // ============================================================
        // CUSTOM TAB
        // ============================================================
        private void LoadCustomTab()
        {
            // Later: date pickers will trigger VM updates
            RenderCustomChart();
            RenderCustomBreakdown();
        }

        private void RenderCustomChart()
        {
            _panelCustomChart.Controls.Clear();

            var chart = EnergyChartFactory.CreatePeriodBarChart(_vm.Custom);
            chart.Dock = DockStyle.Fill;

            _panelCustomChart.Controls.Add(chart);
        }

        private void RenderCustomBreakdown()
        {
            _panelCustomBreakdown.Controls.Clear();

            var breakdown = new EnergyBreakdownControl(_vm.Custom);
            breakdown.Dock = DockStyle.Fill;

            _panelCustomBreakdown.Controls.Add(breakdown);
        }

        // ============================================================
        // Utility: find panel inside tab by name
        // ============================================================
        private Panel FindPanel(TabPage tab, string name)
        {
            foreach (Control c in tab.Controls)
            {
                if (c is Panel p && p.Name == name)
                    return p;
            }

            throw new InvalidOperationException($"Panel '{name}' not found in tab '{tab.Name}'.");
        }

        private void OnClickLoadSolarData(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Import Enphase Solar Data"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var importer = new EnphaseSolarImporter(_energyDataStore);
                importer.Import(dialog.FileName);

                CalculateEvAttribution(importer.StartDate, importer.EndDate);

                MessageBox.Show($"Solar data imported successfully for {importer.StartDate.Date} to {importer.EndDate.Date}.", "THMS",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            } 
            catch (Exception ex) 
            {
                MessageBox.Show($"Error importing solar data: {ex.Message}", "THMS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshDashboard();
        }

        private void CalculateEvAttribution(DateTime start, DateTime end)
        {
            if (_energyDataStore.GetEvCircuitReadings(start, end).Any())
            {
                var engine = new EvAttributionEngine(_energyDataStore);
                engine.Compute(start, end);
            }
        }

        private static void OnPaintLoadSolarDataButton(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string caption)
                return;

            TextRenderer.DrawText(
                e.Graphics,
                caption,
                btn.Font,
                btn.ClientRectangle,
                btn.ForeColor,
                TextFormatFlags.HorizontalCenter
                    | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.WordBreak);
        }

        private void OnMoveTabBackward(object? sender, EventArgs e) 
        {
            var vcrControl = sender as VcrControl;
            switch (vcrControl?.Tag)
            {
                case "Year":
                    _vm.MoveYear(-1);
                    break;
                case "Month":
                    _vm.MoveMonth(-1);
                    break;
                case "Week":
                    _vm.MoveWeek(-1);
                    break;
                case "Day":
                    _vm.MoveDay(-1);
                    break;
            }
            RefreshDashboard();
        }

        private void OnMoveTabForward(object? sender, EventArgs e) 
        {
            var vcrControl = sender as VcrControl;
            switch (vcrControl?.Tag)
            {
                case "Year":
                    _vm.MoveYear(1);
                    break;
                case "Month":
                    _vm.MoveMonth(1);
                    break;
                case "Week":
                    _vm.MoveWeek(1);
                    break;
                case "Day":
                    _vm.MoveDay(1);
                    break;
            }
            RefreshDashboard();
        }

        private void OnTabDateSelected(object? sender, EventArgs<DateTime> e)
        {
            var vcrControl = sender as VcrControl;
            switch (vcrControl?.Tag)
            {
                case "Year":
                    _vm.JumpToYear(e.Value);
                    break;
                case "Month":
                    _vm.JumpToMonth(e.Value);
                    break;
                case "Week":
                    _vm.JumpToWeek(e.Value);
                    break;
                case "Day":
                    _vm.JumpToDay(e.Value);
                    break;
            }
            RefreshDashboard();
        }
    }
}
