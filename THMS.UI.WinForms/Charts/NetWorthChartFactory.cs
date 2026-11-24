using System.Windows.Forms.DataVisualization.Charting;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Charts
{
    public static class NetWorthChartFactory
    {
        public static Chart CreateNetWorthChart(IReadOnlyList<NetWorthMonthRow> months)
        {
            var chart = CreateBaseChart("Net worth");
            var series = CreateSeries("Net worth", SeriesChartType.Line, Color.SteelBlue);
            series.BorderWidth = 2;
            series.MarkerStyle = MarkerStyle.Circle;
            series.MarkerSize = 6;
            series.IsVisibleInLegend = false;
            AddPoints(series, months, row => row.NetWorth);
            chart.Series.Add(series);
            SpaceLabels(chart, months.Count);
            return chart;
        }

        public static Chart CreateCompositionChart(IReadOnlyList<NetWorthMonthRow> months)
        {
            var chart = CreateBaseChart("What you own");
            chart.Legends.Add(new Legend
            {
                Docking = Docking.Bottom,
                BackColor = Color.White
            });
            AddStacked(chart, months, "Cash", Color.SteelBlue, row => row.Cash);
            AddStacked(chart, months, "Investments", Color.DarkOrange, row => row.Investments);
            AddStacked(chart, months, "Property", Color.SeaGreen, row => row.Property);
            AddStacked(chart, months, "Vehicles", Color.MediumPurple, row => row.Vehicles);
            AddStacked(chart, months, "Personal", Color.CadetBlue, row => row.Personal);
            SpaceLabels(chart, months.Count);
            return chart;
        }

        private static void AddStacked(
            Chart chart,
            IReadOnlyList<NetWorthMonthRow> months,
            string name,
            Color color,
            Func<NetWorthMonthRow, decimal> value)
        {
            var series = CreateSeries(name, SeriesChartType.StackedColumn, color);
            series.IsVisibleInLegend = true;
            AddPoints(series, months, value);
            chart.Series.Add(series);
        }

        private static void SpaceLabels(Chart chart, int count)
        {
            if (count > 18)
                chart.ChartAreas[0].AxisX.Interval = Math.Max(1, count / 12);
        }

        private static void AddPoints(
            Series series,
            IReadOnlyList<NetWorthMonthRow> months,
            Func<NetWorthMonthRow, decimal> value)
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
                XValueType = ChartValueType.Int32
            };
    }
}
