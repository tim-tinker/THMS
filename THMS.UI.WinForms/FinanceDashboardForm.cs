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
        private readonly CategoryOrchestrator _categories = new();

        private readonly Label _valueBank = NewValueLabel();
        private readonly Label _valueCredit = NewValueLabel();
        private readonly Label _valueLoans = NewValueLabel();
        private readonly Label _valueInvest = NewValueLabel();
        private readonly Label _valueLiquid = NewValueLabel();
        private readonly Label _valueNet = NewValueLabel();

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

            table.Controls.Add(CreateCard("Bank", _valueBank), 0, 0);
            table.Controls.Add(CreateCard("Credit owed", _valueCredit), 1, 0);
            table.Controls.Add(CreateCard("Loans / mortgages", _valueLoans), 2, 0);
            table.Controls.Add(CreateCard("Investment cash", _valueInvest), 3, 0);
            table.Controls.Add(CreateCard("Net liquid", _valueLiquid), 4, 0);
            table.Controls.Add(CreateCard("Net position", _valueNet), 5, 0);
            return table;
        }

        private Control BuildMiddle()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 8
            };
            split.Panel1.Controls.Add(BuildBudgetPanel());
            split.Panel2.Controls.Add(BuildSidePanel());
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
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(0, 0, 0, 4)
            };
            var btnPeriod = new ThmsButton { Text = "Open Current Period" };
            var btnTransfer = new ThmsButton { Text = "Transfer Balance" };
            var btnHistory = new ThmsButton { Text = "View History" };
            var btnCategories = new ThmsButton { Text = "Manage Categories" };
            var btnLedger = new ThmsButton { Text = "Open Ledger" };
            btnPeriod.Click += (_, _) => OpenSelectedPeriod();
            btnTransfer.Click += (_, _) => OpenSelectedTransfer();
            btnHistory.Click += (_, _) => OpenSelectedHistory();
            btnCategories.Click += (_, _) => OpenCategories();
            btnLedger.Click += (_, _) => OpenLedger();
            toolbar.Controls.Add(btnPeriod);
            toolbar.Controls.Add(btnTransfer);
            toolbar.Controls.Add(btnHistory);
            toolbar.Controls.Add(btnCategories);
            toolbar.Controls.Add(btnLedger);

            _budgetGrid = CreateGrid();
            _budgetGrid.DataSource = _budgetsSource;
            _budgetGrid.Columns.AddRange(
                TextColumn("BudgetName", "Budget", DataGridViewAutoSizeColumnMode.AllCells, 140),
                TextColumn("AccountName", "Account", DataGridViewAutoSizeColumnMode.AllCells, 100),
                CurrencyColumn("Remaining", "Remaining"),
                CurrencyColumn("Ending", "Ending"),
                CurrencyColumn("Recommended", "Recommended"),
                TextColumn("Frequency", "Frequency", DataGridViewAutoSizeColumnMode.AllCells, 80),
                TextColumn("Status", "Status", DataGridViewAutoSizeColumnMode.Fill, 120));
            _budgetGrid.CellFormatting += OnBudgetCellFormatting;
            _budgetGrid.CellDoubleClick += (_, _) => OpenSelectedPeriod();

            panel.Controls.Add(_budgetGrid);
            panel.Controls.Add(toolbar);
            return panel;
        }

        private Control BuildSidePanel()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 8
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

        private void OpenSelectedTransfer()
        {
            if (SelectedBudget() is not FinanceDashboardBudgetRow row)
                return;
            using var dialog = new BudgetBalanceTransferDialog(_budgets, row.RuleId);
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshDashboard();
        }

        private void OpenSelectedHistory()
        {
            if (SelectedBudget() is not FinanceDashboardBudgetRow row)
                return;
            using var viewer = new BudgetHistoryViewer(_budgets, row.RuleId);
            viewer.ShowDialog(FindForm());
        }

        private void OpenCategories()
        {
            using var manager = new CategoryManager(_categories);
            manager.ShowDialog(FindForm());
            RefreshDashboard();
        }

        private void OpenLedger()
        {
            using var ledger = new TransactionLedgerForm();
            ledger.ShowDialog(FindForm());
            RefreshDashboard();
        }

        private void OnBudgetCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_budgetGrid.Columns[e.ColumnIndex].DataPropertyName != "Remaining")
                return;
            if (e.Value is decimal remaining && remaining < 0)
                e.CellStyle.ForeColor = Color.Firebrick;
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

        private static Control CreateCard(string caption, Label value)
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
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);
            value.Dock = DockStyle.Fill;
            value.TextAlign = ContentAlignment.TopLeft;
            cell.Controls.Add(value, 0, 1);
            return cell;
        }

        private static Label NewValueLabel() => new()
        {
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Text = "$0.00"
        };

        private static DataGridView CreateGrid()
        {
            return new DataGridView
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
