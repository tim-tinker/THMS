using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
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
        private readonly BindingSource _transactionsSource = new();
        private int _loadedRevision = int.MinValue;

        public RegisterForm()
        {
            InitializeComponent();
            ConfigureTransactionGrid();
            accountUpdater.SelectedAccountChanged += (_, _) => LoadTransactionsForSelectedAccount();
            LoadTransactionsForSelectedAccount();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || Disposing)
                return;

            if (FinanceDataRevision.Current == _loadedRevision)
                return;

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

        private void LoadTransactionsForSelectedAccount()
        {
            var account = accountUpdater.SelectedAccount;
            if (account is null)
            {
                _transactionsSource.DataSource = new List<UnifiedTransactionView>();
                lblTxStatus.Text = "Select an account to view posted transactions.";
                UpdateSplitButton();
                _loadedRevision = FinanceDataRevision.Current;
                return;
            }

            var txs = _txOrchestrator.GetTransactionsForAccount(account.Id);
            var views = UnifiedTransactionViewBuilder.Build(txs.Posted, txs.PostedTransfers);
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
                LoadTransactionsForSelectedAccount();
                lblTxStatus.Text = $"Imported {preview.ImportedCount:N0} transaction{(preview.ImportedCount == 1 ? "" : "s")}.";
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

            accountUpdater.RefreshAccounts();
            LoadTransactionsForSelectedAccount();
            lblTxStatus.Text = $"Imported {dialog.ImportedCount:N0} Plaid transaction{(dialog.ImportedCount == 1 ? "" : "s")}.";
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
                ShowCategoryMenu(e.RowIndex);
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
            ShowCategoryMenu(e.RowIndex);
        }

        private void OnTransactionGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (gridTransactions.CurrentCell is { RowIndex: >= 0 } cell
                && IsCategoryColumn(cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditCategory(cell.RowIndex))
            {
                ShowCategoryMenu(cell.RowIndex);
                e.Handled = true;
            }
        }

        private bool IsCategoryColumn(int columnIndex) =>
            columnIndex >= 0 && gridTransactions.Columns[columnIndex] == CategoryColumn;

        private bool CanEditCategory(int rowIndex)
        {
            if (gridTransactions.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view)
                return false;
            return CanSplit(view);
        }

        private static bool CanSplit(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;

        private void ShowCategoryMenu(int rowIndex)
        {
            if (gridTransactions.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view
                || !CanEditCategory(rowIndex))
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

            var cell = gridTransactions.GetCellDisplayRectangle(CategoryColumn.Index, rowIndex, cutOverflow: false);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(gridTransactions, new Point(cell.Left, cell.Bottom));
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
            using var manager = new CategoryManager(_categoryOrchestrator);
            if (manager.ShowDialog(this) == DialogResult.OK)
                LoadTransactionsForSelectedAccount();
        }

        private void OpenSplitEditor(UnifiedTransactionView view)
        {
            if (!CanSplit(view))
                return;

            var parentId = view.LookupId;
            var txs = _txOrchestrator.GetTransactionsForAccount(view.AccountId);
            BaseTransaction? parent =
                (BaseTransaction?)txs.Posted.FirstOrDefault(t => t.Id == parentId) ??
                txs.PostedTransfers.FirstOrDefault(t => t.Id == parentId);
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
