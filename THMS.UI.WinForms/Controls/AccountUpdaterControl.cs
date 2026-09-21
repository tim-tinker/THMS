using System.ComponentModel;
using System.Diagnostics;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
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
        private readonly ContextMenuStrip _accountMenu = new();
        private bool _suppressSelectionEvents;

        public event EventHandler? SelectedAccountChanged;

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
            gridAccounts.ContextMenuStrip = _accountMenu;
            components ??= new Container();
            components.Add(_accountMenu);
            _accountMenu.Opening += OnAccountMenuOpening;
            LoadAccounts();
        }

        public void RefreshAccounts() => LoadAccounts();

        public void ImportAccountsFromFile() => OnImportAccounts(this, EventArgs.Empty);

        private void LoadAccounts()
        {
            var selectedId = GetSelectedAccount()?.Id;
            var sortColumn = gridAccounts.SortedColumn?.Name;
            var sortOrder = gridAccounts.SortOrder;
            var posted = _transactions.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList();
            var accounts = _accountOrchestrator.GetAllAccounts()
                .Select(account => AccountRegisterRow.From(
                    account,
                    _statements.GetForAccount(account.Id).ToList(),
                    posted.Where(tx => tx.AccountId == account.Id)))
                .ToList();
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
                SelectAccount(selectedId);
            }
            finally
            {
                _suppressSelectionEvents = false;
            }

            UpdateActionButtons();
            SelectedAccountChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetImportStatus(string message) => lblStatus.Text = message;

        private void SelectAccount(Guid? id)
        {
            if (id is null)
                return;

            foreach (DataGridViewRow row in gridAccounts.Rows)
            {
                if (row.DataBoundItem is AccountRegisterRow item && item.Id == id)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0 && row.Cells[0].Visible)
                        gridAccounts.CurrentCell = row.Cells[0];
                    return;
                }
            }
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
                var rows = _importOrchestrator.LoadAccountsFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show(FindForm(), "The selected file did not contain any accounts.", "Import Accounts",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new AccountImportPreviewDialog(rows, _importOrchestrator);
                if (preview.ShowDialog(FindForm()) != DialogResult.OK)
                    return;

                LoadAccounts();
                SetImportStatus(ImportStatusText.Imported(preview.Result, "account", "accounts"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Could not parse the file.\n{ex.Message}", "Import Accounts",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

            gridAccounts.ClearSelection();
            gridAccounts.Rows[hit.RowIndex].Selected = true;
            var cell = gridAccounts.Rows[hit.RowIndex].Cells.Cast<DataGridViewCell>()
                .FirstOrDefault(c => c.Visible);
            if (cell is not null)
                gridAccounts.CurrentCell = cell;
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

            switch (account)
            {
                case CreditAccount:
                    _accountMenu.Items.Add(MenuItem("Add Charge", (_, _) => AddCharge(account)));
                    _accountMenu.Items.Add(PayMenuItem(account, latest));
                    break;
                case LoanAccount or MortgageAccount:
                    _accountMenu.Items.Add(MenuItem("Add Interest", (_, _) => AddInterest(account)));
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, existing is null ? "Add Statement" : "Edit Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AddCharge(Account account)
        {
            using var dlg = new AccountEntryDialog(
                "Add Charge",
                "Negative amounts are purchases. Positive amounts are refunds.",
                Categories(),
                defaultDescription: "Charge");
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            RunActivity(() => _activityOrchestrator.AddPending(
                account.Id, dlg.Date, dlg.Amount, dlg.Description, dlg.CategoryId));
        }

        private void AddInterest(Account account)
        {
            using var dlg = new AccountEntryDialog(
                "Add Interest",
                "Interest is posted now. A positive amount increases the balance owed.",
                Categories(),
                DefaultExpenseCategories.InterestId,
                "Interest");
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            RunActivity(() => _activityOrchestrator.AddPosted(
                account.Id, dlg.Date, dlg.Amount, dlg.Description, dlg.CategoryId));
        }

        private void AddBankTransaction(Account account)
        {
            using var dlg = new AccountEntryDialog(
                "Add Transaction",
                "Negative amounts leave this account. Positive amounts are deposits. The other party is not a THMS account.",
                Categories());
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            RunActivity(() => _activityOrchestrator.AddPending(
                account.Id, dlg.Date, dlg.Amount, dlg.Description, dlg.CategoryId));
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
                latest?.DueDate ?? latest?.StatementDate);
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

        private void RunActivity(Action action)
        {
            try
            {
                action();
                LoadAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            if (gridAccounts.Columns[e.ColumnIndex].Name != "Name")
                return;
            if (gridAccounts.Rows[e.RowIndex].DataBoundItem is not AccountRegisterRow row)
                return;
            if (gridAccounts.Rows[e.RowIndex].Cells[e.ColumnIndex] is not DataGridViewLinkCell cell)
                return;

            if (string.IsNullOrWhiteSpace(row.WebsiteUrl))
            {
                cell.LinkBehavior = LinkBehavior.NeverUnderline;
                cell.LinkColor = gridAccounts.DefaultCellStyle.ForeColor;
                cell.ActiveLinkColor = gridAccounts.DefaultCellStyle.ForeColor;
                cell.VisitedLinkColor = gridAccounts.DefaultCellStyle.ForeColor;
            }
            else
            {
                cell.LinkBehavior = LinkBehavior.HoverUnderline;
                cell.LinkColor = Color.FromArgb(0, 99, 177);
                cell.ActiveLinkColor = Color.FromArgb(0, 70, 127);
                cell.VisitedLinkColor = Color.FromArgb(0, 99, 177);
            }
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
                MessageBox.Show(this, ex.Message, "Accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show(this, ex.Message, "Accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
