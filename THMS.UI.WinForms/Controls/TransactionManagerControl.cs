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
        private BindingSource _categorySource = new BindingSource();
        private BindingSource _budgetsSource = new BindingSource();
        private DataGridView budgetGrid = null!;
        private DataGridView categoryGrid = null!;
        private DataGridViewTextBoxColumn categoryViewCategoryColumn = null!;
        private ThmsButton btnCategorySplit = null!;
        private Label? lblLoadStatus;
        private ProgressBar? progressLoad;
        private TabControl? _detailTabs;
        private TabPage? _categoryPage;
        private TabPage? _budgetsPage;
        private CancellationTokenSource? _txLoadCts;
        private bool _filterApplied;
        private bool _ready;
        private bool _suspendAccountChange;
        private bool _suspendHistoryChange;
        private bool _suspendCategoryFilter;
        private Label lblCategoryFilter = null!;
        private ComboBox cmbCategoryFilter = null!;
        private decimal _postedBalanceBeforeEdit;
        private bool _hostProvidesHistory;
        private bool _hostProvidesAccounts;

        public TransactionManagerControl()
        {
            InitializeComponent();
            InitializeHistory();
            InitializeForecastPeriod();
            InitializeShowFilter();
            InitializeCategoryFilter();
            LayoutForecastToolbar();
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

        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HostProvidesAccounts
        {
            get => _hostProvidesAccounts;
            set
            {
                _hostProvidesAccounts = value;
                splitContainer.Panel1Collapsed = value;
            }
        }

        public void SelectAccount(Guid? accountId)
        {
            LoadAccounts();
            _suspendAccountChange = true;
            try
            {
                if (accountId is Guid id)
                {
                    for (var i = 0; i < _accountsSource.Count; i++)
                    {
                        if (_accountsSource[i] is UnifiedAccountView view && view.Id == id)
                        {
                            _accountsSource.Position = i;
                            break;
                        }
                    }
                }
            }
            finally
            {
                _suspendAccountChange = false;
            }

            RefreshCurrentAccount();
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
            cmbCategoryFilter.Width = 220;
            cmbCategoryFilter.DropDownWidth = 280;
            cmbCategoryFilter.IntegralHeight = false;
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

        private void InitializeCategoryFilter()
        {
            lblCategoryFilter = new Label
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Margin = new Padding(12, 8, 8, 4),
                Text = "Category:"
            };
            cmbCategoryFilter = new ComboBox
            {
                Anchor = AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(4, 4, 8, 4)
            };
            cmbCategoryFilter.SelectedIndexChanged += OnCategoryFilterChanged;
            BindCategoryFilter();
        }

        private void BindCategoryFilter()
        {
            var selected = cmbCategoryFilter.SelectedItem as CategoryFilterChoice;
            var items = CategoryFilterChoice.ForCategories(_categoryOrchestrator.GetActiveCategories());
            _suspendCategoryFilter = true;
            cmbCategoryFilter.DisplayMember = nameof(CategoryFilterChoice.Name);
            cmbCategoryFilter.DataSource = items;
            if (selected is not null)
            {
                var match = items.FirstOrDefault(i =>
                    i.UncategorizedOnly == selected.UncategorizedOnly && i.CategoryId == selected.CategoryId);
                if (match is not null)
                    cmbCategoryFilter.SelectedItem = match;
            }

            _suspendCategoryFilter = false;
        }

        private void OnCategoryFilterChanged(object? sender, EventArgs e)
        {
            if (_suspendCategoryFilter || !_ready)
                return;

            LoadCategoryRowsForSelectedAccount();
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
            detailGrid.SelectionChanged += (_, _) => UpdateRuleActionButtons();
        }

        private void HostBudgetUi()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            var tabDetails = new ThmsTabControl { Dock = DockStyle.Fill, Name = "tabDetails" };
            var transactionsPage = new TabPage("Transactions");
            var categoryPage = new TabPage("By Category");
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

            HostCategoryUi(categoryPage);

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
            btnAddBudget.Click += (_, _) => OpenBudgetRuleEditor(existing: false);
            btnEditBudget.Click += (_, _) => OpenBudgetRuleEditor(existing: true);
            btnDeleteBudget.Click += (_, _) => DeleteSelectedBudget();
            btnViewHistory.Click += (_, _) => OpenBudgetHistory();
            btnOpenPeriod.Click += (_, _) => OpenBudgetPeriod();
            btnTransferBalance.Click += (_, _) => OpenBudgetTransfer();
            toolbar.Controls.Add(btnAddBudget);
            toolbar.Controls.Add(btnEditBudget);
            toolbar.Controls.Add(btnDeleteBudget);
            toolbar.Controls.Add(btnViewHistory);
            toolbar.Controls.Add(btnOpenPeriod);
            toolbar.Controls.Add(btnTransferBalance);

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
            tabDetails.TabPages.Add(categoryPage);
            tabDetails.TabPages.Add(budgetsPage);
            tabDetails.SelectedIndexChanged += (_, _) => RefreshCurrentAccount();
            _detailTabs = tabDetails;
            _categoryPage = categoryPage;
            _budgetsPage = budgetsPage;
            splitContainer.Panel2.Controls.Add(tabDetails);
        }

        private void HostCategoryUi(TabPage categoryPage)
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 6, 8, 6),
                WrapContents = true
            };
            lblCategoryFilter.Margin = new Padding(4, 8, 8, 4);
            cmbCategoryFilter.Margin = new Padding(4, 4, 8, 4);
            toolbar.Controls.Add(lblCategoryFilter);
            toolbar.Controls.Add(cmbCategoryFilter);
            btnCategorySplit = new ThmsButton { Text = "Split Transaction" };
            btnCategorySplit.Click += OnCategorySplitTransactionClicked;
            toolbar.Controls.Add(btnCategorySplit);

            categoryViewCategoryColumn = TextColumn("Category", "Category");
            var dateColumn = TextColumn("Date", "Date");
            dateColumn.DefaultCellStyle.Format = "d";
            var descriptionColumn = TextColumn("Description", "Description");
            descriptionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            categoryGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Dock = DockStyle.Fill,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            DataGridViewUtil.EnableDoubleBuffering(categoryGrid);
            categoryGrid.Columns.AddRange(
                dateColumn,
                CurrencyColumn("Amount", "Amount"),
                categoryViewCategoryColumn,
                TextColumn("TypeLabel", "Type"),
                descriptionColumn);
            categoryGrid.DataSource = _categorySource;
            categoryGrid.CellDoubleClick += OnCategoryViewCellDoubleClick;
            categoryGrid.CellMouseClick += OnCategoryViewCellMouseClick;
            categoryGrid.KeyDown += OnCategoryViewKeyDown;
            categoryGrid.SelectionChanged += (_, _) => UpdateCategorySplitButton();

            categoryPage.Controls.Add(categoryGrid);
            categoryPage.Controls.Add(toolbar);
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
                var liveBalances = new Dictionary<Guid, PostedBalanceDisplay>();
                foreach (var account in accounts)
                {
                    var statements = _statements.GetForAccount(account.Id).ToList();
                    if (!PostedBalanceCalculator.TryResolveLatestStatement(account, statements, out _, out var anchor))
                        continue;

                    var activity = _txOrchestrator.SumPostedAmountsAfter(account.Id, anchor.AsOf);
                    var latest = _txOrchestrator.GetLatestPostedActivityDate(account.Id);
                    if (PostedBalanceCalculator.TryCreateDisplay(account, statements, activity, latest, out var display))
                        liveBalances[account.Id] = display;
                }

                _accountsSource.DataSource = UnifiedAccountViewBuilder.Build(
                    accounts,
                    nextPayments,
                    livePostedBalances: liveBalances);
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
            cmbForecastPeriod.Enabled = SelectedShowMode() is ShowAll or ShowForecast;
            UpdateRuleActionButtons();
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
                UpdateRuleActionButtons();
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
                    _ruleOrchestrator.GetAllSingleRules(),
                    _ruleOrchestrator.GetTransferRules(accountId),
                    accountId);
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
                txs.FutureTransfers,
                accountId,
                txs.IncomingTransferSplitPosted,
                txs.IncomingTransferSplitFutures);

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

            if (IsCategoryColumn(detailGrid, CategoryColumn, e.ColumnIndex))
            {
                ShowCategoryMenu(detailGrid, CategoryColumn, e.RowIndex);
                return;
            }

            if (detailGrid.Rows[e.RowIndex].DataBoundItem is not UnifiedTransactionView view)
                return;

            if (TryGetRule(view, out _, out _))
            {
                OpenRuleEditor(view);
                return;
            }

            if (CanSplit(view))
                OpenSplitEditor(view);
        }

        private void OnCategoryCellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || !IsCategoryColumn(detailGrid, CategoryColumn, e.ColumnIndex))
                return;
            if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
                return;
            if (!CanEditCategory(detailGrid, e.RowIndex))
                return;

            detailGrid.CurrentCell = detailGrid[e.ColumnIndex, e.RowIndex];
            ShowCategoryMenu(detailGrid, CategoryColumn, e.RowIndex);
        }

        private void OnCategoryViewCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (IsCategoryColumn(categoryGrid, categoryViewCategoryColumn, e.ColumnIndex))
            {
                ShowCategoryMenu(categoryGrid, categoryViewCategoryColumn, e.RowIndex);
                return;
            }

            if (categoryGrid.Rows[e.RowIndex].DataBoundItem is UnifiedTransactionView view && CanSplit(view))
                OpenSplitEditor(view);
        }

        private void OnCategoryViewCellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || !IsCategoryColumn(categoryGrid, categoryViewCategoryColumn, e.ColumnIndex))
                return;
            if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
                return;
            if (!CanEditCategory(categoryGrid, e.RowIndex))
                return;

            categoryGrid.CurrentCell = categoryGrid[e.ColumnIndex, e.RowIndex];
            ShowCategoryMenu(categoryGrid, categoryViewCategoryColumn, e.RowIndex);
        }

        private void OnCategoryViewKeyDown(object? sender, KeyEventArgs e)
        {
            if (categoryGrid.CurrentCell is { RowIndex: >= 0 } cell
                && IsCategoryColumn(categoryGrid, categoryViewCategoryColumn, cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditCategory(categoryGrid, cell.RowIndex))
            {
                ShowCategoryMenu(categoryGrid, categoryViewCategoryColumn, cell.RowIndex);
                e.Handled = true;
            }
        }

        private void OnCategorySplitTransactionClicked(object? sender, EventArgs e)
        {
            if (GetSelectedCategoryRow() is not UnifiedTransactionView view)
            {
                MessageBox.Show(FindForm(), "Select a category row to split its posted transaction.", "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenSplitEditor(view);
        }

        private UnifiedTransactionView? GetSelectedCategoryRow() =>
            categoryGrid?.CurrentRow?.DataBoundItem as UnifiedTransactionView;

        private void UpdateCategorySplitButton()
        {
            if (btnCategorySplit is null)
                return;
            btnCategorySplit.Enabled = GetSelectedCategoryRow() is UnifiedTransactionView view && CanSplit(view);
        }

        private static bool IsCategoryColumn(DataGridView grid, DataGridViewColumn categoryColumn, int columnIndex) =>
            columnIndex >= 0 && grid.Columns[columnIndex] == categoryColumn;

        private static bool CanEditCategory(DataGridView grid, int rowIndex)
        {
            if (grid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view)
                return false;

            return view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;
        }

        private void ShowCategoryMenu(DataGridView grid, DataGridViewColumn categoryColumn, int rowIndex)
        {
            if (grid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view || !CanEditCategory(grid, rowIndex))
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

            var cell = grid.GetCellDisplayRectangle(categoryColumn.Index, rowIndex, cutOverflow: false);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(grid, new Point(cell.Left, cell.Bottom));
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
                && IsCategoryColumn(detailGrid, CategoryColumn, cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditCategory(detailGrid, cell.RowIndex))
            {
                ShowCategoryMenu(detailGrid, CategoryColumn, cell.RowIndex);
                e.Handled = true;
                return;
            }

            if (e.KeyCode != Keys.Delete || GetSelectedRuleView() is null)
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

            if (TryGetRule(view, out _, out _))
            {
                OpenRuleEditor(view);
                return;
            }

            OpenSplitEditor(view);
        }

        private static bool CanSplit(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType
                or UnifiedTransactionView.PostedTransferType
                or UnifiedTransactionView.FutureType
                or UnifiedTransactionView.FutureTransferType;

        private void OpenSplitEditor(UnifiedTransactionView view)
        {
            if (!CanSplit(view))
                return;

            var parentId = view.LookupId;
            var parent = _txOrchestrator.GetParent(parentId);
            if (parent is null)
                return;

            var amount = parent.Amount;
            var description = parent.Description ?? view.Description;
            var existing = parent.Splits.Select(s => s.Clone()).ToList();

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

        private void UpdateRuleActionButtons()
        {
            btnDeleteRule.Enabled = GetSelectedRuleView() is not null;
        }

        private UnifiedTransactionView? GetSelectedRuleView()
        {
            if (detailGrid.CurrentRow?.DataBoundItem is UnifiedTransactionView view
                && TryGetRule(view, out _, out _))
                return view;

            return null;
        }

        private bool TryGetRule(
            UnifiedTransactionView view,
            out RecurringSingleTransactionRule? single,
            out RecurringTransferRule? transfer)
        {
            single = _ruleOrchestrator.GetSingleRule(view.LookupId);
            transfer = single is null ? _ruleOrchestrator.GetTransferRule(view.LookupId) : null;
            return single is not null || transfer is not null;
        }

        private void OpenRuleEditor(UnifiedTransactionView view)
        {
            if (!TryGetRule(view, out var single, out var transfer))
                return;

            using var editor = single is not null
                ? new RecurringRuleEditor(view.AccountId, single)
                : new RecurringRuleEditor(view.AccountId, existingTransfer: transfer!);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private void DeleteSelectedRule()
        {
            if (GetSelectedRuleView() is not UnifiedTransactionView view
                || !TryGetRule(view, out var single, out var transfer))
                return;

            if (MessageBox.Show(
                    FindForm(),
                    $"Delete recurring rule '{view.Description}'?",
                    "Delete Rule",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (single is not null)
                _ruleOrchestrator.DeleteSingleRule(single.Id);
            else
                _ruleOrchestrator.DeleteTransferRule(transfer!.Id);

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
            BindCategoryFilter();
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
                else if (IsCategoryTabSelected())
                    LoadCategoryRowsForAccount(account.Id);
                else
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

        private bool IsCategoryTabSelected() =>
            _detailTabs is not null && _categoryPage is not null && _detailTabs.SelectedTab == _categoryPage;

        private void LoadCategoryRowsForSelectedAccount()
        {
            if (_accountsSource.Current is UnifiedAccountView account)
                LoadCategoryRowsForAccount(account.Id);
        }

        private void LoadCategoryRowsForAccount(Guid accountId)
        {
            if (categoryGrid is null)
                return;
            var history = SelectedHistory();
            var start = BaseOrchestrator.GetStartDate(DateTime.Today, history);
            var txs = history == HistoryLifetime
                ? _txOrchestrator.GetTransactionsForAccount(accountId)
                : _txOrchestrator.GetTransactionsForAccount(accountId, start, DateTime.Today);
            var rows = UnifiedTransactionViewBuilder.BuildCategoryRows(txs.Posted, txs.PostedTransfers);
            ApplyCategoryDisplayNames(rows);
            var filtered = UnifiedTransactionViewBuilder.FilterCategoryRows(
                rows,
                cmbCategoryFilter.SelectedItem as CategoryFilterChoice ?? CategoryFilterChoice.All,
                _categoryOrchestrator.GetAllCategories(includeInactive: true));
            _categorySource.DataSource = UnifiedTransactionView.OrderForDisplay(filtered).ToList();
            UpdateCategorySplitButton();
        }

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
