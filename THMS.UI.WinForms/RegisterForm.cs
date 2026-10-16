using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class RegisterForm : BaseEmbeddedForm
    {
        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly TransactionImportOrchestrator _importOrchestrator = new();
        private readonly RecurringRuleImportOrchestrator _ruleImportOrchestrator = new();
        private readonly RecurringTransferImportOrchestrator _transferImportOrchestrator = new();
        private readonly PlanningOrchestrator _planningOrchestrator = new();
        private readonly TransactionOrchestrator _transactionOrchestrator = new();
        private readonly CategoryOrchestrator _categoryOrchestrator = new();
        private readonly BindingSource _statementsSource = new();
        private readonly BindingSource _statementDetailsSource = new();
        private readonly BindingSource _statementTransactionsSource = new();
        private List<AccountStatement> _accountStatements = [];
        private SplitContainer? _statementColumns;
        private SplitContainer? _statementRows;
        private bool _statementColumnsPending = true;
        private bool _lineItemsSplitPending;
        private bool _suspendStatementSelection;
        private int _loadedRevision = int.MinValue;

        private bool _accountSplitReady;

        public RegisterForm()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(split);
            ledger.HostProvidesAccounts = true;
            categoryManager.Bind(new CategoryOrchestrator(), reload: false);
            categoryManager.CatalogChanged += (_, _) => ledger.RefreshCurrentAccount();
            ConfigureStatementGrid();
            tabsTop.RecalculateItemSize();
            tabs.RecalculateItemSize();
            lblSelectedAccount.Height = Math.Max(
                lblSelectedAccount.PreferredHeight,
                lblSelectedAccount.Font.Height + lblSelectedAccount.Padding.Vertical);
            split.SizeChanged += (_, _) => LayoutAccountSplit();
            tabsTop.SelectedIndexChanged += (_, _) =>
            {
                if (tabsTop.SelectedTab == tabCategories)
                {
                    categoryManager.RefreshLayout();
                    categoryManager.ReloadIfClean();
                }
                else
                    LayoutAccountSplit();
            };
            tabs.SelectedIndexChanged += (_, _) =>
            {
                if (tabs.SelectedTab == tabBills)
                    billsControl.Reload();
                if (tabs.SelectedTab == tabLedger)
                    ledger.SelectAccount(accountUpdater.SelectedAccount?.Id);
            };
            billsControl.DataChanged += (_, _) =>
            {
                accountUpdater.RefreshAccounts();
                ledger.SelectAccount(accountUpdater.SelectedAccount?.Id);
            };
            ledger.DataChanged += (_, _) => accountUpdater.RefreshAccounts();
            accountUpdater.SelectedAccountChanged += (_, _) => LoadSelectedAccount();
            historyBar.SelectedPeriodChanged += (_, _) => ApplyHistoryPeriod();
        }

        public void ShowHome()
        {
            tabsTop.SelectedTab = tabAccounts;
            tabs.SelectedTab = tabStatements;
            accountUpdater.SelectFirstAccount();
            if (accountUpdater.SelectedAccount is not null)
                LoadStatementsForSelectedAccount();
        }

        public void ShowAccount(Guid accountId)
        {
            tabsTop.SelectedTab = tabAccounts;
            tabs.SelectedTab = tabLedger;
            accountUpdater.SelectAccount(accountId);
        }

        private void LayoutAccountSplit()
        {
            if (_accountSplitReady)
                return;

            const int panel1Min = 140;
            const int panel2Min = 180;
            var available = split.Height - split.SplitterWidth;
            if (available < panel1Min + panel2Min)
                return;

            split.Panel1MinSize = panel1Min;
            split.Panel2MinSize = panel2Min;
            split.SplitterDistance = Math.Clamp(260, panel1Min, available - panel2Min);
            _accountSplitReady = true;
        }

        private void ApplyHistoryPeriod()
        {
            var period = historyBar.SelectedPeriod;
            ledger.SetGridDataSource(period);
            categoryManager.SetHistoryPeriod(period);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || Disposing)
                return;

            categoryManager.ReloadIfClean();

            if (FinanceDataRevision.Current == _loadedRevision)
            {
                billsControl.Reload();
                LoadStatementsForSelectedAccount();
                return;
            }

            accountUpdater.RefreshAccounts();
        }

        private void ConfigureStatementGrid()
        {
            DataGridViewUtil.EnableDoubleBuffering(gridStatements);
            gridStatements.Columns.Clear();
            gridStatements.Columns.AddRange(
                TextColumn(nameof(AccountStatementListRow.Type), "Type"),
                TextColumn(nameof(AccountStatementListRow.StatementDate), "Statement Date"),
                TextColumn(nameof(AccountStatementListRow.DueDate), "Due Date"),
                MoneyColumn(nameof(AccountStatementListRow.AmountDue), "Amount Due"),
                MoneyColumn(nameof(AccountStatementListRow.StatementBalance), "Statement Balance"),
                MoneyColumn(nameof(AccountStatementListRow.EscrowBalance), "Escrow Balance"),
                FillColumn(nameof(AccountStatementListRow.Notes), "Notes"));
            gridStatements.DataSource = _statementsSource;
            gridStatements.SelectionChanged += (_, _) =>
            {
                if (!_suspendStatementSelection)
                    LoadStatementDetails();
            };
            gridStatements.CellDoubleClick += OnStatementCellDoubleClick;
            HostStatementDetails();
        }

        private void HostStatementDetails()
        {
            var splitStatements = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel2Collapsed = true,
                Name = "splitStatements"
            };
            pnlStatements.Controls.Remove(gridStatements);
            gridStatements.Dock = DockStyle.Fill;
            splitStatements.Panel1.Controls.Add(gridStatements);

            var gridDetails = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                Name = "gridStatementDetails",
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            DataGridViewUtil.EnableDoubleBuffering(gridDetails);
            var amountColumn = TextColumn(nameof(StatementChildRow.Amount), "Amount");
            amountColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            var detailsColumn = TextColumn(nameof(StatementChildRow.Details), "Details");
            detailsColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            gridDetails.Columns.AddRange(
                TextColumn(nameof(StatementChildRow.Kind), "Kind"),
                TextColumn(nameof(StatementChildRow.Description), "Description"),
                amountColumn,
                detailsColumn);
            gridDetails.DataSource = _statementDetailsSource;
            gridDetails.CellDoubleClick += OnStatementCellDoubleClick;
            splitStatements.Panel2.Controls.Add(SectionHost(gridDetails, "Line items"));
            splitStatements.SizeChanged += (_, _) => OnStatementRowsSized();
            _statementRows = splitStatements;

            var gridTransactions = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                Name = "gridStatementTransactions",
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            DataGridViewUtil.EnableDoubleBuffering(gridTransactions);
            gridTransactions.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            var txDate = TextColumn(nameof(UnifiedTransactionView.Date), "Transaction Date");
            txDate.DefaultCellStyle.Format = "d";
            var txAmount = TextColumn(nameof(UnifiedTransactionView.Amount), "Amount");
            txAmount.DefaultCellStyle.Format = "c2";
            txAmount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            txAmount.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            var txDescription = TextColumn(nameof(UnifiedTransactionView.Description), "Description");
            txDescription.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            txDescription.MinimumWidth = 120;
            gridTransactions.Columns.AddRange(
                txDate,
                txAmount,
                TextColumn(nameof(UnifiedTransactionView.Category), "Category"),
                txDescription);
            gridTransactions.DataSource = _statementTransactionsSource;

            var columns = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel2Collapsed = true,
                Name = "splitStatementColumns"
            };
            columns.Panel1.Controls.Add(splitStatements);
            columns.Panel2.Controls.Add(gridTransactions);
            columns.SizeChanged += (_, _) => OnStatementColumnsSized();
            _statementColumns = columns;

            pnlStatements.Controls.Add(columns);
            // Dock the button bar first so the grids fill only the space above it.
            columns.BringToFront();
            SplitContainerUtil.MakeSplitterVisible(splitStatements);
            SplitContainerUtil.MakeSplitterVisible(columns);
        }

        private Panel SectionHost(Control body, string title)
        {
            var host = new Panel { Dock = DockStyle.Fill };
            var font = new Font(Font.FontFamily, Font.Size + 3f, FontStyle.Bold);
            var labelPad = LogicalToDeviceUnits(6);
            var labelHeight = TextRenderer.MeasureText(title, font).Height + (labelPad * 2);
            var label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Font = font,
                Height = Math.Max(LogicalToDeviceUnits(32), labelHeight),
                Padding = new Padding(LogicalToDeviceUnits(8), labelPad, LogicalToDeviceUnits(8), labelPad),
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            host.Controls.Add(body);
            host.Controls.Add(label);
            return host;
        }

        private void TryApplyStatementColumnSplit()
        {
            if (!_statementColumnsPending || _statementColumns is null || _statementColumns.Panel2Collapsed || !_statementColumns.IsHandleCreated)
                return;

            const int min = 220;
            var available = _statementColumns.Width - _statementColumns.SplitterWidth;
            if (available < min * 2)
                return;

            _statementColumns.Panel1MinSize = min;
            _statementColumns.Panel2MinSize = min;
            _statementColumns.SplitterDistance = available / 2;
            _statementColumnsPending = false;
        }

        private void OnStatementColumnsSized()
        {
            if (_statementColumns is null)
                return;

            var show = _statementTransactionsSource.Count > 0;
            if (_statementColumns.Panel2Collapsed == show)
                UpdateTransactionSplit();
            else
                TryApplyStatementColumnSplit();
        }

        private void UpdateTransactionSplit()
        {
            if (_statementColumns is null || _statementColumns.IsDisposed)
                return;

            var show = _statementTransactionsSource.Count > 0;
            if (_statementColumns.Panel2Collapsed == !show)
                return;

            try
            {
                if (!show)
                {
                    _statementColumns.Panel2Collapsed = true;
                    return;
                }

                _statementColumns.Panel2Collapsed = false;
                TryApplyStatementColumnSplit();
            }
            catch (InvalidOperationException)
            {
                _statementColumnsPending = true;
            }
        }

        private void OnStatementRowsSized()
        {
            if (_statementRows is null)
                return;

            var show = _statementDetailsSource.Count > 0;
            if (_statementRows.Panel2Collapsed == show)
                UpdateLineItemsSplit();
            else
                TryApplyLineItemsSplitDistance();
        }

        private void UpdateLineItemsSplit()
        {
            if (_statementRows is null || _statementRows.IsDisposed)
                return;

            var show = _statementDetailsSource.Count > 0;
            if (_statementRows.Panel2Collapsed == !show)
                return;

            try
            {
                if (!show)
                {
                    _lineItemsSplitPending = false;
                    _statementRows.Panel2Collapsed = true;
                    return;
                }

                _lineItemsSplitPending = true;
                _statementRows.Panel2Collapsed = false;
                TryApplyLineItemsSplitDistance();
            }
            catch (InvalidOperationException)
            {
                _lineItemsSplitPending = show;
            }
        }

        private void TryApplyLineItemsSplitDistance()
        {
            if (!_lineItemsSplitPending || _statementRows is null || _statementRows.Panel2Collapsed || !_statementRows.IsHandleCreated)
                return;

            const int min = 80;
            var available = _statementRows.Height - _statementRows.SplitterWidth;
            if (available < min * 2)
                return;

            _statementRows.Panel1MinSize = min;
            _statementRows.Panel2MinSize = min;
            _statementRows.SplitterDistance = Math.Clamp((available * 2) / 3, min, available - min);
            _lineItemsSplitPending = false;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn FillColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.MinimumWidth = 120;
            return column;
        }

        private void LoadSelectedAccount()
        {
            var name = accountUpdater.SelectedAccount?.Name;
            lblSelectedAccount.Text = string.IsNullOrWhiteSpace(name)
                ? "Account:"
                : $"Account: {name}";
            billsControl.SelectAccount(accountUpdater.SelectedAccount?.Id);
            ledger.SelectAccount(accountUpdater.SelectedAccount?.Id);
            LoadStatementsForSelectedAccount();
            _loadedRevision = FinanceDataRevision.Current;
        }

        private void LoadStatementsForSelectedAccount()
        {
            var account = accountUpdater.SelectedAccount;
            if (account is null)
            {
                _accountStatements = [];
                _statementsSource.DataSource = new List<AccountStatementListRow>();
                ClearStatementSelection();
                btnAddStatement.Enabled = false;
                return;
            }

            var canAdd = StatementAccountMatch.ForAccount(account) is not null;
            btnAddStatement.Enabled = canAdd;

            _accountStatements = _planningOrchestrator.GetStatementStore().GetForAccount(account.Id).ToList();
            var rows = _accountStatements
                .OrderByDescending(s => s.StatementDate)
                .ThenByDescending(s => s.DueDate)
                .Select(AccountStatementListRow.From)
                .ToList();
            _suspendStatementSelection = true;
            try
            {
                _statementsSource.DataSource = rows;
                if (rows.Count > 0)
                    _statementsSource.Position = 0;
            }
            finally
            {
                _suspendStatementSelection = false;
            }

            if (!canAdd || rows.Count == 0)
            {
                ClearStatementSelection();
                return;
            }

            SelectFirstStatement();
        }

        private void SelectFirstStatement()
        {
            if (gridStatements.Rows.Count == 0)
            {
                ClearStatementSelection();
                return;
            }

            var row = gridStatements.Rows[0];
            row.Selected = true;
            var column = gridStatements.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
            if (column is not null)
            {
                try
                {
                    gridStatements.CurrentCell = row.Cells[column.Index];
                }
                catch (InvalidOperationException)
                {
                }
            }

            LoadStatementDetails();
        }

        private void ClearStatementSelection()
        {
            _statementDetailsSource.DataSource = new List<StatementChildRow>();
            _statementTransactionsSource.DataSource = new List<UnifiedTransactionView>();
            UpdateLineItemsSplit();
            UpdateTransactionSplit();
        }

        private void LoadStatementDetails()
        {
            if (gridStatements.CurrentRow?.DataBoundItem is not AccountStatementListRow row)
            {
                ClearStatementSelection();
                return;
            }

            var statement = _planningOrchestrator.GetStatement(row.Id);
            _statementDetailsSource.DataSource = statement is null
                ? new List<StatementChildRow>()
                : StatementChildRow.From(statement);
            _statementTransactionsSource.DataSource = statement is null
                ? new List<UnifiedTransactionView>()
                : StatementTransactions(statement);
            UpdateLineItemsSplit();
            UpdateTransactionSplit();
        }

        private List<UnifiedTransactionView> StatementTransactions(AccountStatement statement)
        {
            var end = statement.StatementDate.Date;
            var previous = _accountStatements
                .Where(item => item.Id != statement.Id && item.StatementDate.Date < end)
                .Select(item => item.StatementDate.Date)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();
            var start = previous == DateTime.MinValue ? DateTime.MinValue : previous.AddDays(1);
            var rangeEnd = end == DateTime.MaxValue.Date ? DateTime.MaxValue : end.AddDays(1).AddTicks(-1);
            var txs = start <= DateTime.MinValue
                ? _transactionOrchestrator.GetTransactionsForAccount(statement.AccountId)
                : _transactionOrchestrator.GetTransactionsForAccount(statement.AccountId, start, rangeEnd);
            var views = UnifiedTransactionViewBuilder.Build(
                txs.Posted,
                txs.PostedTransfers,
                userFutureSingles: [],
                userFutureTransfers: [],
                statement.AccountId,
                txs.IncomingTransferSplitPosted,
                incomingFutureSplitSources: []);
            var names = _categoryOrchestrator.GetAllCategories(includeInactive: true)
                .ToDictionary(category => category.Id, category => category.Name);
            foreach (var view in views)
            {
                if (view.CategoryId is Guid id && names.TryGetValue(id, out var name))
                    view.Category = name;
            }

            return UnifiedTransactionView.OrderForDisplay(
                views.Where(view => view.Date.Date >= start && view.Date.Date <= end)).ToList();
        }

        private void AfterImport()
        {
            accountUpdater.RefreshAccounts();
            LoadSelectedAccount();
        }

        public void RefreshAfterExternalData() => AfterImport();

        private void OnImportAccounts(object? sender, EventArgs e)
        {
            accountUpdater.ImportAccountsFromFile();
            AfterImport();
        }

        private void OnImportCategories(object? sender, EventArgs e)
        {
            categoryManager.ImportFromFile();
            AfterImport();
        }

        private void OnLinkPlaidAccounts(object? sender, EventArgs e)
        {
            using var dialog = new PlaidAccountSetupDialog();
            dialog.ShowDialog(this);
            AfterImport();
        }

        private void OnImportFromFile(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select transaction spreadsheet",
                Multiselect = true
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing transactions...", busy: true);
                var rows = _importOrchestrator.LoadTransactionsFromFiles(fileDialog.FileNames);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this, "The selected file(s) did not contain any transactions.", "Import Transactions",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _importOrchestrator.ImportTransactions(rows, AppStatus.ForImport());
                AfterImport();
                AppStatus.Set(ImportStatusText.Imported(result, "transaction", "transactions"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                MessageBox.Show(this, $"Could not parse the file(s).\n{ex.Message}", "Import Transactions",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void OnImportFromPlaid(object? sender, EventArgs e)
        {
            plaidSyncDataItem.Enabled = false;
            try
            {
                AppStatus.Set("Syncing Plaid...", busy: true);
                var orchestrator = new PlaidTransactionOrchestrator();
                var progress = new Progress<PlaidSyncProgress>(AppStatus.Report);
                var result = await orchestrator.SyncIncrementalAsync(progress: progress);
                AfterImport();
                AppStatus.Set(result.Summary);
            }
            catch (Exception ex)
            {
                AppStatus.Set("Plaid sync failed.");
                MessageBox.Show(this, $"Plaid sync failed.\n{ex.Message}", "Plaid",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                plaidSyncDataItem.Enabled = true;
            }
        }

        private void OnImportTransactionRules(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select transaction rules spreadsheet"
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing transaction rules...", busy: true);
                var rows = _ruleImportOrchestrator.LoadRulesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this,
                        "The selected file did not contain any transaction rules for known accounts.",
                        "Import Transaction Rules",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _ruleImportOrchestrator.ImportRules(rows, AppStatus.ForImport());
                AfterImport();
                AppStatus.Set(ImportStatusText.Imported(result, "transaction rule", "transaction rules"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                MessageBox.Show(this, $"Could not parse the file.\n{ex.Message}", "Import Transaction Rules",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnImportTransferRules(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select transfer rules spreadsheet"
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing transfer rules...", busy: true);
                var rows = _transferImportOrchestrator.LoadRulesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this,
                        "The selected file did not contain any transfer rules for known accounts.",
                        "Import Transfer Rules",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _transferImportOrchestrator.ImportRules(rows, AppStatus.ForImport());
                AfterImport();
                AppStatus.Set(ImportStatusText.Imported(result, "transfer rule", "transfer rules"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                MessageBox.Show(this, $"Could not parse the file.\n{ex.Message}", "Import Transfer Rules",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnAddStatement(object? sender, EventArgs e) =>
            OpenStatementEditor(existing: null, accountUpdater.SelectedAccount);

        private void OnStatementCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (gridStatements.CurrentRow?.DataBoundItem is not AccountStatementListRow row)
                return;

            var statement = _planningOrchestrator.GetStatement(row.Id);
            if (statement is null)
            {
                MessageBox.Show(this, "The selected statement was not found.", "Edit Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenStatementEditor(statement, accountUpdater.SelectedAccount);
        }

        private void OpenStatementEditor(AccountStatement? existing, Account? account)
        {
            account ??= accountUpdater.SelectedAccount;
            if (account is null)
            {
                MessageBox.Show(this, "Select an account to add a statement.", "Add Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (StatementAccountMatch.ForAccount(account) is null)
            {
                MessageBox.Show(this, $"Statements are not supported for {account.Name}.", "Add Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var dlg = new StatementEditorDialog(_planningOrchestrator, existing, account);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                LoadStatementsForSelectedAccount();
                billsControl.Reload();
                accountUpdater.RefreshAccounts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, existing is null ? "Add Statement" : "Edit Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnImportStatements(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select statement spreadsheet"
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing statements...", busy: true);
                var import = new StatementImportOrchestrator();
                var rows = import.LoadStatementsFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this,
                        "The selected file did not contain any statements for accounts that already exist.",
                        "Import Statements",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = import.ImportStatements(rows, AppStatus.ForImport());
                LoadStatementsForSelectedAccount();
                billsControl.Reload();
                accountUpdater.RefreshAccounts();
                AppStatus.Set(ImportStatusText.Imported(result, "statement", "statements"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                MessageBox.Show(this, $"Could not parse the file.\n{ex.Message}", "Import Statements",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
