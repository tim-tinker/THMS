using System.Windows.Forms.DataVisualization.Charting;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms.Charts;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public class InvestmentsDashboardForm : BaseDashboardForm
    {
        private InvestmentDashboardViewModel? _vm;
        private bool _initialized;
        private int _refreshGeneration;

        private readonly Label _value = ValueLabel();
        private readonly Label _incomeMonth = ValueLabel();
        private readonly Label _incomeYear = ValueLabel();
        private readonly Label _estimated = ValueLabel();
        private readonly Label _note = new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DimGray
        };
        private readonly Panel _valueChart = new() { Dock = DockStyle.Fill };
        private readonly Panel _incomeChart = new() { Dock = DockStyle.Fill };
        private readonly DataGridView _months = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        private readonly BindingSource _monthsSource = new();

        public InvestmentsDashboardForm()
        {
            Text = "Investments";
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
        }

        public override void InitializeDashboard()
        {
            if (_initialized)
                return;

            _vm = new InvestmentDashboardViewModel();
            BuildLayout();
            _initialized = true;
        }

        public override void RefreshDashboard()
        {
            if (_vm is null || !_initialized)
                return;

            var generation = Interlocked.Increment(ref _refreshGeneration);
            _ = LoadAsync(generation);
        }

        private async Task LoadAsync(int generation)
        {
            InvestmentDashboardSnapshot snapshot;
            try
            {
                snapshot = await Task.Run(() =>
                {
                    _vm!.Refresh(DateTime.Today);
                    return _vm.Snapshot;
                });
            }
            catch (Exception ex)
            {
                if (IsDisposed || generation != _refreshGeneration)
                    return;
                BeginInvoke(() =>
                {
                    if (!IsDisposed && generation == _refreshGeneration)
                        _note.Text = ex.Message;
                });
                return;
            }

            if (IsDisposed || generation != _refreshGeneration)
                return;
            BeginInvoke(() =>
            {
                if (!IsDisposed && generation == _refreshGeneration)
                    Bind(snapshot);
            });
        }

        private void Bind(InvestmentDashboardSnapshot snapshot)
        {
            _value.Text = snapshot.TotalValue.ToString("c2");
            _incomeMonth.Text = snapshot.IncomeThisMonth.ToString("c2");
            _incomeYear.Text = snapshot.IncomeLastTwelveMonths.ToString("c2");
            _estimated.Text = snapshot.EstimatedAnnualIncome.ToString("c2");
            _note.Text = snapshot.Note;
            _monthsSource.DataSource = snapshot.Months.Reverse().ToList();
            ReplaceChart(_valueChart, InvestmentChartFactory.CreateValueChart(snapshot.Months));
            ReplaceChart(_incomeChart, InvestmentChartFactory.CreateIncomeChart(snapshot.Months));
        }

        private void BuildLayout()
        {
            _months.DataSource = _monthsSource;
            _months.Columns.AddRange(
                TextColumn(nameof(InvestmentDashboardMonthRow.Label), "Month", DataGridViewAutoSizeColumnMode.AllCells),
                MoneyColumn(nameof(InvestmentDashboardMonthRow.Value), "Value"),
                MoneyColumn(nameof(InvestmentDashboardMonthRow.Change), "Change"),
                MoneyColumn(nameof(InvestmentDashboardMonthRow.Income), "Income"),
                TextColumn(nameof(InvestmentDashboardMonthRow.Prices), "Prices", DataGridViewAutoSizeColumnMode.Fill));
            DataGridViewUtil.EnableDoubleBuffering(_months);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            root.Controls.Add(BuildCards(), 0, 0);
            root.Controls.Add(_note, 0, 1);

            var charts = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.Controls.Add(_valueChart, 0, 0);
            charts.Controls.Add(_incomeChart, 1, 0);
            root.Controls.Add(charts, 0, 2);
            root.Controls.Add(_months, 0, 3);
            Controls.Add(root);
        }

        private Control BuildCards()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            for (var i = 0; i < 4; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            table.Controls.Add(Card("Total value", _value), 0, 0);
            table.Controls.Add(Card("Income this month", _incomeMonth), 1, 0);
            table.Controls.Add(Card("Income, last 12 months", _incomeYear), 2, 0);
            table.Controls.Add(Card("Estimated annual income", _estimated), 3, 0);
            return table;
        }

        private static Control Card(string caption, Label value)
        {
            var cell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(4, 0, 4, 0)
            };
            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            cell.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = Color.DimGray
            }, 0, 0);
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.TopLeft;
            cell.Controls.Add(value, 0, 1);
            return cell;
        }

        private static Label ValueLabel() => new()
        {
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Text = "$0.00"
        };

        private static DataGridViewTextBoxColumn TextColumn(
            string property,
            string header,
            DataGridViewAutoSizeColumnMode size) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = size
            };

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                DefaultCellStyle = { Format = "c2", Alignment = DataGridViewContentAlignment.MiddleRight }
            };

        private static void ReplaceChart(Panel host, Chart chart)
        {
            host.Controls.Clear();
            chart.Dock = DockStyle.Fill;
            host.Controls.Add(chart);
        }
    }
}
