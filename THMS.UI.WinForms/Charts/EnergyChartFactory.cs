using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using THMS.Logic.ViewModels.Energy;

namespace THMS.UI.WinForms.Charts
{
    public static class EnergyChartFactory
    {
        // ============================================================
        // DAILY ENERGY FLOW CHART
        // ============================================================
        public static Chart CreateDayEnergyFlowChart(EnergyDayViewModel vm)
        {
            var chart = CreateBaseChart();

            chart.Titles.Add("Energy Flow (Daily)");

            var areaSolar = CreateSeries("Solar", SeriesChartType.Column, Color.Goldenrod);
            var areaBatteryCharge = CreateSeries("Battery", SeriesChartType.Column, Color.LightGreen);
            var areaBatteryDischarge = CreateSeries("Battery Discharge", SeriesChartType.Column, Color.DarkGreen);
            var areaGridImport = CreateSeries("Grid Import", SeriesChartType.Column, Color.SteelBlue);
            var areaGridExport = CreateSeries("Grid Export", SeriesChartType.Column, Color.OrangeRed);
            var areaHome = CreateSeries("Home Consumption", SeriesChartType.Column, Color.Gray);
            var areaEv = CreateSeries("EV Charging", SeriesChartType.Column, Color.MediumPurple);

            foreach (var h in vm.Hours)
            {
                string hourLabel = h.Timestamp.ToString("HH:mm");

                areaSolar.Points.AddXY(hourLabel, h.SolarKwh);
                areaBatteryCharge.Points.AddXY(hourLabel, h.BatteryChargeKwh);
                areaBatteryDischarge.Points.AddXY(hourLabel, h.BatteryDischargeKwh);
                areaGridImport.Points.AddXY(hourLabel, h.GridImportKwh);
                areaGridExport.Points.AddXY(hourLabel, h.GridExportKwh);
                areaHome.Points.AddXY(hourLabel, h.HomeConsumptionKwh);
                areaEv.Points.AddXY(hourLabel, h.EvChargingKwh);
            }

            chart.Series.Add(areaSolar);
            chart.Series.Add(areaBatteryCharge);
            chart.Series.Add(areaBatteryDischarge);
            chart.Series.Add(areaGridImport);
            chart.Series.Add(areaGridExport);
            chart.Series.Add(areaHome);
            chart.Series.Add(areaEv);

            return chart;
        }

        // ============================================================
        // DAILY BATTERY SOC CHART
        // ============================================================
        public static Chart CreateBatterySocChart(EnergyDayViewModel vm)
        {
            var chart = CreateBaseChart();

            chart.Titles.Add("Battery SOC (Daily)");

            var series = CreateSeries("SOC %", SeriesChartType.Line, Color.DarkGreen);
            series.BorderWidth = 3;

            foreach (var soc in vm.BatterySocTimeline)
            {
                string hourLabel = soc.Timestamp.ToString("HH:mm");
                series.Points.AddXY(hourLabel, soc.SocPercent);
            }

            chart.Series.Add(series);

            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisY.Maximum = 100;

            return chart;
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

            foreach (var r in vm.Records)
            {
                string label = r.Date.ToString("MM/dd");

                solar.Points.AddXY(label, r.SolarKwh);
                home.Points.AddXY(label, r.HomeConsumptionKwh);
                gridIn.Points.AddXY(label, r.GridImportKwh);
                gridOut.Points.AddXY(label, r.GridExportKwh);
                battCharge.Points.AddXY(label, r.BatteryChargeKwh);
                battDischarge.Points.AddXY(label, r.BatteryDischargeKwh);
                ev.Points.AddXY(label, r.EvChargingKwh);
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

        private static Series CreateSeries(string name, SeriesChartType type, Color color)
        {
            return new Series(name)
            {
                ChartType = type,
                Color = color,
                BorderWidth = 2
            };
        }
    }
}
