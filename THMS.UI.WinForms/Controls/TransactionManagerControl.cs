using System.ComponentModel;
using System.Diagnostics;

using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionManagerControl : UserControl, IDataManagerControl
    {
        private const string ShowAll = "All";
        private const string ShowPosted = "Posted";
        private const string ShowForecast = "Forecast";
        private const string ShowRecurringRules = "Recurring Rules";
        private const string HistoryMonth = "Month";
        private const string HistoryYear = "Year";
        private const string HistoryLifetime = "Lifetime";

        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly TransactionOrchestrator _txOrchestrator = new();
        private readonly RecurringRuleOrchestrator _ruleOrchestrator = new();
        private readonly BudgetOrchestrator _budgetOrchestrator = new();
        private readonly CategoryOrchestrator _categoryOrchestrator = new();
        private readonly IAccountStatementDataStore _statements = new DataStoreFactory().GetAccountStatementStore();

        private BindingSource _accountsSource = new BindingSource();
        private BindingSource _transactionsSource = new BindingSource();
        private BindingSource _budgetsSource = new BindingSource();
        private DataGridView budgetGrid = null!;
        private Label? lblLoadStatus;
        private ProgressBar? progressLoad;
        private TabControl? _detailTabs;
        private TabPage? _budgetsPage;
        private CancellationTokenSource? _txLoadCts;
        private bool _filterApplied;
        private bool _ready;
        private bool _suspendAccountChange;
        private bool _suspendHistoryChange;
        private decimal _postedBalanceBeforeEdit;
        private bool _hostProvidesHistory;

        public TransactionManagerControl()
        {
            InitializeComponent();
            LayoutForecastToolbar();
            InitializeHistory();
            InitializeForecastPeriod();
            InitializeShowFilter();
            InitializeGrids();
            HostBudgetUi();
            _ready = true;
        }

        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HostProvidesHistory
        {
            get => _hostProvidesHistory;
            set
            {
                _hostProvidesHistory = value;
                lblHistory.Visible = !value;
                cmbHistory.Visible = !value;
            }
        }

        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            SelectHistory(period);
            RefreshAll();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hostProvidesHistory)
                LoadAccounts();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (_hostProvidesHistory)
                return;
            if (_ready && Visible && IsHandleCreated && Parent != null && !Disposing)
                RefreshAll();
        }

        private void LayoutForecastToolbar()
        {
            cmbHistory.Width = 120;
            cmbHistory.DropDownWidth = 120;
            cmbHistory.IntegralHeight = false;
            cmbForecastPeriod.Width = 140;
            cmbForecastPeriod.DropDownWidth = 140;
            cmbForecastPeriod.IntegralHeight = false;
            cmbShow.Width = 200;
            cmbShow.DropDownWidth = 220;
            cmbShow.IntegralHeight = false;
            btnAddRule.AutoSize = true;
            btnDeleteRule.AutoSize = true;
            btnSplitTransaction.AutoSize = true;
        }

        private void InitializeHistory()
        {
            cmbHistory.Items.Clear();
            cmbHistory.Items.AddRange([HistoryMonth, HistoryYear, HistoryLifetime]);
            cmbHistory.SelectedItem = HistoryMonth;
            cmbHistory.SelectedIndexChanged += OnHistoryChanged;
        }

        private void InitializeForecastPeriod()
        {
            cmbForecastPeriod.Items.Clear();
            cmbForecastPeriod.Items.AddRange(["None", "30 days", "60 days", "90 days", "6 months", "1 year"]);
            cmbForecastPeriod.SelectedIndex = 0;
            cmbForecastPeriod.SelectedIndexChanged += OnForecastPeriodChanged;
        }

        private void InitializeShowFilter()
        {
            cmbShow.Items.Clear();
            cmbShow.Items.AddRange([ShowAll, ShowPosted, ShowForecast, ShowRecurringRules]);
            cmbShow.SelectedIndex = 0;
            cmbShow.SelectedIndexChanged += OnShowFilterChanged;
            btnAddRule.Click += OnAddRuleClicked;
            btnDeleteRule.Click += OnDeleteRuleClicked;
            btnSplitTransaction.Click += OnSplitTransactionClicked;
        }

        private void InitializeGrids()
        {
            DataGridViewUtil.EnableDoubleBuffering(masterGrid);
            DataGridViewUtil.EnableDoubleBuffering(detailGrid);
            masterGrid.AutoGenerateColumns = false;
            detailGrid.AutoGenerateColumns = false;
            detailGrid.ReadOnly = true;
            detailGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
            detailGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            CategoryColumn.ReadOnly = true;

            detailGrid.DataSource = _transactionsSource;
            masterGrid.DataSource = _accountsSource;
            BalanceColumn.DefaultCellStyle.NullValue = "N/A";
            AvailableColumn.DefaultCellStyle.NullValue = "N/A";

            _accountsSource.CurrentChanged += OnCurrentAccountChanged;
            _transactionsSource.ListChanged += OnTransactionsListChanged;

            masterGrid.CellBeginEdit += OnAccountCellBeginEdit;
            masterGrid.CellValidating += OnAccountCellValidating;
            masterGrid.CellParsing += OnAccountCellParsing;
            masterGrid.CellEndEdit += OnAccountPostedBalanceEdited;
            masterGrid.DataError += OnAccountGridDataError;
            masterGrid.CellDoubleClick += OnAccountCellDoubleClick;
            masterGrid.CellContentClick += OnAccountWebsiteClicked;
            masterGrid.CellFormatting += OnAccountGridCellFormatting;

            detailGrid.CellDoubleClick += OnTransactionCellDoubleClick;
            detailGrid.KeyDown += OnTransactionGridKeyDown;
            detailGrid.CellMouseClick += OnCategoryCellMouseClick;
        }

        private void HostBudgetUi()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            var tabDetails = new ThmsTabControl { Dock = DockStyle.Fill, Name = "tabDetails" };
            var transactionsPage = new TabPage("Transactions");
            var budgetsPage = new TabPage("Budgets");

            splitContainer.Panel2.Controls.Remove(detailGrid);
            splitContainer.Panel2.Controls.Remove(forecastPanel);

            var forecastBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(8, 6, 8, 6),
                Name = "forecastBar"
            };
            var forecastControls = forecastPanel.Controls.Cast<Control>().ToArray();
            forecastPanel.Controls.Clear();
            foreach (var child in forecastControls)
            {
                child.Margin = new Padding(4, 4, 8, 4);
                forecastBar.Controls.Add(child);
            }

            lblLoadStatus = new Label
            {
                AutoSize = true,
                Margin = new Padding(12, 8, 8, 4),
                Visible = false
            };
            progressLoad = new ProgressBar
            {
                Width = 220,
                Height = 22,
                Margin = new Padding(4, 8, 8, 4),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false
            };
            forecastBar.Controls.Add(lblLoadStatus);
            forecastBar.Controls.Add(progressLoad);

            transactionsPage.Controls.Add(detailGrid);
            transactionsPage.Controls.Add(forecastBar);

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 6, 8, 6),
                WrapContents = true
            };
            var btnAddBudget = new ThmsButton { Text = "Add Budget" };
            var btnEditBudget = new ThmsButton { Text = "Edit Budget" };
            var btnDeleteBudget = new ThmsButton { Text = "Delete Budget", Destructive = true };
            var btnViewHistory = new ThmsButton { Text = "View History" };
            var btnOpenPeriod = new ThmsButton { Text = "Open Current Period" };
            var btnTransferBalance = new ThmsButton { Text = "Transfer Balance" };
            var btnManageCategories = new ThmsButton { Text = "Manage Categories" };
            btnAddBudget.Click += (_, _) => OpenBudgetRuleEditor(existing: false);
            btnEditBudget.Click += (_, _) => OpenBudgetRuleEditor(existing: true);
            btnDeleteBudget.Click += (_, _) => DeleteSelectedBudget();
            btnViewHistory.Click += (_, _) => OpenBudgetHistory();
            btnOpenPeriod.Click += (_, _) => OpenBudgetPeriod();
            btnTransferBalance.Click += (_, _) => OpenBudgetTransfer();
            btnManageCategories.Click += (_, _) => OpenCategoryManager();
            toolbar.Controls.Add(btnAddBudget);
            toolbar.Controls.Add(btnEditBudget);
            toolbar.Controls.Add(btnDeleteBudget);
            toolbar.Controls.Add(btnViewHistory);
            toolbar.Controls.Add(btnOpenPeriod);
            toolbar.Controls.Add(btnTransferBalance);
            toolbar.Controls.Add(btnManageCategories);

            budgetGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            DataGridViewUtil.EnableDoubleBuffering(budgetGrid);
            budgetGrid.Columns.AddRange(
                TextColumn("BudgetName", "Budget"),
                TextColumn("Frequency", "Frequency"),
                CurrencyColumn("StartingBalance", "Starting"),
                CurrencyColumn("PeriodBudget", "Budget"),
                CurrencyColumn("Actual", "Actual"),
                CurrencyColumn("Remaining", "Remaining"),
                CurrencyColumn("EndingBalance", "Ending"));
            budgetGrid.DataSource = _budgetsSource;
            budgetGrid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    OpenBudgetRuleEditor(existing: true);
            };

            budgetsPage.Controls.Add(budgetGrid);
            budgetsPage.Controls.Add(toolbar);
            tabDetails.TabPages.Add(transactionsPage);
            tabDetails.TabPages.Add(budgetsPage);
            tabDetails.SelectedIndexChanged += (_, _) =>
            {
                if (tabDetails.SelectedTab == budgetsPage)
                    LoadBudgetsForSelectedAccount();
            };
            _detailTabs = tabDetails;
            _budgetsPage = budgetsPage;
            splitContainer.Panel2.Controls.Add(tabDetails);
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new() { DataPropertyName = property, HeaderText = header, Name = property };

        private static DataGridViewTextBoxColumn CurrencyColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "c2";
            return column;
        }

        private void LoadAccounts()
        {
            _suspendAccountChange = true;
            try
            {
                var accounts = _accountOrchestrator.GetAllAccounts().ToList();
                var nextPayments = accounts
                    .Where(a => a is LoanAccount or MortgageAccount)
                    .ToDictionary(a => a.Id, a => _ruleOrchestrator.GetNextPaymentDate(a.Id));
                var usableBalances = PostedBalanceCalculator.UsablePostedBalanceAccountIds(
                    accounts,
                    id => _statements.GetForAccount(id));

                _accountsSource.DataSource = UnifiedAccountViewBuilder.Build(accounts, nextPayments, usableBalances);
            }
            finally
            {
                _suspendAccountChange = false;
            }
        }

        private void OnCurrentAccountChanged(object? sender, EventArgs e)
        {
            if (_suspendAccountChange || !_ready)
                return;

            RefreshCurrentAccount();
        }

        private void OnHistoryChanged(object? sender, EventArgs e)
        {
            if (_suspendHistoryChange || !_ready)
                return;

            RefreshCurrentAccount();
        }

        private void OnForecastPeriodChanged(object? sender, EventArgs e)
        {
            if (!_ready)
                return;

            RefreshCurrentAccount();
        }

        private void OnShowFilterChanged(object? sender, EventArgs e)
        {
            if (!_ready)
                return;

            _filterApplied = SelectedShowMode() == ShowPosted;
            var showingRules = SelectedShowMode() == ShowRecurringRules;
            cmbForecastPeriod.Enabled = SelectedShowMode() is ShowAll or ShowForecast;
            btnDeleteRule.Enabled = showingRules;
            RefreshCurrentAccount();
        }

        private string SelectedShowMode() =>
            cmbShow.SelectedItem?.ToString() ?? ShowAll;

        private string SelectedHistory() =>
            cmbHistory.SelectedItem?.ToString() ?? HistoryMonth;

        private void SelectHistory(string period)
        {
            var item = period is HistoryYear or HistoryLifetime ? period : HistoryMonth;
            if (Equals(cmbHistory.SelectedItem, item))
                return;

            _suspendHistoryChange = true;
            cmbHistory.SelectedItem = item;
            _suspendHistoryChange = false;
        }

        private async Task LoadTransactionsForAccountAsync(Guid accountId, CancellationToken token)
        {
            var show = SelectedShowMode();
            var history = SelectedHistory();
            var forecastEnd = GetForecastEnd();
            ShowLoadProgress($"Loading {history.ToLowerInvariant()} history...");

            try
            {
                var views = await Task.Run(
                    () => BuildTransactionViews(accountId, show, history, forecastEnd, token),
                    token);
                if (token.IsCancellationRequested || CurrentAccountId != accountId)
                    return;

                ShowLoadProgress("Updating grid...");
                _transactionsSource.DataSource = views;
            }
            catch (OperationCanceledException)
            {
            }
        }

        private List<UnifiedTransactionView> BuildTransactionViews(
            Guid accountId,
            string show,
            string history,
            DateTime forecastEnd,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (show == ShowRecurringRules)
            {
                var rules = UnifiedTransactionViewBuilder.BuildRecurringRules(
                    _ruleOrchestrator.GetSingleRules(accountId),
                    _ruleOrchestrator.GetTransferRules(accountId));
                ApplyCategoryDisplayNames(rules);
                return rules;
            }

            var chronological = BuildUnifiedTransactions(accountId, show, history, forecastEnd);
            token.ThrowIfCancellationRequested();
            ApplyCategoryDisplayNames(chronological);

            if (show != ShowAll)
            {
                ClearForecastBalances(chronological);
                return UnifiedTransactionView.OrderForDisplay(chronological).ToList();
            }

            var historyStart = BaseOrchestrator.GetStartDate(DateTime.Today, history);
            var opening = GetStartingBalance(accountId)
                + _txOrchestrator.SumPostedAmountsBefore(accountId, historyStart);
            ApplyRunningBalances(chronological, opening, accountId);
            return UnifiedTransactionView.OrderForDisplay(chronological).ToList();
        }

        private List<UnifiedTransactionView> BuildUnifiedTransactions(
            Guid accountId,
            string show,
            string history,
            DateTime forecastEnd)
        {
            var start = BaseOrchestrator.GetStartDate(DateTime.Today, history);
            var txs = history == HistoryLifetime
                ? _txOrchestrator.GetTransactionsForAccount(accountId)
                : _txOrchestrator.GetTransactionsForAccount(accountId, start, DateTime.Today);
            var posted = UnifiedTransactionViewBuilder.Build(
                txs.Posted,
                txs.PostedTransfers,
                txs.FutureSingles,
                txs.FutureTransfers);

            if (show == ShowPosted)
                return posted;

            var forecast = _txOrchestrator.GenerateForecast(
                accountId,
                DateTime.Today,
                forecastEnd);

            if (show == ShowForecast)
                return forecast;

            return UnifiedTransactionView.OrderForRunningBalance(posted.Concat(forecast)).ToList();
        }

        private DateTime GetForecastEnd()
        {
            return cmbForecastPeriod.SelectedItem?.ToString() switch
            {
                "None" => DateTime.Today,
                "30 days" => DateTime.Today.AddDays(30),
                "60 days" => DateTime.Today.AddDays(60),
                "90 days" => DateTime.Today.AddDays(90),
                "6 months" => DateTime.Today.AddMonths(6),
                "1 year" => DateTime.Today.AddYears(1),
                _ => DateTime.Today.AddDays(90)
            };
        }

        private void ApplyRunningBalances(
            IEnumerable<UnifiedTransactionView> chronological,
            decimal startingBalance,
            Guid accountId)
        {
            var credit = _accountOrchestrator.GetAccount(accountId) is CreditAccount;
            decimal balance = startingBalance;
            foreach (var tx in chronological)
            {
                balance += tx.Amount;
                tx.ForecastBalance = credit ? -balance : balance;
            }
        }

        private decimal GetStartingBalance(Guid accountId)
        {
            return PostedBalanceCalculator.GetStartingBalance(_accountOrchestrator.GetAccount(accountId));
        }

        private void OnAccountCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
            {
                e.Cancel = true;
                return;
            }

            _postedBalanceBeforeEdit = GetRowPostedBalance(e.RowIndex);
        }

        private void OnAccountCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (!IsPostedBalanceColumn(e.ColumnIndex) || e.RowIndex < 0)
                return;

            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
                return;

            if (!TryParsePostedBalance(e.FormattedValue, out _))
            {
                e.Cancel = true;
                masterGrid.CancelEdit();
            }
        }

        private void OnAccountCellParsing(object? sender, DataGridViewCellParsingEventArgs e)
        {
            if (!IsPostedBalanceColumn(e.ColumnIndex))
                return;

            if (TryParsePostedBalance(e.Value, out var parsed))
            {
                e.Value = parsed;
                e.ParsingApplied = true;
            }
        }

        private void OnAccountPostedBalanceEdited(object? sender, DataGridViewCellEventArgs e)
        {
            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
                return;

            if (GetAccountView(e.RowIndex) is not UnifiedAccountView view)
                return;

            var entered = view.Balance ?? 0;
            var displayDelta = entered - _postedBalanceBeforeEdit;
            if (displayDelta == 0)
                return;

            try
            {
                var account = _accountOrchestrator.GetAccount(view.Id);
                var ledgerDelta = PostedBalanceCalculator.ToLedgerPostedDelta(account, displayDelta);
                _accountOrchestrator.AdjustStartingBalanceForPostedDelta(view.Id, ledgerDelta);
                view.Balance = entered;
                RefreshCurrentAccount();
            }
            catch
            {
                view.Balance = _postedBalanceBeforeEdit;
                masterGrid.Refresh();
            }
        }

        private void OnAccountGridDataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;
        }

        private void OnAccountCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
                return;

            masterGrid.CurrentCell = masterGrid[e.ColumnIndex, e.RowIndex];
            masterGrid.BeginEdit(true);
        }

        private void OnAccountWebsiteClicked(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (masterGrid.Columns[e.ColumnIndex] != WebsiteColumn)
                return;

            var url = GetAccountView(e.RowIndex)?.WebsiteUrl;
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (!Uri.TryCreate("https://" + url.Trim(), UriKind.Absolute, out uri))
                    return;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }

        private bool IsPostedBalanceColumn(int columnIndex) =>
            columnIndex >= 0 && masterGrid.Columns[columnIndex] == BalanceColumn;

        private void OnAccountGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var column = masterGrid.Columns[e.ColumnIndex];
            if (column != BalanceColumn && column != AvailableColumn)
                return;

            if (e.Value is not null and not DBNull)
                return;

            e.Value = "N/A";
            e.FormattingApplied = true;
        }

        private bool IsEditablePostedBalanceCell(int rowIndex, int columnIndex)
        {
            if (!IsPostedBalanceColumn(columnIndex) || rowIndex < 0)
                return false;

            return GetAccountView(rowIndex)?.AccountType is "Bank" or "Credit"
                && GetAccountView(rowIndex)?.Balance is not null;
        }

        private UnifiedAccountView? GetAccountView(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= masterGrid.Rows.Count)
                return null;

            return masterGrid.Rows[rowIndex].DataBoundItem as UnifiedAccountView;
        }

        private decimal GetRowPostedBalance(int rowIndex) =>
            GetAccountView(rowIndex)?.Balance ?? 0;

        private static bool TryParsePostedBalance(object? value, out decimal parsed)
        {
            parsed = 0;
            if (value is decimal numeric)
            {
                parsed = numeric;
                return true;
            }

            var text = Convert.ToString(value)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.CurrentCulture,
                out parsed);
        }

        private void ClearForecastBalances(IEnumerable<UnifiedTransactionView> list)
        {
            foreach (var item in list)
                item.ForecastBalance = null;
        }

        private void OnTransactionsListChanged(object? sender, ListChangedEventArgs e)
        {
            if (_accountsSource.Current is not UnifiedAccountView account)
                return;

            var unified = _transactionsSource.List.Cast<UnifiedTransactionView>().ToList();
            if (SelectedShowMode() != ShowAll || _filterApplied || !string.IsNullOrEmpty(_transactionsSource.Filter))
            {
                ClearForecastBalances(unified);
                detailGrid.Refresh();
                return;
            }

            var chronological = UnifiedTransactionView.OrderForRunningBalance(unified).ToList();
            ApplyRunningBalances(chronological, GetStartingBalance(account.Id), account.Id);
            detailGrid.Refresh();
        }

        private void OnTransactionCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (IsCategoryColumn(e.ColumnIndex))
            {
                ShowCategoryMenu(e.RowIndex);
                return;
            }

            if (detailGrid.Rows[e.RowIndex].DataBoundItem is not UnifiedTransactionView view)
                return;

            if (view.IsRecurringRule)
            {
                OpenRuleEditor(view);
                return;
            }

            if (CanSplit(view))
                OpenSplitEditor(view);
        }

        private void OnCategoryCellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || !IsCategoryColumn(e.ColumnIndex))
                return;
            if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
                return;
            if (!CanEditCategory(e.RowIndex))
                return;

            detailGrid.CurrentCell = detailGrid[e.ColumnIndex, e.RowIndex];
            ShowCategoryMenu(e.RowIndex);
        }

        private bool IsCategoryColumn(int columnIndex) =>
            columnIndex >= 0 && detailGrid.Columns[columnIndex] == CategoryColumn;

        private bool CanEditCategory(int rowIndex)
        {
            if (detailGrid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view)
                return false;

            return view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;
        }

        private void ShowCategoryMenu(int rowIndex)
        {
            if (detailGrid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view || !CanEditCategory(rowIndex))
                return;

            var posted = view.Type == UnifiedTransactionView.PostedType
                ? _txOrchestrator.GetTransactionsForAccount(view.AccountId).Posted.FirstOrDefault(t => t.Id == view.LookupId)
                : null;
            var suggestion = posted is null ? null : _categoryOrchestrator.Suggest(posted);

            var menu = new ContextMenuStrip();
            var categories = _categoryOrchestrator.GetActiveCategories();
            var currentId = view.CategoryId ?? suggestion?.CategoryId;

            foreach (var root in THMS.Logic.Finance.Categories.ExpenseCategoryTree.Roots(categories))
                menu.Items.Add(CategoryTreeUi.CreateMenuItem(
                    categories,
                    root,
                    currentId,
                    suggestion?.CategoryId,
                    category => AssignCategory(view, category.Id)));

            menu.Items.Add(new ToolStripSeparator());
            var newItem = new ToolStripMenuItem("New Category…");
            newItem.Click += (_, _) => CreateAndAssignCategory(view);
            menu.Items.Add(newItem);
            var manageItem = new ToolStripMenuItem("Manage Categories…");
            manageItem.Click += (_, _) => OpenCategoryManager();
            menu.Items.Add(manageItem);

            var cell = detailGrid.GetCellDisplayRectangle(CategoryColumn.Index, rowIndex, cutOverflow: false);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(detailGrid, new Point(cell.Left, cell.Bottom));
        }

        private void AssignCategory(UnifiedTransactionView view, Guid categoryId)
        {
            _categoryOrchestrator.AssignToPosted(view.LookupId, categoryId, splitRowId: view.SplitRowId);
            RefreshAll();
        }

        private void CreateAndAssignCategory(UnifiedTransactionView view)
        {
            using var editor = new CategoryEditor(_categoryOrchestrator);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK || editor.CreatedCategory is null)
                return;

            AssignCategory(view, editor.CreatedCategory.Id);
        }

        private void OpenCategoryManager()
        {
            using var manager = new CategoryManager(_categoryOrchestrator);
            if (manager.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private void ApplyCategoryDisplayNames(IEnumerable<UnifiedTransactionView> views)
        {
            var names = _categoryOrchestrator.GetAllCategories(includeInactive: true)
                .ToDictionary(c => c.Id, c => c.Name);
            foreach (var view in views)
            {
                if (view.CategoryId is Guid id && names.TryGetValue(id, out var name))
                    view.Category = name;
            }
        }

        private void OnTransactionGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (detailGrid.CurrentCell is { RowIndex: >= 0 } cell
                && IsCategoryColumn(cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditCategory(cell.RowIndex))
            {
                ShowCategoryMenu(cell.RowIndex);
                e.Handled = true;
                return;
            }

            if (e.KeyCode != Keys.Delete || SelectedShowMode() != ShowRecurringRules)
                return;

            DeleteSelectedRule();
            e.Handled = true;
        }

        private void OnAddRuleClicked(object? sender, EventArgs e)
        {
            var accountId = (_accountsSource.Current as UnifiedAccountView)?.Id;
            using var editor = new RecurringRuleEditor(accountId);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private void OnSplitTransactionClicked(object? sender, EventArgs e)
        {
            if (detailGrid.CurrentRow?.DataBoundItem is not UnifiedTransactionView view)
            {
                MessageBox.Show(FindForm(), "Select a transaction or rule to split.", "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenSplitEditor(view);
        }

        private static bool CanSplit(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType
                or UnifiedTransactionView.PostedTransferType
                or UnifiedTransactionView.FutureType
                or UnifiedTransactionView.FutureTransferType
                or UnifiedTransactionView.RecurringRuleType
                or UnifiedTransactionView.RecurringTransferRuleType
                or UnifiedTransactionView.ForecastType
                or UnifiedTransactionView.ForecastTransferType;

        private void OpenSplitEditor(UnifiedTransactionView view)
        {
            if (!CanSplit(view))
                return;

            var parentId = view.LookupId;
            decimal amount;
            string description;
            List<SplitTransactionRow> existing;

            if (view.IsRecurringRule || view.IsForecasted)
            {
                var single = _ruleOrchestrator.GetSingleRule(parentId);
                var transfer = single is null ? _ruleOrchestrator.GetTransferRule(parentId) : null;
                if (single is null && transfer is null)
                {
                    MessageBox.Show(FindForm(), "Splits on forecast rows are edited on the recurring rule.",
                        "Split Transaction", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                amount = single?.Amount ?? transfer!.Amount;
                description = single?.Description ?? transfer!.Description ?? view.Description;
                existing = _txOrchestrator.GetSplits(parentId);
            }
            else
            {
                var txs = _txOrchestrator.GetTransactionsForAccount(view.AccountId);
                BaseTransaction? parent =
                    (BaseTransaction?)txs.Posted.FirstOrDefault(t => t.Id == parentId) ??
                    txs.PostedTransfers.FirstOrDefault(t => t.Id == parentId) ??
                    (BaseTransaction?)txs.FutureSingles.FirstOrDefault(t => t.Id == parentId) ??
                    txs.FutureTransfers.FirstOrDefault(t => t.Id == parentId);
                if (parent is null)
                    return;

                amount = parent.Amount;
                description = parent.Description ?? view.Description;
                existing = parent.Splits.Select(s => s.Clone()).ToList();
            }

            using var editor = new SplitTransactionEditor(
                description,
                amount,
                existing,
                _categoryOrchestrator.GetActiveCategories(),
                _accountOrchestrator.GetAllAccounts().ToList());
            if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _txOrchestrator.ApplySplits(parentId, editor.Result);
                RefreshAll();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnDeleteRuleClicked(object? sender, EventArgs e)
        {
            DeleteSelectedRule();
        }

        private UnifiedTransactionView? GetSelectedRuleView()
        {
            if (detailGrid.CurrentRow?.DataBoundItem is UnifiedTransactionView view && view.IsRecurringRule)
                return view;

            return null;
        }

        private void OpenRuleEditor(UnifiedTransactionView view)
        {
            if (view.Type == UnifiedTransactionView.RecurringRuleType)
            {
                var rule = _ruleOrchestrator.GetSingleRule(view.LookupId);
                if (rule is null)
                    return;

                using var editor = new RecurringRuleEditor(view.AccountId, rule);
                if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                    RefreshAll();
                return;
            }

            var transfer = _ruleOrchestrator.GetTransferRule(view.LookupId);
            if (transfer is null)
                return;

            using (var editor = new RecurringRuleEditor(view.AccountId, existingTransfer: transfer))
            {
                if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                    RefreshAll();
            }
        }

        private void DeleteSelectedRule()
        {
            if (GetSelectedRuleView() is not UnifiedTransactionView view)
                return;

            if (MessageBox.Show(
                    FindForm(),
                    $"Delete recurring rule '{view.Description}'?",
                    "Delete Rule",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (view.Type == UnifiedTransactionView.RecurringRuleType)
                _ruleOrchestrator.DeleteSingleRule(view.LookupId);
            else
                _ruleOrchestrator.DeleteTransferRule(view.LookupId);

            RefreshAll();
        }

        public void FilterByType(string type)
        {
            cmbShow.SelectedItem = ShowPosted;
            _filterApplied = !string.IsNullOrWhiteSpace(type);
            RefreshCurrentAccount();

            if (string.IsNullOrWhiteSpace(type))
            {
                _transactionsSource.RemoveFilter();
                return;
            }

            var filtered = _transactionsSource.List.Cast<UnifiedTransactionView>()
                .Where(t => !t.IsForecasted && t.Type == type)
                .ToList();
            ClearForecastBalances(filtered);
            _transactionsSource.DataSource = filtered;
            detailGrid.Refresh();
        }

        public void RefreshAll()
        {
            LoadAccounts();
            RefreshCurrentAccount();
        }

        public void RefreshCurrentAccount()
        {
            _ = RefreshCurrentAccountAsync();
        }

        private async Task RefreshCurrentAccountAsync()
        {
            if (_accountsSource.Current is not UnifiedAccountView account)
                return;

            _txLoadCts?.Cancel();
            var cts = new CancellationTokenSource();
            _txLoadCts = cts;

            try
            {
                if (IsBudgetsTabSelected())
                    LoadBudgetsForAccount(account.Id);

                await LoadTransactionsForAccountAsync(account.Id, cts.Token);
            }
            finally
            {
                if (ReferenceEquals(_txLoadCts, cts) && !cts.IsCancellationRequested)
                    HideLoadProgress();
            }
        }

        private bool IsBudgetsTabSelected() =>
            _detailTabs is not null && _budgetsPage is not null && _detailTabs.SelectedTab == _budgetsPage;

        private void LoadBudgetsForSelectedAccount()
        {
            if (_accountsSource.Current is UnifiedAccountView account)
                LoadBudgetsForAccount(account.Id);
        }

        private void ShowLoadProgress(string status)
        {
            if (lblLoadStatus is null || progressLoad is null)
                return;

            lblLoadStatus.Text = status;
            lblLoadStatus.Visible = true;
            progressLoad.Visible = true;
            lblLoadStatus.Update();
            progressLoad.Update();
        }

        private void HideLoadProgress()
        {
            if (lblLoadStatus is null || progressLoad is null)
                return;

            progressLoad.Visible = false;
            lblLoadStatus.Visible = false;
            lblLoadStatus.Text = "";
        }

        private void LoadBudgetsForAccount(Guid accountId)
        {
            _budgetOrchestrator.EnsureSuggestedRules(accountId);
            _budgetOrchestrator.RefreshAccount(accountId);

            var views = new List<UnifiedBudgetView>();
            foreach (var rule in _budgetOrchestrator.GetRules(accountId))
            {
                var period = _budgetOrchestrator.GetActivePeriod(rule.Id);
                views.Add(new UnifiedBudgetView
                {
                    Id = rule.Id,
                    BudgetName = rule.BudgetName,
                    Frequency = rule.BudgetFrequency.ToString(),
                    StartingBalance = period?.StartingBalance,
                    PeriodBudget = period?.BudgetAmount,
                    Actual = period?.ActualExpenses,
                    Remaining = period?.Remaining,
                    EndingBalance = period?.EndingBalance,
                    IsActive = rule.IsActive,
                    Status = !rule.IsActive ? "Inactive" : period is null ? "No period" : period.IsClosed ? "Closed" : "Open"
                });
            }

            _budgetsSource.DataSource = views;
        }

        private UnifiedBudgetView? GetSelectedBudget() =>
            budgetGrid.CurrentRow?.DataBoundItem as UnifiedBudgetView;

        private Guid? CurrentAccountId =>
            (_accountsSource.Current as UnifiedAccountView)?.Id;

        private void OpenBudgetRuleEditor(bool existing)
        {
            if (CurrentAccountId is not Guid accountId)
                return;

            ExpenseBudgetRule? rule = null;
            if (existing)
            {
                if (GetSelectedBudget() is not UnifiedBudgetView view)
                    return;
                rule = _budgetOrchestrator.GetRule(view.Id);
                if (rule is null)
                    return;
            }

            using var editor = new BudgetRuleEditor(accountId, rule);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private void DeleteSelectedBudget()
        {
            if (GetSelectedBudget() is not UnifiedBudgetView view)
                return;

            if (MessageBox.Show(FindForm(), $"Delete budget '{view.BudgetName}' and its history?", "Delete Budget",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _budgetOrchestrator.DeleteRule(view.Id);
            RefreshAll();
        }

        private void OpenBudgetHistory()
        {
            if (GetSelectedBudget() is not UnifiedBudgetView view)
                return;

            using var viewer = new BudgetHistoryViewer(view.Id);
            viewer.ShowDialog(FindForm());
        }

        private void OpenBudgetPeriod()
        {
            if (GetSelectedBudget() is not UnifiedBudgetView view)
                return;

            using var editor = new BudgetPeriodEditor(_budgetOrchestrator, view.Id);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private void OpenBudgetTransfer()
        {
            if (GetSelectedBudget() is not UnifiedBudgetView view)
                return;

            using var dialog = new BudgetBalanceTransferDialog(_budgetOrchestrator, view.Id);
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }
    }
}
