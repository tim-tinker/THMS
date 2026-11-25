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
        private NetWorthSnapshot _snapshot = new();
        private ToolStripDropDown? _openDrop;
        private Control? _openAnchor;
        private Control? _closedByClickAnchor;
        private int _closedByClickTick;

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

        internal void Bind(NetWorthSnapshot snapshot)
        {
            _snapshot = snapshot;
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
            table.Controls.Add(Card("Cash", _cash, "Cash"), 1, 0);
            table.Controls.Add(Card("Investments", _investments, "Investments"), 2, 0);
            table.Controls.Add(Card("Other assets", _otherAssets, "Property", "Vehicles", "Personal"), 3, 0);
            table.Controls.Add(Card("Debt", _debt, "Debt"), 4, 0);
            return table;
        }

        private Control Card(string caption, Label value, params string[] groups)
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
            var captionLabel = new Label
            {
                Text = groups.Length == 0 ? caption : caption + "  ▾",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = Color.DimGray
            };
            cell.Controls.Add(captionLabel, 0, 0);
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.TopLeft;
            cell.Controls.Add(value, 0, 1);
            if (groups.Length > 0)
            {
                captionLabel.Cursor = Cursors.Hand;
                value.Cursor = Cursors.Hand;
                cell.Cursor = Cursors.Hand;
                captionLabel.Click += (_, _) => ShowBreakdown(cell, groups);
                value.Click += (_, _) => ShowBreakdown(cell, groups);
                cell.Click += (_, _) => ShowBreakdown(cell, groups);
            }

            return cell;
        }

        private void ShowBreakdown(Control anchor, string[] groups)
        {
            // The opening click is also seen as a click outside the menu, so the menu
            // closes before this handler runs. Skip the reopen when that click was on
            // the same total.
            if (ReferenceEquals(_closedByClickAnchor, anchor)
                && (uint)(Environment.TickCount - _closedByClickTick) < 500)
            {
                _closedByClickAnchor = null;
                return;
            }

            if (_openDrop is { Visible: true, IsDisposed: false } open && ReferenceEquals(_openAnchor, anchor))
            {
                open.Close(ToolStripDropDownCloseReason.CloseCalled);
                return;
            }

            var debt = groups.Length == 1 && groups[0] == "Debt";
            var matching = _snapshot.Lines
                .Where(line => line.Amount is not null && groups.Contains(line.Group));
            var lines = (debt
                    ? matching.OrderBy(line => line.Amount)
                    : matching.OrderByDescending(line => line.Amount))
                .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var showGroup = groups.Length > 1;
            var list = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None,
                BorderStyle = BorderStyle.None,
                MultiSelect = false,
                HideSelection = false,
                ShowItemToolTips = true,
                Font = Font
            };
            if (showGroup)
                list.Columns.Add("Group", 90);
            list.Columns.Add("Name", 160);
            list.Columns.Add("Amount", 100, HorizontalAlignment.Right);

            if (lines.Count == 0)
            {
                list.Items.Add("None");
            }
            else
            {
                foreach (var line in lines)
                {
                    var amount = line.Amount!.Value.ToString("c2");
                    var item = showGroup ? list.Items.Add(line.Group) : list.Items.Add(line.Name);
                    if (showGroup)
                        item.SubItems.Add(line.Name);
                    item.SubItems.Add(amount);
                    if (line.Source.Length > 0)
                        item.ToolTipText = line.Source;
                }
            }

            var groupWidth = showGroup ? 70 : 0;
            var nameWidth = 140;
            var amountWidth = 90;
            foreach (ListViewItem item in list.Items)
            {
                if (showGroup)
                    groupWidth = Math.Max(groupWidth, TextRenderer.MeasureText(item.Text, list.Font).Width + 16);
                var name = showGroup && item.SubItems.Count > 1 ? item.SubItems[1].Text : item.Text;
                nameWidth = Math.Max(nameWidth, TextRenderer.MeasureText(name, list.Font).Width + 16);
                var amountIndex = showGroup ? 2 : 1;
                if (item.SubItems.Count > amountIndex)
                    amountWidth = Math.Max(amountWidth, TextRenderer.MeasureText(item.SubItems[amountIndex].Text, list.Font).Width + 16);
            }

            nameWidth = Math.Min(nameWidth, 360);
            groupWidth = Math.Min(groupWidth, 140);
            var rowHeight = list.Font.Height + 10;
            var height = Math.Clamp((Math.Max(1, list.Items.Count) * rowHeight) + 6, rowHeight + 8, 320);
            var scroll = list.Items.Count * rowHeight + 6 > 320;
            var chrome = (scroll ? SystemInformation.VerticalScrollBarWidth : 0) + 4;
            var width = Math.Max(anchor.Width, groupWidth + nameWidth + amountWidth + chrome);
            var used = 0;
            if (showGroup)
            {
                list.Columns[0].Width = groupWidth;
                used = groupWidth;
            }

            var nameColumn = showGroup ? 1 : 0;
            var amountColumn = showGroup ? 2 : 1;
            list.Columns[amountColumn].Width = amountWidth;
            list.Columns[nameColumn].Width = Math.Max(80, width - used - amountWidth - chrome);
            list.Size = new Size(width, height);

            var panel = new Panel { Size = list.Size };
            list.Dock = DockStyle.Fill;
            panel.Controls.Add(list);

            var drop = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                AutoClose = true
            };
            var host = new ToolStripControlHost(panel)
            {
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Size = panel.Size
            };
            drop.Items.Add(host);

            var owner = FindForm() as Control ?? anchor;
            drop.Closed += (_, e) =>
            {
                if (ReferenceEquals(_openDrop, drop))
                {
                    _openDrop = null;
                    _openAnchor = null;
                }

                if (e.CloseReason == ToolStripDropDownCloseReason.AppClicked
                    && !anchor.IsDisposed
                    && anchor.ClientRectangle.Contains(anchor.PointToClient(System.Windows.Forms.Cursor.Position)))
                {
                    _closedByClickAnchor = anchor;
                    _closedByClickTick = Environment.TickCount;
                }

                DeferDispose(owner, drop);
            };
            if (owner.IsHandleCreated && !owner.IsDisposed)
            {
                owner.BeginInvoke(() =>
                {
                    if (drop.IsDisposed || anchor.IsDisposed)
                        return;

                    _openDrop = drop;
                    _openAnchor = anchor;
                    drop.Show(anchor, new Point(0, anchor.Height));
                });
            }
        }

        internal string[][] OpenBreakdownRows()
        {
            if (_openDrop is not { IsDisposed: false, Visible: true })
                return [];

            foreach (ToolStripItem item in _openDrop.Items)
            {
                if (item is not ToolStripControlHost host)
                    continue;
                var list = host.Control as ListView ?? host.Control?.Controls.OfType<ListView>().FirstOrDefault();
                if (list is null)
                    continue;
                return list.Items.Cast<ListViewItem>()
                    .Select(row =>
                    {
                        var texts = new string[row.SubItems.Count];
                        for (var i = 0; i < texts.Length; i++)
                            texts[i] = row.SubItems[i].Text;
                        return texts;
                    })
                    .ToArray();
            }

            return [];
        }

        private static void DeferDispose(Control owner, ToolStripDropDown drop)
        {
            if (owner.IsDisposed || !owner.IsHandleCreated)
            {
                if (!drop.IsDisposed)
                    drop.Dispose();
                return;
            }

            owner.BeginInvoke(() =>
            {
                if (!drop.IsDisposed)
                    drop.Dispose();
            });
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
