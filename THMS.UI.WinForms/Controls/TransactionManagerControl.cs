using System.ComponentModel;
using System.Diagnostics;

using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionManagerControl : UserControl
    {
        private const string ShowAll = "All";
        private const string ShowPosted = "Posted";
        private const string ShowForecast = "Forecast";
        private const string ShowRecurringRules = "Recurring Rules";

        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly TransactionOrchestrator _txOrchestrator = new();
        private readonly RecurringRuleOrchestrator _ruleOrchestrator = new();
        private readonly BudgetOrchestrator _budgetOrchestrator = new();
        private readonly CategoryOrchestrator _categoryOrchestrator = new();

        private BindingSource _accountsSource = new BindingSource();
        private BindingSource _transactionsSource = new BindingSource();
        private BindingSource _budgetsSource = new BindingSource();
        private DataGridView budgetGrid = null!;
        private bool _filterApplied;
        private decimal _postedBalanceBeforeEdit;

        public TransactionManagerControl()
        {
            InitializeComponent();
            InitializeForecastPeriod();
            InitializeShowFilter();
            InitializeGrids();
            HostBudgetUi();
            LoadAccounts();
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
        }

        private void InitializeGrids()
        {
            masterGrid.AutoGenerateColumns = false;
            detailGrid.AutoGenerateColumns = false;
            detailGrid.ReadOnly = true;
            detailGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
            detailGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            CategoryColumn.ReadOnly = true;

            detailGrid.DataSource = _transactionsSource;
            masterGrid.DataSource = _accountsSource;

            _accountsSource.CurrentChanged += OnCurrentAccountChanged;
            _transactionsSource.ListChanged += OnTransactionsListChanged;

            masterGrid.CellBeginEdit += OnAccountCellBeginEdit;
            masterGrid.CellValidating += OnAccountCellValidating;
            masterGrid.CellParsing += OnAccountCellParsing;
            masterGrid.CellEndEdit += OnAccountPostedBalanceEdited;
            masterGrid.DataError += OnAccountGridDataError;
            masterGrid.CellDoubleClick += OnAccountCellDoubleClick;
            masterGrid.CellContentClick += OnAccountWebsiteClicked;

            detailGrid.CellDoubleClick += OnTransactionCellDoubleClick;
            detailGrid.KeyDown += OnTransactionGridKeyDown;
            detailGrid.CellMouseClick += OnCategoryCellMouseClick;
        }

        private void HostBudgetUi()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;
            var tabDetails = new TabControl { Dock = DockStyle.Fill, Name = "tabDetails" };
            var transactionsPage = new TabPage("Transactions");
            var budgetsPage = new TabPage("Budgets");

            splitContainer.Panel2.Controls.Remove(detailGrid);
            splitContainer.Panel2.Controls.Remove(forecastPanel);
            transactionsPage.Controls.Add(detailGrid);
            transactionsPage.Controls.Add(forecastPanel);

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 6, 8, 6),
                WrapContents = true
            };
            var btnAddBudget = new Button { Text = "Add Budget", AutoSize = true };
            var btnEditBudget = new Button { Text = "Edit Budget", AutoSize = true };
            var btnDeleteBudget = new Button { Text = "Delete Budget", AutoSize = true };
            var btnViewHistory = new Button { Text = "View History", AutoSize = true };
            var btnOpenPeriod = new Button { Text = "Open Current Period", AutoSize = true };
            var btnTransferBalance = new Button { Text = "Transfer Balance", AutoSize = true };
            var btnManageCategories = new Button { Text = "Manage Categories", AutoSize = true };
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
            var accounts = _accountOrchestrator.GetAllAccounts().ToList();
            foreach (var account in accounts)
                ApplyPostedBalance(account);

            var nextPayments = accounts
                .Where(a => a is LoanAccount or MortgageAccount)
                .ToDictionary(a => a.Id, a => _ruleOrchestrator.GetNextPaymentDate(a.Id));

            _accountsSource.DataSource = UnifiedAccountViewBuilder.Build(accounts, nextPayments);
        }

        private void ApplyPostedBalance(Account account)
        {
            if (account is not BankAccount and not CreditAccount)
                return;

            var postedBalance = _txOrchestrator.ComputePostedBalance(
                account.Id,
                PostedBalanceCalculator.GetStartingBalance(account));
            PostedBalanceCalculator.ApplyPostedBalance(account, postedBalance);
        }

        private void OnCurrentAccountChanged(object? sender, EventArgs e)
        {
            RefreshCurrentAccount();
        }

        private void OnForecastPeriodChanged(object? sender, EventArgs e)
        {
            RefreshCurrentAccount();
        }

        private void OnShowFilterChanged(object? sender, EventArgs e)
        {
            _filterApplied = SelectedShowMode() == ShowPosted;
            var showingRules = SelectedShowMode() == ShowRecurringRules;
            cmbForecastPeriod.Enabled = SelectedShowMode() is ShowAll or ShowForecast;
            btnDeleteRule.Enabled = showingRules;
            RefreshCurrentAccount();
        }

        private string SelectedShowMode() =>
            cmbShow.SelectedItem?.ToString() ?? ShowAll;

        private void LoadTransactionsForAccount(Guid accountId)
        {
            var show = SelectedShowMode();
            if (show == ShowRecurringRules)
            {
                var rules = UnifiedTransactionViewBuilder.BuildRecurringRules(
                    _ruleOrchestrator.GetSingleRules(accountId),
                    _ruleOrchestrator.GetTransferRules(accountId));
                ApplyCategoryDisplayNames(rules);
                _transactionsSource.DataSource = rules;
                return;
            }

            var chronological = BuildUnifiedTransactions(accountId, show);
            ApplyCategoryDisplayNames(chronological);

            if (show != ShowAll)
            {
                ClearForecastBalances(chronological);
                _transactionsSource.DataSource = UnifiedTransactionView.OrderForDisplay(chronological).ToList();
                return;
            }

            ApplyRunningBalances(chronological, GetStartingBalance(accountId));
            _transactionsSource.DataSource = UnifiedTransactionView.OrderForDisplay(chronological).ToList();
        }

        private List<UnifiedTransactionView> BuildUnifiedTransactions(Guid accountId, string show)
        {
            var txs = _txOrchestrator.GetTransactionsForAccount(accountId);
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
                GetForecastEnd());

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

        private void ApplyRunningBalances(IEnumerable<UnifiedTransactionView> chronological, decimal startingBalance)
        {
            decimal balance = startingBalance;
            foreach (var tx in chronological)
            {
                balance += tx.Amount;
                tx.ForecastBalance = balance;
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
            var delta = entered - _postedBalanceBeforeEdit;
            if (delta == 0)
                return;

            try
            {
                _accountOrchestrator.AdjustStartingBalanceForPostedDelta(view.Id, delta);
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

        private bool IsEditablePostedBalanceCell(int rowIndex, int columnIndex)
        {
            if (!IsPostedBalanceColumn(columnIndex) || rowIndex < 0)
                return false;

            return GetAccountView(rowIndex)?.AccountType is "Bank" or "Credit";
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
            ApplyRunningBalances(chronological, GetStartingBalance(account.Id));
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

            if (SelectedShowMode() != ShowRecurringRules)
                return;

            if (GetSelectedRuleView() is not UnifiedTransactionView view)
                return;

            OpenRuleEditor(view);
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
                ? _txOrchestrator.GetTransactionsForAccount(view.AccountId).Posted.FirstOrDefault(t => t.Id == view.Id)
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
            menu.Closed += (_, _) => menu.Dispose();
            menu.Show(detailGrid, new Point(cell.Left, cell.Bottom));
        }

        private void AssignCategory(UnifiedTransactionView view, Guid categoryId)
        {
            _categoryOrchestrator.AssignToPosted(view.Id, categoryId);
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
                var rule = _ruleOrchestrator.GetSingleRule(view.Id);
                if (rule is null)
                    return;

                using var editor = new RecurringRuleEditor(view.AccountId, rule);
                if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                    RefreshAll();
                return;
            }

            var transfer = _ruleOrchestrator.GetTransferRule(view.Id);
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
                _ruleOrchestrator.DeleteSingleRule(view.Id);
            else
                _ruleOrchestrator.DeleteTransferRule(view.Id);

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
            if (_accountsSource.Current is not UnifiedAccountView account)
                return;

            LoadTransactionsForAccount(account.Id);
            LoadBudgetsForAccount(account.Id);
        }

        private void LoadBudgetsForAccount(Guid accountId)
        {
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
