using System.ComponentModel;
using System.Diagnostics;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class AccountUpdaterControl : UserControl
    {
        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly AccountImportOrchestrator _importOrchestrator = new();
        private readonly PlanningOrchestrator _planningOrchestrator = new();
        private readonly AccountActivityOrchestrator _activityOrchestrator = new();
        private readonly BillsOrchestrator _billsOrchestrator = new();
        private readonly CategoryOrchestrator _categoryOrchestrator = new();
        private readonly IAccountStatementDataStore _statements = new DataStoreFactory().GetAccountStatementStore();
        private readonly ITransactionDataStore _transactions = new DataStoreFactory().GetTransactionStore();
        private readonly IAccountDataStore _accounts = new DataStoreFactory().GetAccountStore();
        private readonly ContextMenuStrip _accountMenu = new();
        private bool _suppressSelectionEvents;
        private int _accountLoadGeneration;
        private int _appliedAccountGeneration;
        private bool _selectFirstOnLoad;
        private Guid? _pendingAccountId;
        private Font? _unreconciledAccountFont;

        public event EventHandler? SelectedAccountChanged;
        public event EventHandler? StatementsChanged;

        public Account? SelectedAccount => GetSelectedAccount();

        public AccountUpdaterControl()
        {
            InitializeComponent();
            DataGridViewUtil.EnableDoubleBuffering(gridAccounts);
            gridAccounts.SelectionChanged += (_, _) => OnAccountSelectionChanged();
            gridAccounts.DataBindingComplete += (_, _) => OnAccountSelectionChanged();
            gridAccounts.CellDoubleClick += OnAccountCellDoubleClick;
            gridAccounts.CellContentClick += OnAccountNameClicked;
            gridAccounts.CellFormatting += OnAccountCellFormatting;
            gridAccounts.MouseDown += OnAccountGridMouseDown;
            gridAccounts.MouseUp += OnAccountGridMouseUp;
            gridAccounts.KeyDown += OnAccountGridKeyDown;
            components ??= new Container();
            components.Add(_accountMenu);
            _accountMenu.Opening += OnAccountMenuOpening;
        }

        public void RefreshAccounts() => LoadAccounts();

        public void Pay(Guid accountId)
        {
            var account = _accountOrchestrator.GetAccount(accountId);
            if (account is null || !SupportsPay(account))
                return;
            PayAccount(account, LatestStatement(account));
        }

        private static bool SupportsPay(Account account) =>
            account is CreditAccount or LoanAccount or MortgageAccount or UntrackedAccount;

        public void ImportAccountsFromFile() => OnImportAccounts(this, EventArgs.Empty);

        private void LoadAccounts() => _ = LoadAccountsAsync();

        private async Task LoadAccountsAsync()
        {
            var generation = Interlocked.Increment(ref _accountLoadGeneration);
            if (Visible)
                AppStatus.Set("Loading accounts...", busy: true);

            List<AccountRegisterRow> accounts;
            try
            {
                accounts = await Task.Run(QueryAccountRows);
            }
            catch (Exception ex)
            {
                OnUi(() =>
                {
                    if (generation != _accountLoadGeneration || IsDisposed)
                        return;
                    if (Visible)
                        AppStatus.Set("Could not load accounts.");
                    ExceptionDialog.Show(FindForm(), "Accounts", ex);
                });
                return;
            }

            if (generation != _accountLoadGeneration || IsDisposed)
                return;

            OnUi(() => ApplyAccountRows(generation, accounts));
        }

        private List<AccountRegisterRow> QueryAccountRows()
        {
            var posted = _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var unreconciled = new ReconciliationOrchestrator(_transactions).AccountIdsWithUnreconciled();
            var paymentPlans = _transactions.GetAllRecurringTransferRules()
                .Where(rule => rule.IsPaymentPlan && rule.IsActive)
                .GroupBy(rule => rule.ToAccountId)
                .ToDictionary(group => group.Key, group => group.First().Amount);
            var itemState = _accounts.GetPlaidItemSyncStates()
                .ToDictionary(s => s.ItemId, StringComparer.Ordinal);
            return _accountOrchestrator.GetAllAccounts()
                .Select(account =>
                {
                    itemState.TryGetValue(account.ExternalLink?.ItemId ?? "", out var state);
                    return AccountRegisterRow.From(
                        account,
                        _statements.GetForAccount(account.Id).ToList(),
                        posted.Where(tx => tx.AccountId == account.Id),
                        AccountRegisterRow.PlaidStatusOf(account, state),
                        unreconciled.Contains(account.Id),
                        paymentPlans.TryGetValue(account.Id, out var planAmount) ? planAmount : null);
                })
                .ToList();
        }

        private void ApplyAccountRows(int generation, List<AccountRegisterRow> accounts)
        {
            if (generation != _accountLoadGeneration || IsDisposed)
                return;

            _appliedAccountGeneration = generation;
            var selectFirst = _selectFirstOnLoad;
            _selectFirstOnLoad = false;
            var selectedId = selectFirst ? null : _pendingAccountId ?? GetSelectedAccount()?.Id;
            var firstDisplayed = gridAccounts.RowCount > 0
                ? gridAccounts.FirstDisplayedScrollingRowIndex
                : -1;
            var sortColumn = gridAccounts.SortedColumn?.Name;
            var sortOrder = gridAccounts.SortOrder;
            _suppressSelectionEvents = true;
            try
            {
                gridAccounts.DataSource = null;
                gridAccounts.DataSource = new SortableBindingList<AccountRegisterRow>(
                    accounts,
                    (property, direction) => Comparer<AccountRegisterRow>.Create((left, right) =>
                        AccountRegisterRow.Compare(
                            left,
                            right,
                            property.Name,
                            direction == ListSortDirection.Descending)));
                if (sortColumn is not null
                    && sortOrder != SortOrder.None
                    && gridAccounts.Columns[sortColumn] is DataGridViewColumn column)
                {
                    gridAccounts.Sort(
                        column,
                        sortOrder == SortOrder.Descending
                            ? ListSortDirection.Descending
                            : ListSortDirection.Ascending);
                }
                if (selectFirst)
                    SelectGridRow(0);
                else
                    SelectAccount(selectedId);
                RestoreScrollPosition(firstDisplayed);
            }
            finally
            {
                _suppressSelectionEvents = false;
            }

            _pendingAccountId = null;
            UpdateActionButtons();
            if (Visible)
                AppStatus.Set("Ready.");
            SelectedAccountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnUi(Action action)
        {
            if (IsDisposed)
                return;
            if (!IsHandleCreated)
            {
                HandleCreated += OnCreated;
                return;

                void OnCreated(object? sender, EventArgs e)
                {
                    HandleCreated -= OnCreated;
                    if (!IsDisposed)
                        BeginInvoke(action);
                }
            }

            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        public void SelectFirstAccount()
        {
            _pendingAccountId = null;
            if (gridAccounts.Rows.Count == 0)
            {
                _selectFirstOnLoad = true;
                if (_accountLoadGeneration == _appliedAccountGeneration)
                    LoadAccounts();
                return;
            }

            if (_accountLoadGeneration != _appliedAccountGeneration)
                _selectFirstOnLoad = true;
            SelectGridRow(0);
        }

        public void SelectAccount(Guid? id)
        {
            _selectFirstOnLoad = false;
            _pendingAccountId = id;
            if (id is null)
                return;

            foreach (DataGridViewRow row in gridAccounts.Rows)
            {
                if (row.DataBoundItem is AccountRegisterRow item && item.Id == id)
                {
                    SelectGridRow(row.Index);
                    return;
                }
            }
        }

        private void SelectGridRow(int index)
        {
            if (index < 0 || index >= gridAccounts.Rows.Count)
                return;

            var row = gridAccounts.Rows[index];
            var column = gridAccounts.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
            if (column is null)
                return;

            try
            {
                gridAccounts.CurrentCell = row.Cells[column.Index];
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void RestoreScrollPosition(int firstDisplayed)
        {
            if (firstDisplayed < 0 || gridAccounts.RowCount == 0)
                return;

            ApplyScrollPosition(firstDisplayed);
            if (gridAccounts.IsHandleCreated)
                gridAccounts.BeginInvoke(() => ApplyScrollPosition(firstDisplayed));
        }

        private void ApplyScrollPosition(int firstDisplayed)
        {
            if (gridAccounts.IsDisposed || gridAccounts.RowCount == 0)
                return;

            var visible = Math.Max(1, gridAccounts.DisplayedRowCount(includePartialRow: false));
            var maxFirst = Math.Max(0, gridAccounts.RowCount - visible);
            var index = Math.Clamp(firstDisplayed, 0, maxFirst);
            if (gridAccounts.FirstDisplayedScrollingRowIndex != index)
                gridAccounts.FirstDisplayedScrollingRowIndex = index;
        }

        private void OnAccountSelectionChanged()
        {
            UpdateActionButtons();
            if (!_suppressSelectionEvents)
                SelectedAccountChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateActionButtons()
        {
            var hasSelection = GetSelectedAccount() is not null;
            btnDelete.Enabled = hasSelection;
        }

        private void OnImportAccounts(object sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Select account spreadsheet"
            };
            if (fileDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing accounts...", busy: true);
                var rows = _importOrchestrator.LoadAccountsFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(FindForm(), "The selected file did not contain any accounts.", "Import Accounts",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _importOrchestrator.ImportAccounts(rows, AppStatus.ForImport());
                LoadAccounts();
                AppStatus.Set(ImportStatusText.Imported(result, "account", "accounts"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                ExceptionDialog.Show(FindForm(), "Import Accounts", ex, "Could not parse the file.");
            }
        }

        private void ImportTransactionsForAccount(Account account)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Spreadsheets (*.csv;*.xlsx;*.xls)|*.csv;*.xlsx;*.xls|CSV files (*.csv)|*.csv|Excel files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*",
                Title = $"Import transactions for {account.Name}"
            };
            if (fileDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            TransactionFileImportLauncher.ImportFile(FindForm(), account, fileDialog.FileName, LoadAccounts);
        }

        private void OnRunDiagnostics(object sender, EventArgs e)
        {
            using var dialog = new AccountDiagnosticsDialog();
            dialog.ShowDialog(FindForm());
        }

        private void OnAccountCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            EditSelectedAccount();
        }

        private void OnAccountGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = gridAccounts.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;

            SelectAccountRow(hit.RowIndex, hit.ColumnIndex);
        }

        private void OnAccountGridMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = gridAccounts.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;

            if (!SelectAccountRow(hit.RowIndex, hit.ColumnIndex))
                return;

            _accountMenu.Show(gridAccounts, e.Location);
        }

        private void OnAccountGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Apps && (e.KeyCode != Keys.F10 || !e.Shift))
                return;
            if (gridAccounts.CurrentCell is not { } cell)
                return;

            var bounds = gridAccounts.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, cutOverflow: false);
            _accountMenu.Show(gridAccounts, new Point(bounds.Left, bounds.Bottom));
            e.Handled = true;
        }

        private bool SelectAccountRow(int rowIndex, int columnIndex)
        {
            var alreadyCurrent = gridAccounts.CurrentCell?.RowIndex == rowIndex
                && (columnIndex < 0 || gridAccounts.CurrentCell?.ColumnIndex == columnIndex)
                && DataGridViewUtil.CellIsSelected(gridAccounts, rowIndex, columnIndex);
            if (alreadyCurrent)
                return true;

            if (!DataGridViewUtil.SelectOnlyCell(gridAccounts, rowIndex, columnIndex))
                return false;
            // A current-cell change made while the right button is down does not
            // raise SelectionChanged, so the statements and other child grids stay put.
            OnAccountSelectionChanged();
            return true;
        }

        private void OnAccountMenuOpening(object? sender, CancelEventArgs e)
        {
            _accountMenu.Items.Clear();
            var account = GetSelectedAccount();
            if (account is null)
            {
                e.Cancel = true;
                return;
            }

            var latest = LatestStatement(account);
            var canStatement = StatementAccountMatch.ForAccount(account) is not null;
            _accountMenu.Items.Add(MenuItem("View/Edit Account", (_, _) => EditSelectedAccount()));
            var editStatement = MenuItem("View/Edit Latest Statement", (_, _) => EditLatestStatement(account, latest));
            editStatement.Enabled = latest is not null;
            _accountMenu.Items.Add(editStatement);
            var addStatement = MenuItem("Add Statement", (_, _) => AddStatement(account));
            addStatement.Enabled = canStatement;
            _accountMenu.Items.Add(addStatement);
            _accountMenu.Items.Add(MenuItem("Import Transactions", (_, _) => ImportTransactionsForAccount(account)));

            switch (account)
            {
                case CreditAccount:
                    _accountMenu.Items.Add(MenuItem("Add Charge", (_, _) => AddCharge(account)));
                    _accountMenu.Items.Add(PayMenuItem(account, latest));
                    _accountMenu.Items.Add(MenuItem("Payment plan", (_, _) => OpenPaymentPlan(account, latest)));
                    break;
                case LoanAccount or MortgageAccount:
                    _accountMenu.Items.Add(MenuItem("Add Transaction", (_, _) => AddBankTransaction(account)));
                    _accountMenu.Items.Add(PayMenuItem(account, latest));
                    break;
                case BankAccount or InvestmentAccount or InternalAccount:
                    _accountMenu.Items.Add(MenuItem("Add Transaction", (_, _) => AddBankTransaction(account)));
                    _accountMenu.Items.Add(TransferMenuItem(account));
                    break;
                case UntrackedAccount:
                    _accountMenu.Items.Add(PayMenuItem(account, latest));
                    break;
            }
        }

        private ToolStripMenuItem PayMenuItem(Account account, AccountStatement? latest)
        {
            var item = MenuItem("Pay", (_, _) => PayAccount(account, latest));
            item.Enabled = PayFromAccounts(account).Count > 0;
            return item;
        }

        private ToolStripMenuItem TransferMenuItem(Account account)
        {
            var item = MenuItem("Transfer", (_, _) => TransferFrom(account));
            item.Enabled = TransferAccounts(account.Id).Count > 0;
            return item;
        }

        private static ToolStripMenuItem MenuItem(string text, EventHandler onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += onClick;
            return item;
        }

        private void EditLatestStatement(Account account, AccountStatement? latest)
        {
            if (latest is null)
                return;
            OpenStatementEditor(account, latest);
        }

        private void AddStatement(Account account) => OpenStatementEditor(account, existing: null);

        private void OpenStatementEditor(Account account, AccountStatement? existing)
        {
            if (StatementAccountMatch.ForAccount(account) is null)
            {
                MessageBox.Show(FindForm(), $"Statements are not supported for {account.Name}.", "Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var dlg = new StatementEditorDialog(_planningOrchestrator, existing, account);
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                    return;
                LoadAccounts();
                StatementsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(FindForm(), existing is null ? "Add Statement" : "Edit Statement", ex);
            }
        }

        private void AddCharge(Account account)
        {
            using var dlg = new AccountEntryDialog(
                "Add Charge",
                "A charge is a positive amount. A refund is negative.",
                Categories(),
                defaultDescription: "Charge",
                accounts: _accountOrchestrator.GetAllAccounts().ToList());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            RunActivity(() => SavePosted(account, dlg));
        }

        private void AddBankTransaction(Account account)
        {
            var hint = account is LoanAccount or MortgageAccount
                ? "A charge or interest is a positive amount. A payment is negative."
                : "Negative amounts leave this account. Positive amounts are deposits. The other party is not a THMS account.";
            using var dlg = new AccountEntryDialog(
                "Add Transaction",
                hint,
                Categories(),
                accounts: _accountOrchestrator.GetAllAccounts().ToList());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            RunActivity(() => SavePosted(account, dlg));
        }

        private void TransferFrom(Account account)
        {
            var others = TransferAccounts(account.Id);
            if (others.Count == 0)
                return;

            using var dlg = new AccountCounterpartyDialog(
                "Transfer",
                "To account",
                others);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            RunActivity(() => _activityOrchestrator.AddTransfer(
                account.Id, dlg.CounterpartyAccountId, dlg.Date, dlg.Amount, dlg.Description));
        }

        private void OpenPaymentPlan(Account account, AccountStatement? latest)
        {
            var funding = PayFromAccounts(account);
            if (funding.Count == 0)
            {
                MessageBox.Show(FindForm(), "Add a bank account to pay from.", "Payment plan",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var promotions = latest is CreditCardStatement card
                ? card.Promotions.Where(promo => promo.CurrentBalance > 0).ToList()
                : [];
            var existing = _billsOrchestrator.GetPaymentPlan(account.Id);
            using var dlg = new PaymentPlanDialog(
                account,
                funding,
                PromotionPaymentPlanner.StatementBalanceOf(latest),
                promotions,
                latest?.DueDate ?? latest?.StatementDate,
                existing);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            if (dlg.DeletePlan)
            {
                RunActivity(() => _billsOrchestrator.DeletePaymentPlan(account.Id));
                return;
            }

            RunActivity(() => _billsOrchestrator.SavePaymentPlan(
                account.Id,
                dlg.FundingAccountId,
                dlg.Amount,
                dlg.Frequency,
                dlg.NextDate,
                dlg.EndDate));
        }

        private void PayAccount(Account account, AccountStatement? latest)
        {
            var funding = PayFromAccounts(account);
            if (funding.Count == 0)
            {
                MessageBox.Show(FindForm(), "Add a bank account to pay from.", "Pay",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var promotions = latest is CreditCardStatement card
                ? card.Promotions.Where(promo => promo.CurrentBalance > 0).ToList()
                : [];
            using var dlg = new PayAccountDialog(
                account,
                funding,
                PromotionPaymentPlanner.StatementBalanceOf(latest),
                latest is { AmountDue: > 0 } ? latest.AmountDue : null,
                latest is null ? "Payment" : account.Name,
                promotions,
                latest?.DueDate ?? latest?.StatementDate,
                account.AutoPayFromAccountId);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            RunActivity(() => _billsOrchestrator.AddManual(
                account.Id,
                dlg.FundingAccountId,
                dlg.Amount,
                dlg.Date,
                dlg.Description,
                latest?.Id));
        }

        private void SavePosted(Account account, AccountEntryDialog dlg)
        {
            var posted = _activityOrchestrator.AddPosted(
                account.Id,
                dlg.Date,
                PostedBalanceCalculator.ToLedgerPostedDelta(account, dlg.Amount),
                dlg.Description,
                dlg.CategoryId);
            if (!dlg.HasPendingSplits)
                return;

            var rows = dlg.PendingSplits.Select(split =>
            {
                var copy = split.Clone();
                copy.Amount = PostedBalanceCalculator.ToLedgerPostedDelta(account, copy.Amount);
                return copy;
            }).ToList();
            var transactions = new TransactionOrchestrator();
            if (!transactions.TryApplyAsWholeTransfer(posted.Id, rows))
                transactions.ApplySplits(posted.Id, rows);
        }

        private void RunActivity(Action action)
        {
            try
            {
                action();
                LoadAccounts();
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(FindForm(), "Accounts", ex);
            }
        }

        private AccountStatement? LatestStatement(Account account) =>
            _statements.GetForAccount(account.Id)
                .OrderByDescending(s => s.StatementDate)
                .ThenByDescending(s => s.DueDate)
                .FirstOrDefault();

        private List<Account> PayFromAccounts(Account destination) =>
            AllAccounts()
                .Where(a => a.Id != destination.Id)
                .Where(a => destination is CreditAccount or LoanAccount or MortgageAccount
                    ? a is BankAccount
                    : a is BankAccount or CreditAccount)
                .OrderBy(a => a.Name)
                .ToList();

        private List<Account> TransferAccounts(Guid fromId) =>
            AllAccounts()
                .Where(a => a.Id != fromId)
                .OrderBy(a => a.Name)
                .ToList();

        private List<ExpenseCategory> Categories() =>
            _categoryOrchestrator.GetActiveCategories().ToList();

        private void OnAccountNameClicked(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (gridAccounts.Columns[e.ColumnIndex].Name != "Name")
                return;
            if (gridAccounts.Rows[e.RowIndex].DataBoundItem is not AccountRegisterRow row)
                return;

            OpenWebsite(row.WebsiteUrl);
        }

        private void OnAccountCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (gridAccounts.Rows[e.RowIndex].DataBoundItem is not AccountRegisterRow row)
                return;

            e.CellStyle.Font = row.HasUnreconciled
                ? _unreconciledAccountFont ??= new Font(gridAccounts.Font, FontStyle.Bold)
                : gridAccounts.Font;

            if (gridAccounts.Columns[e.ColumnIndex].Name != "Name")
                return;
            if (gridAccounts.Rows[e.RowIndex].Cells[e.ColumnIndex] is not DataGridViewLinkCell cell)
                return;

            DataGridViewUtil.ApplyLinkAppearance(
                cell,
                e.CellStyle,
                !string.IsNullOrWhiteSpace(row.WebsiteUrl),
                gridAccounts.DefaultCellStyle.ForeColor);
        }

        private static void OpenWebsite(string? url)
        {
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

        private void OnAddAccount(object sender, EventArgs e)
        {
            using var dlg = new AccountEditForm(null, AllAccounts());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            SaveAndReload(dlg.Account);
        }

        private void EditSelectedAccount()
        {
            var acct = GetSelectedAccount();
            if (acct == null)
                return;

            using var dlg = new AccountEditForm(acct, AllAccounts());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            SaveAndReload(dlg.Account);
        }

        private void OnDeleteAccount(object sender, EventArgs e)
        {
            var acct = GetSelectedAccount();
            if (acct == null)
            {
                MessageBox.Show(this, "Please select an account to delete.", "Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                this,
                $"Delete account '{acct.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                _accountOrchestrator.Delete(acct.Id);
                LoadAccounts();
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(this, "Accounts", ex);
            }
        }

        private void SaveAndReload(Account account)
        {
            try
            {
                _accountOrchestrator.Save(account);
                _planningOrchestrator.SyncAutoPayForAccount(account);
                LoadAccounts();
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(this, "Accounts", ex);
            }
        }

        private IReadOnlyList<Account> AllAccounts() =>
            _accountOrchestrator.GetAllAccounts().ToList();

        private Account? GetSelectedAccount()
        {
            if (gridAccounts.CurrentRow?.DataBoundItem is AccountRegisterRow row)
                return row.Account;

            return null;
        }
    }
}
