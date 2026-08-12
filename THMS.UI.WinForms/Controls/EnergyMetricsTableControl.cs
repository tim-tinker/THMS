using System;
using System.Drawing;
using System.Windows.Forms;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Controls
{
    public class EnergyMetricsTableControl : UserControl
    {
        private readonly TabControl _tabs;
        private readonly DataGridView _statsGrid;
        private readonly TableLayoutPanel _analyticsPanel;

        public EnergyMetricsTableControl()
        {
            Dock = DockStyle.Fill;

            _tabs = new TabControl { Dock = DockStyle.Fill };

            var statsTab = new TabPage("Statistics");
            var analyticsTab = new TabPage("Analytics");

            _tabs.TabPages.Add(statsTab);
            _tabs.TabPages.Add(analyticsTab);

            _statsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            statsTab.Controls.Add(_statsGrid);

            _analyticsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
            };

            analyticsTab.Controls.Add(_analyticsPanel);

            Controls.Add(_tabs);

            BuildStatsGridColumns();
        }

        private void BuildStatsGridColumns()
        {
            _statsGrid.Columns.Clear();

            _statsGrid.Columns.Add("Metric", "Metric");
            _statsGrid.Columns.Add("Total", "Total");
            _statsGrid.Columns.Add("Min", "Min");
            _statsGrid.Columns.Add("Max", "Max");
            _statsGrid.Columns.Add("Avg", "Avg");
            _statsGrid.Columns.Add("StdDev", "Std Dev");
            _statsGrid.Columns.Add("PeakStart", "Peak Start");
            _statsGrid.Columns.Add("PeakEnd", "Peak End");
        }

        // ---------------------------------------------------------
        // Public API: bind a whole period
        // ---------------------------------------------------------
        public void Bind(EnergyPeriodViewModel period)
        {
            BindStatsTable(period);
            BindAnalyticsTable(period);
        }

        // ---------------------------------------------------------
        // Table 1: Statistical metrics (period-level)
        // ---------------------------------------------------------
        private void BindStatsTable(EnergyPeriodViewModel period)
        {
            _statsGrid.Rows.Clear();

            AddMetricRow("Solar", period.SolarKwh, period, r => r.SolarKwh,
                period.PeriodSolarPeakTime?.TimeOfDay, null);

            AddMetricRow("Home", period.HomeConsumptionKwh, period, r => r.HomeConsumptionKwh,
                period.PeriodPeakWindowStart, period.PeriodPeakWindowEnd);

            AddMetricRow("Grid Import", period.GridImportKwh, period, r => r.GridImportKwh);
            AddMetricRow("Grid Export", period.GridExportKwh, period, r => r.GridExportKwh);
            AddMetricRow("Battery Charge", period.BatteryChargeKwh, period, r => r.BatteryChargeKwh);
            AddMetricRow("Battery Discharge", period.BatteryDischargeKwh, period, r => r.BatteryDischargeKwh);
            AddMetricRow("EV Charge", period.EvChargeKwh, period, r => r.EvChargeKwh);
        }

        private void AddMetricRow(
            string metric,
            decimal total,
            EnergyPeriodViewModel period,
            Func<EnergyPeriodRecordViewModel, decimal> dailySelector,
            TimeSpan? peakStart = null,
            TimeSpan? peakEnd = null)
        {
            var (min, max, avg, std) = period.GetDailyStats(dailySelector);
            AddStatsRow(metric, total, min, max, avg, std, peakStart, peakEnd);
        }

        private void AddStatsRow(
            string metric,
            decimal total,
            decimal min,
            decimal max,
            decimal avg,
            decimal std,
            TimeSpan? peakStart,
            TimeSpan? peakEnd)
        {
            _statsGrid.Rows.Add(
                metric,
                total.ToString("0.##"),
                min.ToString("0.##"),
                max.ToString("0.##"),
                avg.ToString("0.##"),
                std.ToString("0.##"),
                peakStart?.ToString(@"hh\:mm") ?? "",
                peakEnd?.ToString(@"hh\:mm") ?? ""
            );
        }

        // ---------------------------------------------------------
        // Table 2: Specialized analytics (period-level)
        // ---------------------------------------------------------
        private void BindAnalyticsTable(EnergyPeriodViewModel period)
        {
            _analyticsPanel.Controls.Clear();
            _analyticsPanel.RowStyles.Clear();
            _analyticsPanel.RowCount = 0;

            AddAnalyticsRow("Solar Peak (kW)", period.PeriodSolarPeakKw.ToString("0.##"));
            AddAnalyticsRow("Solar Peak Time", period.PeriodSolarPeakTime?.ToString("HH:mm") ?? "");

            AddAnalyticsRow("Battery Contribution to Home Load",
                period.BatteryContributionToHomeLoad.ToString("0.##"));

            AddAnalyticsRow("EV Solar (kWh)", period.EvSolarKwhTotal.ToString("0.##"));
            AddAnalyticsRow("EV Grid (kWh)", period.EvGridKwhTotal.ToString("0.##"));
            AddAnalyticsRow("EV Battery (kWh)", period.EvBatteryKwhTotal.ToString("0.##"));
        }

        private void AddAnalyticsRow(string label, string value)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(0, 5, 0, 5)
            };

            var val = new Label
            {
                Text = value,
                AutoSize = true,
                Padding = new Padding(10, 5, 0, 5)
            };

            _analyticsPanel.RowCount++;
            _analyticsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _analyticsPanel.Controls.Add(lbl, 0, _analyticsPanel.RowCount - 1);
            _analyticsPanel.Controls.Add(val, 1, _analyticsPanel.RowCount - 1);
        }
    }
}
