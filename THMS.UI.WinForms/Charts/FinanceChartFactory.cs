using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Charts
{
    public static class FinanceChartFactory
    {
        public static Chart CreateSpendIncomeChart(IReadOnlyList<FinanceDashboardMonthlyPoint> points)
        {
            var chart = CreateBaseChart();
            chart.Titles.Add("Spending vs income (12 months)");
            chart.Legends.Add(new Legend { Docking = Docking.Top });

            var spending = CreateSeries("Spending", SeriesChartType.Column, Color.IndianRed);
            var income = CreateSeries("Income", SeriesChartType.Column, Color.SeaGreen);
            for (var i = 0; i < points.Count; i++)
            {
                AddIndexedPoint(spending, i, points[i].Spending, points[i].Label);
                AddIndexedPoint(income, i, points[i].Income, points[i].Label);
            }

            chart.Series.Add(spending);
            chart.Series.Add(income);
            return chart;
        }

        public static Chart CreateCategoryPie(IReadOnlyList<FinanceDashboardCategorySlice> slices)
        {
            var chart = CreateBaseChart();
            chart.Titles.Add("Spending by category (this month)");
            chart.Legends.Add(new Legend { Docking = Docking.Right });
            chart.ChartAreas[0].Area3DStyle.Enable3D = false;

            var series = CreateSeries("Categories", SeriesChartType.Pie, Color.SteelBlue);
            series.IsValueShownAsLabel = true;
            series.LabelFormat = "C0";
            series["PieLabelStyle"] = "Outside";

            if (slices.Count == 0)
            {
                var index = series.Points.AddY(1);
                series.Points[index].LegendText = "No spending";
                series.Points[index].Label = "";
                series.Points[index].Color = Color.Gainsboro;
            }
            else
            {
                foreach (var slice in slices)
                {
                    var index = series.Points.AddY((double)slice.Amount);
                    series.Points[index].LegendText = slice.Name;
                    series.Points[index].Label = slice.Name;
                }
            }

            chart.Series.Add(series);
            return chart;
        }

        private static Chart CreateBaseChart()
        {
            var chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            chart.ChartAreas.Add(new ChartArea
            {
                BackColor = Color.White,
                AxisX = { Interval = 1, MajorGrid = { Enabled = false } },
                AxisY = { MajorGrid = { LineColor = Color.LightGray }, LabelStyle = { Format = "C0" } }
            });
            return chart;
        }

        private static Series CreateSeries(string name, SeriesChartType type, Color color)
        {
            return new Series(name)
            {
                ChartType = type,
                Color = Color.FromArgb(180, color),
                BorderWidth = 1,
                XValueType = ChartValueType.Int32,
                IsVisibleInLegend = true
            };
        }

        private static void AddIndexedPoint(Series series, int xIndex, decimal y, string axisLabel)
        {
            var pointIndex = series.Points.AddXY(xIndex, (double)y);
            series.Points[pointIndex].AxisLabel = axisLabel;
        }
    }
}
