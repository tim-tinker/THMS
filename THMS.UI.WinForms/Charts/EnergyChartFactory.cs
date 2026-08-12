using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using THMS.Domain.Energy;
using THMS.Logic.Energy;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Charts
{
    public static class EnergyChartFactory
    {
        // ============================================================
        // DAILY ENERGY FLOW CHART
        // ============================================================
        public static Chart CreateDayChart(EnergyPeriodViewModel vm)
        {
            var chart = CreateBaseChart();
            chart.Titles.Add($"Energy Flow ({vm.Start:yyyy-MM-dd})");

            var solar = CreateSeries("Solar", SeriesChartType.Line, Color.Goldenrod, 5);
            var home = CreateSeries("Home Consumption", SeriesChartType.Line, Color.SteelBlue, 5);
            var gridImport = CreateSeries("Grid Import", SeriesChartType.Line, Color.Red, 2);
            var gridExport = CreateSeries("Grid Export", SeriesChartType.Line, Color.Green, 2);
            var ev = CreateSeries("EV Charging", SeriesChartType.Column, Color.MediumPurple, 5);

            int index = 0;
            foreach (var interval in vm.Intervals)
            {
                string label = interval.Timestamp.Minute == 0 && interval.Timestamp.Hour % 3 == 0
                    ? interval.Timestamp.ToString("HH:mm") : string.Empty;

                AddIndexedPoint(solar, index, interval.SolarKwh, label);

                AddIndexedPoint(home, index, -interval.HomeConsumptionKwh, label);
                AddIndexedPoint(gridImport, index, interval.GridImportKwh, label);
                AddIndexedPoint(gridExport, index, interval.GridExportKwh, label);
                AddIndexedPoint(ev, index, interval.EvChargeKwh, label);

                index++;
            }

            chart.Series.Add(home);
            chart.Series.Add(gridImport);
            chart.Series.Add(ev);

            chart.Series.Add(solar);
            chart.Series.Add(gridExport);

            chart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
            chart.ChartAreas[0].AxisX.Interval = 1;
            chart.ChartAreas[0].AxisX.IsMarksNextToAxis = false;
            chart.ChartAreas[0].AxisY.Crossing = 0;

            chart.Legends.Add(new Legend("Legend"));

            var showBatteryShading = true;
            if (showBatteryShading && vm.Intervals.Count > 0)
            {
                chart.PostPaint += (sender, e) =>
                {
                    if (e.ChartElement is ChartArea area)
                    {
                        DrawBatteryShading(e.ChartGraphics.Graphics, area, vm);
                    }
                };
            }

            return chart;
        }

        private static void DrawBatteryShading(Graphics g, ChartArea area, EnergyPeriodViewModel vm)
        {
            var ca = area;

            var xAxisIndex = 0;
            foreach (var interval in vm.Intervals)
            {
                xAxisIndex++;

                // Determine battery direction
                bool charging = interval.BatteryChargeKwh > 0;
                bool discharging = interval.BatteryDischargeKwh > 0;

                if (!charging && !discharging)
                    continue;

                // Pick shading color
                Color shade = charging
                    ? discharging
                        ? Color.FromArgb(40, Color.Yellow)      // translucent yellow
                        : Color.FromArgb(40, Color.Green)       // translucent green
                    : discharging
                        ? Color.FromArgb(40, Color.Orange)      // translucent orange
                        : Color.FromArgb(0, 0, 0, 0);           // No color (if statement prevents this case)

                // Convert time range to pixel coordinates
                // each interval is 30 minutes
                double xStart = ca.AxisX.ValueToPixelPosition(xAxisIndex - 1);
                double xEnd = ca.AxisX.ValueToPixelPosition(xAxisIndex);

                // Full height of chart area
                double yTop = ca.AxisY.ValueToPixelPosition(ca.AxisY.Maximum);
                double yBottom = ca.AxisY.ValueToPixelPosition(ca.AxisY.Minimum);

                // Draw rectangle
                var rect = new RectangleF(
                    (float)xStart,
                    (float)yTop,
                    (float)(xEnd - xStart),
                    (float)(yBottom - yTop));

                g.FillRectangle(new SolidBrush(shade), rect);
            }
        }
        


        // ============================================================
        // PERIOD BAR CHART (Week / Month / Year / Custom)
        // ============================================================
        public static Chart CreatePeriodBarChart(EnergyPeriodViewModel vm)
        {
            var chart = CreateBaseChart();

            chart.Titles.Add("Energy Summary");

            var solar = CreateSeries("Solar", SeriesChartType.Column, Color.Goldenrod);
            var home = CreateSeries("Home Consumption", SeriesChartType.Column, Color.Gray);
            var gridIn = CreateSeries("Grid Import", SeriesChartType.Column, Color.SteelBlue);
            var gridOut = CreateSeries("Grid Export", SeriesChartType.Column, Color.OrangeRed);
            var battCharge = CreateSeries("Battery Charge", SeriesChartType.Column, Color.LightGreen);
            var battDischarge = CreateSeries("Battery Discharge", SeriesChartType.Column, Color.DarkGreen);
            var ev = CreateSeries("EV Charging", SeriesChartType.Column, Color.MediumPurple);

            int index = 0;
            foreach (var r in vm.Records)
            {
                string label = r.Date.ToString("MM/dd");

                AddIndexedPoint(solar, index, r.SolarKwh, label);
                AddIndexedPoint(home, index, -r.HomeConsumptionKwh, label);
                AddIndexedPoint(gridIn, index, -r.GridImportKwh, label);
                AddIndexedPoint(gridOut, index, r.GridExportKwh, label);
                AddIndexedPoint(battCharge, index, r.BatteryChargeKwh, label);
                AddIndexedPoint(battDischarge, index, -r.BatteryDischargeKwh, label);
                AddIndexedPoint(ev, index, -r.EvChargeKwh, label);

                index++;
            }

            chart.Series.Add(solar);
            chart.Series.Add(home);
            chart.Series.Add(gridIn);
            chart.Series.Add(gridOut);
            chart.Series.Add(battCharge);
            chart.Series.Add(battDischarge);
            chart.Series.Add(ev);

            return chart;
        }

        // ============================================================
        // BASE CHART CONFIGURATION
        // ============================================================
        private static Chart CreateBaseChart()
        {
            var chart = new Chart
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = Color.White
            };

            var area = new ChartArea
            {
                BackColor = Color.White,
                AxisX = { Interval = 1, MajorGrid = { Enabled = false } },
                AxisY = { MajorGrid = { LineColor = Color.LightGray } }
            };

            chart.ChartAreas.Add(area);

            return chart;
        }

        private static Series CreateSeries(string name, SeriesChartType type, Color color, int width = 2)
        {
            var series = new Series(name)
            {
                ChartType = type,
                Color = Color.FromArgb(128, color),
                BorderWidth = width,
                BorderDashStyle = ChartDashStyle.Solid,
                XValueType = ChartValueType.Int32,
                IsVisibleInLegend = true,
            };
            series["LineTension"] = "0.4"; // For smooth lines
            series["PointWidth"] = "1.0"; // For bar width

            return series;
        }

        private static void AddIndexedPoint(Series series, int xIndex, decimal y, string axisLabel)
        {
            int pointIndex = series.Points.AddXY(xIndex, (double)y);
            series.Points[pointIndex].AxisLabel = axisLabel;
        }
    }
}
