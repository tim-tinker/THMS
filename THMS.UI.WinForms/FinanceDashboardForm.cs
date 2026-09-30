using System.Windows.Forms.DataVisualization.Charting;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms.Charts;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class FinanceDashboardForm : BaseDashboardForm
    {
        private FinanceDashboardViewModel _vm = null!;
        private bool _initialized;
        private readonly BudgetOrchestrator _budgets = new();

        private readonly Label _valueBank = NewValueLabel();
        private readonly Label _valueCredit = NewValueLabel();
        private readonly Label _valueLoans = NewValueLabel();
        private readonly Label _valueInvest = NewValueLabel();
        private readonly Label _valueLiquid = NewValueLabel();
        private readonly Label _valueNet = NewValueLabel();
        private readonly AccountBreakdown _bankAccounts = new();
        private readonly AccountBreakdown _creditAccounts = new();
        private readonly AccountBreakdown _loanAccounts = new();
        private readonly AccountBreakdown _investmentAccounts = new();

        private DataGridView _budgetGrid = null!;
        private DataGridView _paymentGrid = null!;
        private DataGridView _recentGrid = null!;
        private DataGridView _forecastGrid = null!;
        private ListBox _alerts = null!;
        private Panel _trendHost = null!;
        private Panel _pieHost = null!;
        private BindingSource _budgetsSource = new();
        private BindingSource _paymentsSource = new();
        private BindingSource _recentSource = new();
        private BindingSource _forecastSource = new();
        private ToolStripDropDown? _openDrop;
        private Control? _openAnchor;
        private Control? _closedByClickAnchor;
        private int _closedByClickTick;

        public FinanceDashboardForm()
        {
            InitializeComponent();
        }

        public override void InitializeDashboard()
        {
            if (_initialized)
                return;

            _vm = new FinanceDashboardViewModel();
            BuildLayout();
            _initialized = true;
            RefreshDashboard();
        }

        public override void RefreshDashboard()
        {
            if (_vm is null || !_initialized)
                return;

            _vm.Refresh();
            BindSnapshot(_vm.Snapshot);
        }

        private void BindSnapshot(FinanceDashboardSnapshot snapshot)
        {
            _valueBank.Text = snapshot.BankBalance.ToString("c2");
            _valueCredit.Text = snapshot.CreditOwed.ToString("c2");
            _valueLoans.Text = snapshot.LoanPrincipal.ToString("c2");
            _valueInvest.Text = snapshot.InvestmentCash.ToString("c2");
            _valueLiquid.Text = snapshot.NetLiquid.ToString("c2");
            _valueNet.Text = snapshot.NetPosition.ToString("c2");
            _bankAccounts.Accounts = snapshot.BankAccounts;
            _creditAccounts.Accounts = snapshot.CreditAccounts;
            _loanAccounts.Accounts = snapshot.LoanAccounts;
            _investmentAccounts.Accounts = snapshot.InvestmentAccounts;

            _budgetsSource.DataSource = snapshot.Budgets.ToList();
            _paymentsSource.DataSource = snapshot.UpcomingPayments.ToList();
            _recentSource.DataSource = snapshot.RecentPosted.ToList();
            _forecastSource.DataSource = snapshot.UpcomingForecast.ToList();
            _alerts.DataSource = null;
            _alerts.DataSource = snapshot.Alerts.Count == 0 ? new List<string> { "No alerts." } : snapshot.Alerts.ToList();

            ReplaceChart(_trendHost, FinanceChartFactory.CreateSpendIncomeChart(snapshot.MonthlyTrend));
            ReplaceChart(_pieHost, FinanceChartFactory.CreateCategoryPie(snapshot.CategorySlices));
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(8)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
            root.Controls.Add(BuildCards(), 0, 0);
            root.Controls.Add(BuildMiddle(), 0, 1);
            root.Controls.Add(BuildCharts(), 0, 2);
            root.Controls.Add(BuildActivity(), 0, 3);
            Controls.Add(root);
        }

        private Control BuildCards()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1
            };
            for (var i = 0; i < 6; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66F));

            table.Controls.Add(CreateCard("Bank", _valueBank, _bankAccounts), 0, 0);
            table.Controls.Add(CreateCard("Credit owed", _valueCredit, _creditAccounts), 1, 0);
            table.Controls.Add(CreateCard("Loans / mortgages", _valueLoans, _loanAccounts), 2, 0);
            table.Controls.Add(CreateCard("Investment cash", _valueInvest, _investmentAccounts), 3, 0);
            table.Controls.Add(CreateCard("Net liquid", _valueLiquid), 4, 0);
            table.Controls.Add(CreateCard("Net position", _valueNet), 5, 0);
            return table;
        }

        private Control BuildMiddle()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            split.Panel1.Controls.Add(BuildBudgetPanel());
            split.Panel2.Controls.Add(BuildSidePanel());
            SplitContainerUtil.MakeSplitterVisible(split);
            Shown += (_, _) =>
            {
                if (split.Width > 40)
                    split.SplitterDistance = Math.Max(240, (int)(split.Width * 0.58));
            };
            return split;
        }

        private Control BuildBudgetPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            _budgetGrid = CreateGrid();
            _budgetGrid.DataSource = _budgetsSource;
            _budgetGrid.Columns.AddRange(
                TextColumn("BudgetName", "Budget", DataGridViewAutoSizeColumnMode.AllCells, 140),
                CurrencyColumn("Remaining", "Remaining"),
                DateColumn("PeriodStart", "Start"),
                DateColumn("PeriodEnd", "End"),
                CurrencyColumn("Recommended", "Recommended"),
                TextColumn("Status", "Status", DataGridViewAutoSizeColumnMode.Fill, 120));
            _budgetGrid.CellFormatting += OnBudgetCellFormatting;
            _budgetGrid.CellDoubleClick += (_, _) => OpenSelectedPeriod();

            panel.Controls.Add(_budgetGrid);
            return panel;
        }

        private Control BuildSidePanel()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            _alerts = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            split.Panel1.Controls.Add(_alerts);
            split.Panel1.Controls.Add(CreateSectionHeader("Alerts"));

            _paymentGrid = CreateGrid();
            _paymentGrid.DataSource = _paymentsSource;
            _paymentGrid.Columns.AddRange(
                DateColumn("Date", "Date"),
                TextColumn("AccountName", "Account", DataGridViewAutoSizeColumnMode.AllCells, 110),
                TextColumn("Description", "Description", DataGridViewAutoSizeColumnMode.Fill, 140),
                CurrencyColumn("Amount", "Amount"));
            split.Panel2.Controls.Add(_paymentGrid);
            split.Panel2.Controls.Add(CreateSectionHeader("Upcoming payments"));
            SplitContainerUtil.MakeSplitterVisible(split);
            Shown += (_, _) =>
            {
                if (split.Height > 40)
                    split.SplitterDistance = Math.Max(80, split.Height / 2);
            };
            return split;
        }

        private Control BuildCharts()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var trendPage = new TabPage("Spend vs Income");
            var piePage = new TabPage("Categories");
            _trendHost = new Panel { Dock = DockStyle.Fill };
            _pieHost = new Panel { Dock = DockStyle.Fill };
            trendPage.Controls.Add(_trendHost);
            piePage.Controls.Add(_pieHost);
            tabs.TabPages.Add(trendPage);
            tabs.TabPages.Add(piePage);
            return tabs;
        }

        private Control BuildActivity()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var recentPage = new TabPage("Recent Posted");
            var forecastPage = new TabPage("Upcoming Forecast");

            _recentGrid = CreateGrid();
            _recentGrid.DataSource = _recentSource;
            _recentGrid.Columns.AddRange(
                DateColumn("Date", "Date"),
                TextColumn("Description", "Description", DataGridViewAutoSizeColumnMode.Fill, 220),
                CurrencyColumn("Amount", "Amount"),
                TextColumn("Category", "Category", DataGridViewAutoSizeColumnMode.AllCells, 120));

            _forecastGrid = CreateGrid();
            _forecastGrid.DataSource = _forecastSource;
            _forecastGrid.Columns.AddRange(
                DateColumn("Date", "Date"),
                TextColumn("Description", "Description", DataGridViewAutoSizeColumnMode.Fill, 220),
                CurrencyColumn("Amount", "Amount"),
                TextColumn("Category", "Category", DataGridViewAutoSizeColumnMode.AllCells, 120));

            recentPage.Controls.Add(_recentGrid);
            forecastPage.Controls.Add(_forecastGrid);
            tabs.TabPages.Add(recentPage);
            tabs.TabPages.Add(forecastPage);
            return tabs;
        }

        private FinanceDashboardBudgetRow? SelectedBudget() =>
            _budgetGrid.CurrentRow?.DataBoundItem as FinanceDashboardBudgetRow;

        private void OpenSelectedPeriod()
        {
            if (SelectedBudget() is not FinanceDashboardBudgetRow row)
                return;
            using var editor = new BudgetPeriodEditor(_budgets, row.RuleId);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshDashboard();
        }

        private void OnBudgetCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_budgetGrid.Columns[e.ColumnIndex].DataPropertyName != "Remaining")
                return;
            if (e.Value is decimal remaining && remaining < 0)
                DataGridViewUtil.SetContentForeColor(e.CellStyle, Color.Firebrick);
        }

        private static void ReplaceChart(Panel host, Chart chart)
        {
            host.Controls.Clear();
            chart.Dock = DockStyle.Fill;
            host.Controls.Add(chart);
        }

        private Label CreateSectionHeader(string text)
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

        private Control CreateCard(string caption, Label value, AccountBreakdown? breakdown = null)
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
                Text = breakdown is null ? caption : caption + "  ▾",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };
            cell.Controls.Add(captionLabel, 0, 0);
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.TopLeft;
            cell.Controls.Add(value, 0, 1);
            if (breakdown is not null)
            {
                captionLabel.Cursor = Cursors.Hand;
                value.Cursor = Cursors.Hand;
                cell.Cursor = Cursors.Hand;
                captionLabel.Click += (_, _) => ShowAccountDropDown(cell, breakdown);
                value.Click += (_, _) => ShowAccountDropDown(cell, breakdown);
                cell.Click += (_, _) => ShowAccountDropDown(cell, breakdown);
            }

            return cell;
        }

        private void ShowAccountDropDown(Control anchor, AccountBreakdown breakdown)
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

            var accounts = breakdown.Accounts;
            var list = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None,
                BorderStyle = BorderStyle.None,
                MultiSelect = false,
                HideSelection = false,
                Font = Font
            };
            list.Columns.Add("Account", 160);
            list.Columns.Add("Balance", 100, HorizontalAlignment.Right);

            if (accounts.Count == 0)
            {
                list.Items.Add("No accounts");
            }
            else
            {
                foreach (var account in accounts)
                {
                    var item = list.Items.Add(account.Name);
                    item.SubItems.Add(account.Balance.ToString("c2"));
                    item.Tag = account.AccountId;
                }
            }

            var nameWidth = 140;
            var balanceWidth = 90;
            foreach (ListViewItem item in list.Items)
            {
                nameWidth = Math.Max(nameWidth, TextRenderer.MeasureText(item.Text, list.Font).Width + 16);
                if (item.SubItems.Count > 1)
                    balanceWidth = Math.Max(balanceWidth, TextRenderer.MeasureText(item.SubItems[1].Text, list.Font).Width + 16);
            }

            nameWidth = Math.Min(nameWidth, 360);
            var rowHeight = list.Font.Height + 10;
            var height = Math.Clamp((Math.Max(1, list.Items.Count) * rowHeight) + 6, rowHeight + 8, 320);
            var scroll = list.Items.Count * rowHeight + 6 > 320;
            var width = Math.Max(anchor.Width, nameWidth + balanceWidth + (scroll ? SystemInformation.VerticalScrollBarWidth : 0) + 4);
            list.Columns[0].Width = width - balanceWidth - (scroll ? SystemInformation.VerticalScrollBarWidth : 0) - 4;
            list.Columns[1].Width = balanceWidth;
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
            list.MouseClick += (_, e) =>
            {
                if (list.HitTest(e.Location).Item?.Tag is not Guid accountId)
                    return;

                drop.Close(ToolStripDropDownCloseReason.ItemClicked);
                OpenAccount(accountId);
            };

            // Closed runs inside SetVisibleCore. Disposing there leaves the menu
            // filter holding a dead dropdown, and the next click throws.
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

        private void OpenAccount(Guid accountId)
        {
            if (FindForm() is THMS.UI.MainForm main)
                main.ShowRegisterAccount(accountId);
        }

        private sealed class AccountBreakdown
        {
            public IReadOnlyList<FinanceDashboardAccountLine> Accounts { get; set; } = [];
        }

        private static Label NewValueLabel() => new()
        {
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Text = "$0.00"
        };

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
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
            DataGridViewUtil.EnableDoubleBuffering(grid);
            return grid;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header, DataGridViewAutoSizeColumnMode autoSizeMode, int width) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = autoSizeMode,
                Width = width
            };

        private static DataGridViewTextBoxColumn CurrencyColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                DefaultCellStyle = { Format = "c2", Alignment = DataGridViewContentAlignment.MiddleRight }
            };

        private static DataGridViewTextBoxColumn DateColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                DefaultCellStyle = { Format = "d" }
            };
    }
}
