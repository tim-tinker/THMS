using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using THMS.Logic.ViewModels.Energy;
using THMS.UI.WinForms.Charts;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class EnergyDashboardForm : BaseDashboardForm
    {
        private EnergyDashboardViewModel _vm { get; set; } = null!;
        private bool _initialized;
        private bool _loading;

        public EnergyDashboardForm()
        {
            InitializeComponent();
            InitializeDynamicLayout();
        }

        private void InitializeDynamicLayout()
        {
            AddVcrControl();
            InitializeSummaryPanel();

            ConfigurePeriodTab(_tabDay);
            ConfigurePeriodTab(_tabWeek);
            ConfigurePeriodTab(_tabMonth);
            ConfigurePeriodTab(_tabYear);
        }

        private void AddVcrControl()
        {
            var vcr = new VcrControl
            {
                Dock = DockStyle.Top,
                Tag = "Global"
            };

            vcr.MoveBackward += OnMoveTabBackward;
            vcr.MoveForward += OnMoveTabForward;
            vcr.DateSelected += OnTabDateSelected;


            var parent = _panelSummary.Parent;

            // Insert VCR above the summary panel
            parent.Controls.Add(vcr);
            parent.Controls.SetChildIndex(vcr, parent.Controls.GetChildIndex(_panelSummary));
        }

        private void InitializeSummaryPanel()
        {
            _panelSummary.Height = 88;

            _tableSummary.SuspendLayout();
            _tableSummary.Controls.Clear();
            _tableSummary.ColumnStyles.Clear();
            _tableSummary.ColumnCount = 11;
            for (int i = 0; i < _tableSummary.ColumnCount; i++)
                _tableSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.09F));

            _tableSummary.Controls.Add(CreateMetricCell("Produced", _labelProduced), 0, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Consumed", _labelConsumed), 1, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Imported", _labelImported), 2, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Exported", _labelExported), 3, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Net Grid", _labelNetGrid), 4, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Grid Dependence", _labelGridDependence), 5, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Net Grid Dependence", _labelNetGridDependence), 6, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Battery+", _labelBatteryCharge), 7, 0);
            _tableSummary.Controls.Add(CreateMetricCell("Battery-", _labelBatteryDischarge), 8, 0);
            _tableSummary.Controls.Add(CreateMetricCell("EV Charge", _labelEvCharge), 9, 0);
            _tableSummary.Controls.Add(CreateMetricCell("EV Consumption", _labelEvConsumption), 10, 0);

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

        // ============================================================
        // WEEK / MONTH / YEAR TABS
        // ============================================================
        void ConfigurePeriodTab(TabPage tab)
        {
            tab.Padding = new Padding(10);

            _panelPeriodChart = new Panel
            {
                Dock = DockStyle.Top,
                Height = 300,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "_panelPeriodChart"
            };

            _panelPeriodMetrics = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "_panelPeriodMetrics",
                MinimumSize = new Size(0, 200),
                AutoScroll = true
            };

            tab.Controls.Add(_panelPeriodMetrics);
            tab.Controls.Add(_panelPeriodChart);
        }

        public override void InitializeDashboard()
        {
            if (_initialized) return;

            _vm = new EnergyDashboardViewModel();
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
            _labelNetGrid.Text = $"{_vm.Summary.NetImportKwh:N1} kWh";
            _labelGridDependence.Text = $"{_vm.Summary.GridDependence:N1}%";
            _labelNetGridDependence.Text = $"{_vm.Summary.NetGridDependence:N1}%";
            _labelBatteryCharge.Text = $"{_vm.Summary.BatteryChargeKwh:N1} kWh";
            _labelBatteryDischarge.Text = $"{_vm.Summary.BatteryDischargeKwh:N1} kWh";
            _labelEvCharge.Text = $"{_vm.Summary.EvChargeKwh:N1} kWh";
            _labelEvConsumption.Text = $"{_vm.Summary.EvConsumption:N1}%";
        }

        // ============================================================
        // DAY / WEEK / MONTH / YEAR TABS
        // ============================================================
        private void LoadDayTab()
        {
            RenderDayChart(_tabDay, _vm.Day);
            RenderPeriodBreakdown(_tabDay, _vm.Day);
        }

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

        private void RenderDayChart(TabPage tab, EnergyPeriodViewModel period)
        {
            var chartPanel = FindPanel(tab, "_panelPeriodChart");
            chartPanel.Controls.Clear();

            var chart = EnergyChartFactory.CreateDayChart(period);
            chart.Dock = DockStyle.Fill;

            chartPanel.Controls.Add(chart);
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
            var metricsPanel = FindPanel(tab, "_panelPeriodMetrics");
            metricsPanel.Controls.Clear();

            var metricsControl = new EnergyMetricsTableControl();
            metricsControl.Bind(period);
            metricsControl.Dock = DockStyle.Fill;

            metricsPanel.Controls.Add(metricsControl);
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

            var metricsControl = new EnergyMetricsTableControl();
            metricsControl.Bind(_vm.Custom);
            metricsControl.Dock = DockStyle.Fill;

            _panelCustomBreakdown.Controls.Add(metricsControl);
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

        private void OnMoveTabBackward(object? sender, EventArgs e)
        {
            switch (_vm.SelectedTab)
            {
                case EnergyTab.Day:
                    _vm.MoveDay(-1);
                    break;
                case EnergyTab.Week:
                    _vm.MoveWeek(-1);
                    break;
                case EnergyTab.Month:
                    _vm.MoveMonth(-1);
                    break;
                case EnergyTab.Year:
                    _vm.MoveYear(-1);
                    break;
            }
            RefreshDashboard();
        }

        private void OnMoveTabForward(object? sender, EventArgs e)
        {
            switch (_vm.SelectedTab)
            {
                case EnergyTab.Day:
                    _vm.MoveDay(1);
                    break;
                case EnergyTab.Week:
                    _vm.MoveWeek(1);
                    break;
                case EnergyTab.Month:
                    _vm.MoveMonth(1);
                    break;
                case EnergyTab.Year:
                    _vm.MoveYear(1);
                    break;
            }
            RefreshDashboard();
        }

        private void OnTabDateSelected(object? sender, EventArgs<DateTime> e)
        {
            switch (_vm.SelectedTab)
            {
                case EnergyTab.Day:
                    _vm.JumpToDay(e.Value);
                    break;
                case EnergyTab.Week:
                    _vm.JumpToWeek(e.Value);
                    break;
                case EnergyTab.Month:
                    _vm.JumpToMonth(e.Value);
                    break;
                case EnergyTab.Year:
                    _vm.JumpToYear(e.Value);
                    break;
            }
            RefreshDashboard();
        }

        private void OnSelectedIndexChangedTabControl(object sender, EventArgs e)
        {

            switch (_tabs.SelectedTab?.Name)
            {
                case "_tabDay":
                    _vm.SelectedTab = EnergyTab.Day;
                    break;

                case "_tabWeek":
                    _vm.SelectedTab = EnergyTab.Week;
                    break;

                case "_tabMonth":
                    _vm.SelectedTab = EnergyTab.Month;
                    break;

                case "_tabYear":
                    _vm.SelectedTab = EnergyTab.Year;
                    break;

                case "_tabCustom":
                    _vm.SelectedTab = EnergyTab.Custom;
                    break;
            }

            RefreshDashboard();
        }
    }
}
