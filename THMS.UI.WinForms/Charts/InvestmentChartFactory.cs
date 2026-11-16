using System.Windows.Forms.DataVisualization.Charting;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Charts
{
    public static class InvestmentChartFactory
    {
        public static Chart CreateValueChart(IReadOnlyList<InvestmentDashboardMonthRow> months)
        {
            var chart = CreateBaseChart("Total value");
            var series = CreateSeries("Total value", SeriesChartType.Line, Color.SteelBlue);
            series.BorderWidth = 2;
            series.MarkerStyle = MarkerStyle.Circle;
            series.MarkerSize = 6;
            AddPoints(series, months, row => row.Value);
            chart.Series.Add(series);
            SpaceLabels(chart, months.Count);
            return chart;
        }

        public static Chart CreateIncomeChart(IReadOnlyList<InvestmentDashboardMonthRow> months)
        {
            var chart = CreateBaseChart("Income received");
            var series = CreateSeries("Income received", SeriesChartType.Column, Color.SeaGreen);
            AddPoints(series, months, row => row.Income);
            chart.Series.Add(series);
            SpaceLabels(chart, months.Count);
            return chart;
        }

        private static void SpaceLabels(Chart chart, int count)
        {
            if (count > 18)
                chart.ChartAreas[0].AxisX.Interval = Math.Max(1, count / 12);
        }

        private static void AddPoints(
            Series series,
            IReadOnlyList<InvestmentDashboardMonthRow> months,
            Func<InvestmentDashboardMonthRow, decimal> value)
        {
            for (var i = 0; i < months.Count; i++)
            {
                var index = series.Points.AddXY(i, (double)value(months[i]));
                series.Points[index].AxisLabel = months[i].Label;
                series.Points[index].Tag = months[i].Month;
            }
        }

        private static Chart CreateBaseChart(string title)
        {
            var chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            chart.Titles.Add(title);
            chart.ChartAreas.Add(new ChartArea
            {
                BackColor = Color.White,
                AxisX = { Interval = 1, MajorGrid = { Enabled = false } },
                AxisY = { MajorGrid = { LineColor = Color.LightGray }, LabelStyle = { Format = "C0" } }
            });
            return chart;
        }

        private static Series CreateSeries(string name, SeriesChartType type, Color color) =>
            new(name)
            {
                ChartType = type,
                Color = Color.FromArgb(180, color),
                BorderWidth = 1,
                XValueType = ChartValueType.Int32,
                IsVisibleInLegend = false
            };
    }
}
