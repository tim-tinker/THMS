using System.ComponentModel;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Aggregation;
using THMS.Logic.Finance.Categories;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class CategoryManagerControl : UserControl
    {
        private CategoryOrchestrator? _orchestrator;
        private readonly BudgetOrchestrator _budgets = new();
        private readonly TransactionOrchestrator _transactions = new();
        private readonly AccountOrchestrator _accounts = new();
        private readonly BindingSource _categoriesSource = new();
        private readonly BindingSource _periodsSource = new();
        private readonly BindingSource _transactionsSource = new();
        private DataGridViewTextBoxColumn _transactionCategoryColumn = null!;
        private Guid? _selectedCategoryId;
        private string _historyPeriod = HistoryPeriodBar.Month;
        private bool _loading;

        public event EventHandler? CatalogChanged;
        public event EventHandler? CloseRequested;

        public bool CatalogHasChanged { get; private set; }

        public IButtonControl CloseButton => btnClose;

        [DefaultValue(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowCloseButton
        {
            get => btnClose.Visible;
            set => btnClose.Visible = value;
        }

        [DefaultValue(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool ShowImportButton
        {
            get => btnImport.Visible;
            set => btnImport.Visible = value;
        }

        public CategoryManagerControl()
        {
            InitializeComponent();
            ConfigureGrids();
            tabsDetail.RecalculateItemSize();
        }

        public void Bind(CategoryOrchestrator orchestrator)
        {
            ArgumentNullException.ThrowIfNull(orchestrator);
            _orchestrator = orchestrator;
            Reload(_selectedCategoryId);
        }

        public void ReloadIfClean()
        {
            if (_orchestrator is null)
                return;
            Reload(_selectedCategoryId);
        }

        public void RefreshLayout()
        {
            PerformLayout();
            ApplySplitLayout();
            tabsDetail.RecalculateItemSize();
        }

        public void SetHistoryPeriod(string period)
        {
            var next = string.IsNullOrWhiteSpace(period) ? HistoryPeriodBar.Month : period;
            if (string.Equals(_historyPeriod, next, StringComparison.Ordinal))
            {
                LoadTransactions();
                return;
            }

            _historyPeriod = next;
            LoadTransactions();
        }

        public void ImportFromFile() => OnImport(this, EventArgs.Empty);

        public bool TryLeave() => true;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplySplitLayout();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
                ApplySplitLayout();
        }

        private void ApplySplitLayout()
        {
            const int panelMin = 80;
            var available = splitMain.Height - splitMain.SplitterWidth;
            if (available < panelMin * 2)
                return;

            splitMain.Panel1MinSize = panelMin;
            splitMain.Panel2MinSize = panelMin;
            splitMain.SplitterDistance = Math.Clamp(available * 3 / 5, panelMin, available - panelMin);
        }

        private CategoryOrchestrator Orchestrator =>
            _orchestrator ??= new CategoryOrchestrator();

        private void ConfigureGrids()
        {
            DataGridViewUtil.EnableDoubleBuffering(gridCategories);
            DataGridViewUtil.EnableDoubleBuffering(gridPeriods);
            gridCategories.Columns.AddRange(
                TextColumn(nameof(CategoryBudgetRow.Name), "Category", DataGridViewAutoSizeColumnMode.Fill, 180),
                TextColumn(nameof(CategoryBudgetRow.Active), "Active", DataGridViewAutoSizeColumnMode.AllCells, 70),
                CurrencyColumn(nameof(CategoryBudgetRow.Remaining), "Remaining"),
                CurrencyColumn(nameof(CategoryBudgetRow.Ending), "Ending"),
                CurrencyColumn(nameof(CategoryBudgetRow.Recommended), "Recommended"),
                TextColumn(nameof(CategoryBudgetRow.Frequency), "Frequency", DataGridViewAutoSizeColumnMode.AllCells, 80),
                TextColumn(nameof(CategoryBudgetRow.Status), "Status", DataGridViewAutoSizeColumnMode.AllCells, 120));
            gridCategories.DataSource = _categoriesSource;
            gridCategories.SelectionChanged += (_, _) => OnCategorySelectionChanged();
            gridCategories.CellDoubleClick += OnCategoryDoubleClick;
            gridCategories.CellFormatting += OnCategoryCellFormatting;
            gridCategories.MouseDown += OnCategoryMouseDown;

            gridPeriods.Columns.AddRange(
                TextColumn(nameof(CategoryBudgetPeriodRow.Period), "Period", DataGridViewAutoSizeColumnMode.AllCells, 160),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Starting), "Starting"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.BudgetAmount), "Budget"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Actual), "Actual"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Remaining), "Remaining"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Ending), "Ending"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Recommended), "Recommended"),
                TextColumn(nameof(CategoryBudgetPeriodRow.Status), "Status", DataGridViewAutoSizeColumnMode.Fill, 80));
            gridPeriods.DataSource = _periodsSource;
            gridPeriods.CellDoubleClick += OnPeriodDoubleClick;

            var dateColumn = TextColumn(nameof(UnifiedTransactionView.Date), "Date", DataGridViewAutoSizeColumnMode.AllCells, 90);
            dateColumn.DefaultCellStyle.Format = "d";
            var descriptionColumn = TextColumn(nameof(UnifiedTransactionView.Description), "Description", DataGridViewAutoSizeColumnMode.Fill, 180);
            _transactionCategoryColumn = TextColumn(nameof(UnifiedTransactionView.Category), "Category", DataGridViewAutoSizeColumnMode.AllCells, 120);
            DataGridViewUtil.EnableDoubleBuffering(gridTransactions);
            gridTransactions.Columns.AddRange(
                dateColumn,
                CurrencyColumn(nameof(UnifiedTransactionView.Amount), "Amount"),
                TextColumn(nameof(UnifiedTransactionView.AccountName), "Account", DataGridViewAutoSizeColumnMode.AllCells, 110),
                _transactionCategoryColumn,
                TextColumn(nameof(UnifiedTransactionView.TypeLabel), "Type", DataGridViewAutoSizeColumnMode.AllCells, 80),
                descriptionColumn);
            gridTransactions.DataSource = _transactionsSource;
            gridTransactions.CellDoubleClick += OnTransactionDoubleClick;
            gridTransactions.CellMouseClick += OnTransactionCategoryMouseClick;
            gridTransactions.KeyDown += OnTransactionKeyDown;
            gridTransactions.SelectionChanged += (_, _) => UpdateSplitButton();
            UpdateSplitButton();
        }

        private void Reload(Guid? selectId = null, bool refreshBudgets = true)
        {
            if (refreshBudgets)
                _budgets.RefreshAllActive();
            var selected = selectId ?? SelectedCategory()?.CategoryId ?? _selectedCategoryId;
            var all = Orchestrator.GetAllCategories(includeInactive: true).ToList();
            var search = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                var include = all
                    .Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Id)
                    .ToHashSet();
                foreach (var match in all.Where(c => include.Contains(c.Id)).ToList())
                {
                    var current = match;
                    var seen = new HashSet<Guid>();
                    while (current.ParentCategoryId is Guid parentId && seen.Add(current.Id))
                    {
                        include.Add(parentId);
                        current = all.FirstOrDefault(c => c.Id == parentId);
                        if (current is null)
                            break;
                    }
                }

                all = all.Where(c => include.Contains(c.Id)).ToList();
            }

            var rules = _budgets.GetRules();
            var periods = rules.ToDictionary(r => r.Id, r => _budgets.GetActivePeriod(r.Id));
            var rows = CategoryBudgetComposer.Build(all, rules, periods, DateTime.Today);

            _loading = true;
            _categoriesSource.DataSource = rows;
            _loading = false;

            SelectCategory(selected);
            LoadDetails();
        }

        private void SelectCategory(Guid? categoryId)
        {
            if (categoryId is not Guid id)
                return;

            foreach (DataGridViewRow row in gridCategories.Rows)
            {
                if (row.DataBoundItem is CategoryBudgetRow bound && bound.CategoryId == id)
                {
                    row.Selected = true;
                    if (row.Cells.Count > 0)
                        gridCategories.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        private CategoryBudgetRow? SelectedCategory() =>
            gridCategories.CurrentRow?.DataBoundItem as CategoryBudgetRow;

        private CategoryBudgetPeriodRow? SelectedPeriod() =>
            gridPeriods.CurrentRow?.DataBoundItem as CategoryBudgetPeriodRow;

        private void OnCategorySelectionChanged()
        {
            if (_loading)
                return;
            LoadDetails();
        }

        private void LoadDetails()
        {
            LoadPeriods();
            LoadTransactions();
        }

        private void LoadPeriods()
        {
            var row = SelectedCategory();
            _selectedCategoryId = row?.CategoryId;
            if (row is null || !row.HasBudget)
            {
                _periodsSource.DataSource = new List<CategoryBudgetPeriodRow>();
                lblPeriodHint.Text = row is null
                    ? "Select a category."
                    : "This category has no budget. Use Add Budget to track periods.";
                return;
            }

            lblPeriodHint.Text = "Budget periods for the selected category.";
            _periodsSource.DataSource = CategoryBudgetComposer.BuildPeriods(_budgets.GetHistory(row.BudgetRuleId!.Value));
        }

        private void LoadTransactions()
        {
            var row = SelectedCategory();
            if (row is null)
            {
                _transactionsSource.DataSource = new List<UnifiedTransactionView>();
                UpdateSplitButton();
                return;
            }

            var start = BaseOrchestrator.GetStartDate(DateTime.Today, _historyPeriod);
            var activity = _transactions.GetPostedActivity(start, DateTime.Today);
            var views = UnifiedTransactionViewBuilder.BuildCategoryRows(activity.Posted, activity.Transfers);
            ApplyDisplayNames(views);
            var filter = row.CategoryId == DefaultExpenseCategories.UncategorizedId
                ? CategoryFilterChoice.Uncategorized
                : new CategoryFilterChoice(row.Name, row.CategoryId);
            var filtered = UnifiedTransactionViewBuilder.FilterCategoryRows(
                views,
                filter,
                Orchestrator.GetAllCategories(includeInactive: true));
            _transactionsSource.DataSource = UnifiedTransactionView.OrderForDisplay(filtered).ToList();
            UpdateSplitButton();
        }

        private void ApplyDisplayNames(IEnumerable<UnifiedTransactionView> views)
        {
            var categoryNames = Orchestrator.GetAllCategories(includeInactive: true)
                .ToDictionary(c => c.Id, c => c.Name);
            var accountNames = _accounts.GetAllAccounts().ToDictionary(a => a.Id, a => a.Name);
            foreach (var view in views)
            {
                if (view.CategoryId is Guid id && categoryNames.TryGetValue(id, out var categoryName))
                    view.Category = categoryName;
                if (accountNames.TryGetValue(view.AccountId, out var accountName))
                    view.AccountName = accountName;
            }
        }

        private void OnSearchChanged(object? sender, EventArgs e) =>
            Reload(_selectedCategoryId, refreshBudgets: false);

        private void OnCategoryDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (SelectedCategory() is not CategoryBudgetRow row)
                return;

            if (row.HasBudget)
                OpenPeriod(row.BudgetRuleId!.Value);
            else
                OpenCategoryEditor(row.CategoryId);
        }

        private void OnPeriodDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (SelectedPeriod() is not CategoryBudgetPeriodRow period)
                return;
            OpenPeriod(period.BudgetRuleId, period.HistoryId);
        }

        private void OnCategoryMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var hit = gridCategories.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;
            gridCategories.ClearSelection();
            gridCategories.Rows[hit.RowIndex].Selected = true;
            gridCategories.CurrentCell = gridCategories.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
            ShowCategoryMenu(gridCategories.PointToScreen(e.Location));
        }

        private void ShowCategoryMenu(Point screen)
        {
            if (SelectedCategory() is not CategoryBudgetRow row)
                return;

            using var menu = new ContextMenuStrip();
            menu.Items.Add("Edit Category", null, (_, _) => OpenCategoryEditor(row.CategoryId));
            menu.Items.Add(row.HasBudget ? "Edit Budget" : "Add Budget", null, (_, _) => OpenBudgetEditor(row));
            if (row.HasBudget)
                menu.Items.Add("Open Current Period", null, (_, _) => OpenPeriod(row.BudgetRuleId!.Value));
            menu.Show(screen);
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            using var editor = new CategoryEditor(Orchestrator, SelectedCategory()?.CategoryId);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK || editor.CreatedCategory is null)
                return;

            NoteCatalogChanged();
            Reload(editor.CreatedCategory.Id);
        }

        private void OnMerge(object? sender, EventArgs e)
        {
            using var dialog = new CategoryMergeDialog(Orchestrator, SelectedCategory()?.CategoryId);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            NoteCatalogChanged();
            Reload(dialog.KeepCategoryId);
        }

        private void OnAddBudget(object? sender, EventArgs e)
        {
            if (SelectedCategory() is not CategoryBudgetRow row)
            {
                MessageBox.Show(this, "Select a category first.", "Add Budget",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenBudgetEditor(row);
        }

        private void OpenCategoryEditor(Guid categoryId)
        {
            var category = Orchestrator.GetCategory(categoryId);
            if (category is null)
                return;

            using var editor = new CategoryEditor(Orchestrator, category);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            NoteCatalogChanged();
            Reload(editor.CreatedCategory?.Id ?? categoryId);
        }

        private void OpenBudgetEditor(CategoryBudgetRow row)
        {
            BudgetRuleEditor editor;
            if (row.HasBudget)
            {
                var rule = _budgets.GetRule(row.BudgetRuleId!.Value);
                if (rule is null)
                    return;
                editor = new BudgetRuleEditor(_budgets, rule);
            }
            else
            {
                var category = Orchestrator.GetCategory(row.CategoryId);
                if (category is null)
                    return;
                editor = new BudgetRuleEditor(_budgets, category);
            }

            using (editor)
            {
                if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                    return;
            }

            NoteCatalogChanged();
            Reload(row.CategoryId);
        }

        private void OpenPeriod(Guid ruleId, Guid? historyId = null)
        {
            using var editor = new BudgetPeriodEditor(_budgets, ruleId, historyId);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            NoteCatalogChanged();
            Reload(_selectedCategoryId);
        }

        private void OnImport(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select category spreadsheet"
            };
            if (fileDialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                var import = new CategoryImportOrchestrator();
                var rows = import.LoadCategoriesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    MessageBox.Show(FindForm(), "The selected file did not contain any categories.", "Import Categories",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var preview = new CategoryImportPreviewDialog(rows, import);
                if (preview.ShowDialog(FindForm()) != DialogResult.OK)
                    return;

                NoteCatalogChanged();
                Reload(_selectedCategoryId);
                SetStatus(ImportStatusText.Imported(preview.Result, "category", "categories"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), $"Could not parse the file.\n{ex.Message}", "Import Categories",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnClose(object? sender, EventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);

        private UnifiedTransactionView? SelectedTransaction() =>
            gridTransactions.CurrentRow?.DataBoundItem as UnifiedTransactionView;

        private void UpdateSplitButton() =>
            btnSplitTransaction.Enabled = SelectedTransaction() is UnifiedTransactionView view && CanSplit(view);

        private void OnSplitTransaction(object? sender, EventArgs e)
        {
            if (SelectedTransaction() is not UnifiedTransactionView view)
            {
                MessageBox.Show(this, "Select a transaction to split.", "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenSplitEditor(view);
        }

        private void OnTransactionDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (IsTransactionCategoryColumn(e.ColumnIndex))
            {
                ShowTransactionCategoryMenu(e.RowIndex);
                return;
            }

            if (SelectedTransaction() is UnifiedTransactionView view && CanSplit(view))
                OpenSplitEditor(view);
        }

        private void OnTransactionCategoryMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || !IsTransactionCategoryColumn(e.ColumnIndex))
                return;
            if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
                return;
            if (!CanEditTransactionCategory(e.RowIndex))
                return;

            gridTransactions.CurrentCell = gridTransactions[e.ColumnIndex, e.RowIndex];
            ShowTransactionCategoryMenu(e.RowIndex);
        }

        private void OnTransactionKeyDown(object? sender, KeyEventArgs e)
        {
            if (gridTransactions.CurrentCell is { RowIndex: >= 0 } cell
                && IsTransactionCategoryColumn(cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditTransactionCategory(cell.RowIndex))
            {
                ShowTransactionCategoryMenu(cell.RowIndex);
                e.Handled = true;
            }
        }

        private bool IsTransactionCategoryColumn(int columnIndex) =>
            columnIndex >= 0 && gridTransactions.Columns[columnIndex] == _transactionCategoryColumn;

        private bool CanEditTransactionCategory(int rowIndex) =>
            gridTransactions.Rows[rowIndex].DataBoundItem is UnifiedTransactionView view
            && view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;

        private static bool CanSplit(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;

        private void ShowTransactionCategoryMenu(int rowIndex)
        {
            if (gridTransactions.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view
                || !CanEditTransactionCategory(rowIndex))
                return;

            var posted = view.Type == UnifiedTransactionView.PostedType
                ? _transactions.GetParent(view.LookupId) as PostedTransaction
                : null;
            var suggestion = posted is null ? null : Orchestrator.Suggest(posted);
            var menu = new ContextMenuStrip();
            var categories = Orchestrator.GetActiveCategories();
            var currentId = view.CategoryId ?? suggestion?.CategoryId;
            foreach (var root in ExpenseCategoryTree.Roots(categories))
            {
                menu.Items.Add(CategoryTreeUi.CreateMenuItem(
                    categories,
                    root,
                    currentId,
                    suggestion?.CategoryId,
                    category => AssignTransactionCategory(view, category.Id)));
            }

            menu.Items.Add(new ToolStripSeparator());
            var newItem = new ToolStripMenuItem("New Category…");
            newItem.Click += (_, _) => CreateAndAssignCategory(view);
            menu.Items.Add(newItem);

            var cell = gridTransactions.GetCellDisplayRectangle(
                _transactionCategoryColumn.Index, rowIndex, cutOverflow: false);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(gridTransactions, new Point(cell.Left, cell.Bottom));
        }

        private void AssignTransactionCategory(UnifiedTransactionView view, Guid categoryId)
        {
            Orchestrator.AssignToPosted(view.LookupId, categoryId, splitRowId: view.SplitRowId);
            NoteCatalogChanged();
            Reload(_selectedCategoryId);
        }

        private void CreateAndAssignCategory(UnifiedTransactionView view)
        {
            using var editor = new CategoryEditor(Orchestrator);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK || editor.CreatedCategory is null)
                return;
            AssignTransactionCategory(view, editor.CreatedCategory.Id);
        }

        private void OpenSplitEditor(UnifiedTransactionView view)
        {
            if (!CanSplit(view))
                return;

            var parent = _transactions.GetParent(view.LookupId);
            if (parent is null)
                return;

            using var editor = new SplitTransactionEditor(
                parent.Description ?? view.Description,
                parent.Amount,
                parent.Splits.Select(s => s.Clone()).ToList(),
                Orchestrator.GetActiveCategories(),
                _accounts.GetAllAccounts().ToList());
            if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _transactions.ApplySplits(parent.Id, editor.Result);
                NoteCatalogChanged();
                Reload(_selectedCategoryId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnCategoryCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gridCategories.Columns[e.ColumnIndex].DataPropertyName != nameof(CategoryBudgetRow.Remaining))
                return;
            if (e.Value is decimal remaining && remaining < 0)
                e.CellStyle.ForeColor = Color.Firebrick;
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
            lblStatus.Visible = !string.IsNullOrWhiteSpace(text);
        }

        private void NoteCatalogChanged()
        {
            CatalogHasChanged = true;
            CatalogChanged?.Invoke(this, EventArgs.Empty);
        }

        private static DataGridViewTextBoxColumn TextColumn(
            string property,
            string header,
            DataGridViewAutoSizeColumnMode autoSize,
            int width) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                AutoSizeMode = autoSize,
                Width = width,
                ReadOnly = true
            };

        private static DataGridViewTextBoxColumn CurrencyColumn(string property, string header)
        {
            var column = TextColumn(property, header, DataGridViewAutoSizeColumnMode.AllCells, 90);
            column.DefaultCellStyle.Format = "c2";
            column.DefaultCellStyle.NullValue = "";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }
    }
}
