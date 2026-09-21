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
        private readonly BindingSource _statementsSource = new();
        private readonly BindingSource _statementDetailsSource = new();
        private int _loadedRevision = int.MinValue;

        public RegisterForm()
        {
            InitializeComponent();
            ledger.HostProvidesAccounts = true;
            categoryManager.Bind(new CategoryOrchestrator());
            categoryManager.CatalogChanged += (_, _) => ledger.RefreshCurrentAccount();
            ConfigureStatementGrid();
            tabsTop.RecalculateItemSize();
            tabs.RecalculateItemSize();
            lblSelectedAccount.Height = Math.Max(
                lblSelectedAccount.PreferredHeight,
                lblSelectedAccount.Font.Height + lblSelectedAccount.Padding.Vertical);
            tabsTop.SelectedIndexChanged += (_, _) =>
            {
                if (tabsTop.SelectedTab == tabCategories)
                    categoryManager.RefreshLayout();
            };
            tabs.SelectedIndexChanged += (_, _) =>
            {
                if (tabs.SelectedTab == tabBills)
                    billsControl.Reload();
                if (tabs.SelectedTab == tabLedger)
                    ledger.SelectAccount(accountUpdater.SelectedAccount?.Id);
            };
            billsControl.AddStatementClicked += OnAddStatementFromBills;
            billsControl.DataChanged += (_, _) =>
            {
                accountUpdater.RefreshAccounts();
                ledger.SelectAccount(accountUpdater.SelectedAccount?.Id);
            };
            accountUpdater.SelectedAccountChanged += (_, _) => LoadSelectedAccount();
            LoadSelectedAccount();
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
                MoneyColumn(nameof(AccountStatementListRow.Promotions), "Promotions"),
                MoneyColumn(nameof(AccountStatementListRow.Usage), "Usage"),
                MoneyColumn(nameof(AccountStatementListRow.Charges), "Charges"),
                FillColumn(nameof(AccountStatementListRow.Notes), "Notes"));
            gridStatements.DataSource = _statementsSource;
            gridStatements.SelectionChanged += (_, _) => LoadStatementDetails();
            gridStatements.CellDoubleClick += OnStatementCellDoubleClick;
            HostStatementDetails();
        }

        private void HostStatementDetails()
        {
            var splitStatements = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 80,
                Panel2MinSize = 80,
                SplitterDistance = 220,
                Name = "splitStatements"
            };
            pnlStatements.Controls.Remove(gridStatements);
            gridStatements.Dock = DockStyle.Fill;
            splitStatements.Panel1.Controls.Add(gridStatements);

            var detailsHost = new Panel { Dock = DockStyle.Fill };
            var detailsFont = new Font(Font.FontFamily, Font.Size + 3f, FontStyle.Bold);
            var labelPad = LogicalToDeviceUnits(6);
            var labelHeight = TextRenderer.MeasureText("Line items", detailsFont).Height + (labelPad * 2);
            var lblDetails = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Font = detailsFont,
                Height = Math.Max(LogicalToDeviceUnits(32), labelHeight),
                Padding = new Padding(LogicalToDeviceUnits(8), labelPad, LogicalToDeviceUnits(8), labelPad),
                Text = "Line items",
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
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
            detailsHost.Controls.Add(gridDetails);
            detailsHost.Controls.Add(lblDetails);
            splitStatements.Panel2.Controls.Add(detailsHost);
            pnlStatements.Controls.Add(splitStatements);
            splitStatements.SendToBack();
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
                _statementsSource.DataSource = new List<AccountStatementListRow>();
                _statementDetailsSource.DataSource = new List<StatementChildRow>();
                lblStatementStatus.Text = "Select an account to view statements.";
                btnAddStatement.Enabled = false;
                return;
            }

            var canAdd = StatementAccountMatch.ForAccount(account) is not null;
            btnAddStatement.Enabled = canAdd;

            var rows = _planningOrchestrator.GetStatementListRows(account.Id);
            _statementsSource.DataSource = rows;

            if (!canAdd)
            {
                lblStatementStatus.Text = $"Statements are not supported for {account.Name}.";
                return;
            }

            lblStatementStatus.Text = rows.Count == 0
                ? $"No statements for {account.Name}."
                : $"{rows.Count} statement{(rows.Count == 1 ? "" : "s")} for {account.Name}.";
            LoadStatementDetails();
        }

        private void LoadStatementDetails()
        {
            if (gridStatements.CurrentRow?.DataBoundItem is not AccountStatementListRow row)
            {
                _statementDetailsSource.DataSource = new List<StatementChildRow>();
                return;
            }

            var statement = _planningOrchestrator.GetStatement(row.Id);
            _statementDetailsSource.DataSource = statement is null
                ? new List<StatementChildRow>()
                : StatementChildRow.From(statement);
        }

        private void AfterImport()
        {
            accountUpdater.RefreshAccounts();
            LoadSelectedAccount();
        }

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
                var rows = _importOrchestrator.LoadTransactionsFromFiles(fileDialog.FileNames);
                if (rows.Count == 0)
                {
                    MessageBox.Show(this, "The selected file(s) did not contain any transactions.", "Import Transactions",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new TransactionImportPreviewDialog(rows, _importOrchestrator);
                if (preview.ShowDialog(this) != DialogResult.OK)
                    return;

                AfterImport();
                var status = ImportStatusText.Imported(preview.Result, "transaction", "transactions");
                billsControl.SetStatus(status);
                lblLedgerStatus.Text = status;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file(s).\n{ex.Message}", "Import Transactions",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnImportFromPlaid(object? sender, EventArgs e)
        {
            using var dialog = new PlaidTransactionImportDialog();
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            AfterImport();
            var status = ImportStatusText.Imported(dialog.Result, "Plaid transaction", "Plaid transactions");
            billsControl.SetStatus(status);
            lblLedgerStatus.Text = status;
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
                var rows = _ruleImportOrchestrator.LoadRulesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show(this,
                        "The selected file did not contain any transaction rules for known accounts.",
                        "Import Transaction Rules",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new RecurringRuleImportPreviewDialog(rows, _ruleImportOrchestrator);
                if (preview.ShowDialog(this) != DialogResult.OK)
                    return;

                AfterImport();
                var status = ImportStatusText.Imported(preview.Result, "transaction rule", "transaction rules");
                billsControl.SetStatus(status);
                lblLedgerStatus.Text = status;
            }
            catch (Exception ex)
            {
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
                var rows = _transferImportOrchestrator.LoadRulesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show(this,
                        "The selected file did not contain any transfer rules for known accounts.",
                        "Import Transfer Rules",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new RecurringTransferImportPreviewDialog(rows, _transferImportOrchestrator);
                if (preview.ShowDialog(this) != DialogResult.OK)
                    return;

                AfterImport();
                var status = ImportStatusText.Imported(preview.Result, "transfer rule", "transfer rules");
                billsControl.SetStatus(status);
                lblLedgerStatus.Text = status;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file.\n{ex.Message}", "Import Transfer Rules",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnAddStatement(object? sender, EventArgs e) =>
            OpenStatementEditor(existing: null, accountUpdater.SelectedAccount);

        private void OnAddStatementFromBills(object? sender, Guid? accountId)
        {
            var account = accountId is Guid id
                ? _accountOrchestrator.GetAccount(id) ?? accountUpdater.SelectedAccount
                : accountUpdater.SelectedAccount;
            OpenStatementEditor(existing: null, account);
        }

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
                var import = new StatementImportOrchestrator();
                var rows = import.LoadStatementsFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show(this,
                        "The selected file did not contain any statements for accounts that already exist.",
                        "Import Statements",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new StatementImportPreviewDialog(rows, import);
                if (preview.ShowDialog(this) != DialogResult.OK)
                    return;

                LoadStatementsForSelectedAccount();
                billsControl.Reload();
                accountUpdater.RefreshAccounts();
                lblStatementStatus.Text = ImportStatusText.Imported(preview.Result, "statement", "statements");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file.\n{ex.Message}", "Import Statements",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
