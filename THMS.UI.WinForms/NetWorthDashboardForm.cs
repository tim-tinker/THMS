using System.Windows.Forms.DataVisualization.Charting;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms.Charts;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public class NetWorthDashboardForm : BaseDashboardForm
    {
        private NetWorthDashboardViewModel? _vm;
        private bool _initialized;
        private int _refreshGeneration;

        private readonly Label _netWorth = ValueLabel();
        private readonly Label _cash = ValueLabel();
        private readonly Label _investments = ValueLabel();
        private readonly Label _otherAssets = ValueLabel();
        private readonly Label _debt = ValueLabel();
        private readonly Label _note = new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DimGray
        };
        private readonly Panel _netWorthChart = new() { Dock = DockStyle.Fill };
        private readonly Panel _compositionChart = new() { Dock = DockStyle.Fill };
        private readonly DataGridView _months = Grid();
        private readonly DataGridView _lines = Grid();
        private readonly BindingSource _monthsSource = new();
        private readonly BindingSource _linesSource = new();

        public NetWorthDashboardForm()
        {
            Text = "Net Worth";
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
        }

        public override void InitializeDashboard()
        {
            if (_initialized)
                return;

            _vm = new NetWorthDashboardViewModel();
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
            NetWorthSnapshot snapshot;
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

        private void Bind(NetWorthSnapshot snapshot)
        {
            _netWorth.Text = snapshot.NetWorth.ToString("c2");
            _cash.Text = snapshot.Cash.ToString("c2");
            _investments.Text = snapshot.Investments.ToString("c2");
            _otherAssets.Text = snapshot.OtherAssets.ToString("c2");
            _debt.Text = snapshot.Debt.ToString("c2");
            _note.Text = snapshot.Note;
            _monthsSource.DataSource = snapshot.Months.Reverse().ToList();
            _linesSource.DataSource = snapshot.Lines.ToList();
            ReplaceChart(_netWorthChart, NetWorthChartFactory.CreateNetWorthChart(snapshot.Months));
            ReplaceChart(_compositionChart, NetWorthChartFactory.CreateCompositionChart(snapshot.Months));
        }

        private void BuildLayout()
        {
            _months.DataSource = _monthsSource;
            _months.Columns.AddRange(
                TextColumn(nameof(NetWorthMonthRow.Label), "Month", DataGridViewAutoSizeColumnMode.AllCells),
                MoneyColumn(nameof(NetWorthMonthRow.NetWorth), "Net worth"),
                MoneyColumn(nameof(NetWorthMonthRow.Change), "Change"),
                MoneyColumn(nameof(NetWorthMonthRow.Cash), "Cash"),
                MoneyColumn(nameof(NetWorthMonthRow.Investments), "Investments"),
                MoneyColumn(nameof(NetWorthMonthRow.Property), "Property"),
                MoneyColumn(nameof(NetWorthMonthRow.Vehicles), "Vehicles"),
                MoneyColumn(nameof(NetWorthMonthRow.Personal), "Personal"),
                MoneyColumn(nameof(NetWorthMonthRow.Debt), "Debt"));
            _lines.DataSource = _linesSource;
            _lines.Columns.AddRange(
                TextColumn(nameof(NetWorthLine.Group), "Group", DataGridViewAutoSizeColumnMode.AllCells),
                TextColumn(nameof(NetWorthLine.Name), "Name", DataGridViewAutoSizeColumnMode.AllCells),
                MoneyColumn(nameof(NetWorthLine.Amount), "Amount"),
                TextColumn(nameof(NetWorthLine.Source), "Source", DataGridViewAutoSizeColumnMode.Fill));
            DataGridViewUtil.EnableDoubleBuffering(_months);
            DataGridViewUtil.EnableDoubleBuffering(_lines);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));

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
            charts.Controls.Add(_netWorthChart, 0, 0);
            charts.Controls.Add(_compositionChart, 1, 0);
            root.Controls.Add(charts, 0, 2);
            root.Controls.Add(BuildDetail(), 0, 3);
            Controls.Add(root);
        }

        private Control BuildDetail()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            split.Panel1.Controls.Add(_months);
            split.Panel1.Controls.Add(SectionHeader("History"));
            split.Panel2.Controls.Add(_lines);
            split.Panel2.Controls.Add(SectionHeader("Today"));
            SplitContainerUtil.MakeSplitterVisible(split);
            Shown += (_, _) => SplitContainerUtil.TrySetLeadingRatio(split, 55, 45);
            return split;
        }

        private Control BuildCards()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };
            for (var i = 0; i < 5; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            table.Controls.Add(Card("Net worth", _netWorth), 0, 0);
            table.Controls.Add(Card("Cash", _cash), 1, 0);
            table.Controls.Add(Card("Investments", _investments), 2, 0);
            table.Controls.Add(Card("Other assets", _otherAssets), 3, 0);
            table.Controls.Add(Card("Debt", _debt), 4, 0);
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

        private Label SectionHeader(string text)
        {
            var font = new Font(Font, FontStyle.Bold);
            var padding = new Padding(0, 4, 0, 4);
            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = TextRenderer.MeasureText("Ag", font).Height + padding.Vertical,
                Padding = padding,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = font
            };
        }

        private static DataGridView Grid() => new()
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
