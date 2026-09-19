using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;
using THMS.Logic.Finance.Model;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class RegisterForm : BaseEmbeddedForm
    {
        private readonly TransactionOrchestrator _txOrchestrator = new();
        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly CategoryOrchestrator _categoryOrchestrator = new();
        private readonly TransactionImportOrchestrator _importOrchestrator = new();
        private readonly PlanningOrchestrator _planningOrchestrator = new();
        private readonly BindingSource _transactionsSource = new();
        private readonly BindingSource _statementsSource = new();
        private readonly BindingSource _statementDetailsSource = new();
        private int _loadedRevision = int.MinValue;

        public RegisterForm()
        {
            InitializeComponent();
            categoryManager.Bind(_categoryOrchestrator);
            categoryManager.CatalogChanged += (_, _) => LoadTransactionsForSelectedAccount();
            ConfigureTransactionGrid();
            ConfigureStatementGrid();
            tabsTop.RecalculateItemSize();
            tabs.RecalculateItemSize();
            tabsTop.SelectedIndexChanged += (_, _) =>
            {
                if (tabsTop.SelectedTab == tabCategories)
                    categoryManager.RefreshLayout();
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
                LoadStatementsForSelectedAccount();
                return;
            }

            accountUpdater.RefreshAccounts();
        }

        private void ConfigureTransactionGrid()
        {
            DataGridViewUtil.EnableDoubleBuffering(gridTransactions);
            gridTransactions.DataSource = _transactionsSource;
            gridTransactions.SelectionChanged += (_, _) => UpdateSplitButton();
            gridTransactions.CellDoubleClick += OnTransactionCellDoubleClick;
            gridTransactions.CellMouseClick += OnCategoryCellMouseClick;
            gridTransactions.KeyDown += OnTransactionGridKeyDown;
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
            var split = new SplitContainer
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
            split.Panel1.Controls.Add(gridStatements);

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
            split.Panel2.Controls.Add(detailsHost);
            pnlStatements.Controls.Add(split);
            split.SendToBack();
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
            LoadTransactionsForSelectedAccount();
            LoadStatementsForSelectedAccount();
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
                btnImportStatements.Enabled = false;
                return;
            }

            var canAdd = StatementAccountMatch.ForAccount(account) is not null;
            btnAddStatement.Enabled = canAdd;
            btnImportStatements.Enabled = canAdd;

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

        private void LoadTransactionsForSelectedAccount()
        {
            var account = accountUpdater.SelectedAccount;
            if (account is null)
            {
                _transactionsSource.DataSource = new List<UnifiedTransactionView>();
                lblTxStatus.Text = "Select an account to view posted transactions.";
                btnImport.Enabled = false;
                btnImportPlaid.Enabled = false;
                UpdateSplitButton();
                _loadedRevision = FinanceDataRevision.Current;
                return;
            }

            btnImport.Enabled = true;
            btnImportPlaid.Enabled = true;

            var txs = _txOrchestrator.GetTransactionsForAccount(account.Id);
            var views = UnifiedTransactionViewBuilder.Build(
                txs.Posted,
                txs.PostedTransfers,
                forAccountId: account.Id,
                incomingPostedSplitSources: txs.IncomingTransferSplitPosted);
            ApplyCategoryDisplayNames(views);
            ApplyRunningBalances(views, account);
            var display = UnifiedTransactionView.OrderForDisplay(views).ToList();
            _transactionsSource.DataSource = display;
            lblTxStatus.Text = $"{display.Count} posted transaction{(display.Count == 1 ? "" : "s")} for {account.Name}.";
            UpdateSplitButton();
            _loadedRevision = FinanceDataRevision.Current;
        }

        private void ApplyRunningBalances(IEnumerable<UnifiedTransactionView> chronological, Account account)
        {
            decimal balance = PostedBalanceCalculator.GetStartingBalance(account);
            foreach (var tx in chronological)
            {
                balance += tx.Amount;
                tx.ForecastBalance = PostedBalanceCalculator.ToDisplayBalance(account, balance);
            }
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

        private void OnImportFromFile(object? sender, EventArgs e)
        {
            if (accountUpdater.SelectedAccount is null)
                return;

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

                accountUpdater.RefreshAccounts();
                LoadSelectedAccount();
                lblTxStatus.Text = ImportStatusText.Imported(preview.Result, "transaction", "transactions");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file(s).\n{ex.Message}", "Import Transactions",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnImportFromPlaid(object? sender, EventArgs e)
        {
            if (accountUpdater.SelectedAccount is null)
                return;

            using var dialog = new PlaidTransactionImportDialog();
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            accountUpdater.RefreshAccounts();
            LoadSelectedAccount();
            lblTxStatus.Text = ImportStatusText.Imported(dialog.Result, "Plaid transaction", "Plaid transactions");
        }

        private void OnAddStatement(object? sender, EventArgs e) =>
            OpenStatementEditor(existing: null);

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

            OpenStatementEditor(statement);
        }

        private void OpenStatementEditor(AccountStatement? existing)
        {
            var account = accountUpdater.SelectedAccount;
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, existing is null ? "Add Statement" : "Edit Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnImportStatements(object? sender, EventArgs e)
        {
            if (accountUpdater.SelectedAccount is null)
                return;

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
                lblStatementStatus.Text = ImportStatusText.Imported(preview.Result, "statement", "statements");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not parse the file.\n{ex.Message}", "Import Statements",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnSplitTransaction(object? sender, EventArgs e)
        {
            if (GetSelectedTransaction() is not UnifiedTransactionView view)
            {
                MessageBox.Show(this, "Select a posted transaction to split.", "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenSplitEditor(view);
        }

        private void OnTransactionCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (IsCategoryColumn(e.ColumnIndex))
            {
                ShowCategoryMenu(gridTransactions, CategoryColumn, e.RowIndex);
                return;
            }

            if (gridTransactions.Rows[e.RowIndex].DataBoundItem is UnifiedTransactionView view
                && CanSplit(view))
            {
                OpenSplitEditor(view);
            }
        }

        private void OnCategoryCellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || !IsCategoryColumn(e.ColumnIndex))
                return;
            if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
                return;
            if (!CanEditCategory(e.RowIndex))
                return;

            gridTransactions.CurrentCell = gridTransactions[e.ColumnIndex, e.RowIndex];
            ShowCategoryMenu(gridTransactions, CategoryColumn, e.RowIndex);
        }

        private void OnTransactionGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (gridTransactions.CurrentCell is { RowIndex: >= 0 } cell
                && IsCategoryColumn(cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditCategory(cell.RowIndex))
            {
                ShowCategoryMenu(gridTransactions, CategoryColumn, cell.RowIndex);
                e.Handled = true;
            }
        }

        private bool IsCategoryColumn(int columnIndex) =>
            columnIndex >= 0 && gridTransactions.Columns[columnIndex] == CategoryColumn;

        private bool CanEditCategory(int rowIndex) => CanEditCategoryRow(gridTransactions, rowIndex);

        private static bool CanEditCategoryRow(DataGridView grid, int rowIndex)
        {
            if (grid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view)
                return false;
            return CanSplit(view);
        }

        private static bool CanSplit(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;

        private void ShowCategoryMenu(DataGridView grid, DataGridViewColumn categoryColumn, int rowIndex)
        {
            if (grid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view
                || !CanEditCategoryRow(grid, rowIndex))
                return;

            var posted = view.Type == UnifiedTransactionView.PostedType
                ? _txOrchestrator.GetTransactionsForAccount(view.AccountId).Posted
                    .FirstOrDefault(t => t.Id == view.LookupId)
                : null;
            var suggestion = posted is null ? null : _categoryOrchestrator.Suggest(posted);

            var menu = new ContextMenuStrip();
            var categories = _categoryOrchestrator.GetActiveCategories();
            var currentId = view.CategoryId ?? suggestion?.CategoryId;

            foreach (var root in ExpenseCategoryTree.Roots(categories))
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
            LoadTransactionsForSelectedAccount();
        }

        private void CreateAndAssignCategory(UnifiedTransactionView view)
        {
            using var editor = new CategoryEditor(_categoryOrchestrator);
            if (editor.ShowDialog(this) != DialogResult.OK || editor.CreatedCategory is null)
                return;

            AssignCategory(view, editor.CreatedCategory.Id);
        }

        private void OpenCategoryManager()
        {
            tabsTop.SelectedTab = tabCategories;
        }

        private void OpenSplitEditor(UnifiedTransactionView view)
        {
            if (!CanSplit(view))
                return;

            var parentId = view.LookupId;
            var parent = _txOrchestrator.GetParent(parentId);
            if (parent is null)
                return;

            using var editor = new SplitTransactionEditor(
                parent.Description ?? view.Description,
                parent.Amount,
                parent.Splits.Select(s => s.Clone()).ToList(),
                _categoryOrchestrator.GetActiveCategories(),
                _accountOrchestrator.GetAllAccounts().ToList());
            if (editor.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                _txOrchestrator.ApplySplits(parentId, editor.Result);
                LoadTransactionsForSelectedAccount();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, ex.Message, "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private UnifiedTransactionView? GetSelectedTransaction() =>
            gridTransactions.CurrentRow?.DataBoundItem as UnifiedTransactionView;

        private void UpdateSplitButton()
        {
            btnSplit.Enabled = GetSelectedTransaction() is UnifiedTransactionView view && CanSplit(view);
        }
    }
}
