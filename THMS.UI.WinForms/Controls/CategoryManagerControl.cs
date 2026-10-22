using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Aggregation;
using THMS.Logic.Finance.Transactions;
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
        private List<UnifiedTransactionView> _categoryTransactions = [];
        private CategoryBudgetPeriodRow? _selectedPeriod;
        private Guid? _selectedCategoryId;
        private bool _loading;
        private int _reloadGeneration;

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
            SplitContainerUtil.MakeSplitterVisible(splitMain);
            SplitContainerUtil.MakeSplitterVisible(splitDetail);
            ConfigureGrids();
        }

        public void Bind(CategoryOrchestrator orchestrator, bool reload = true)
        {
            ArgumentNullException.ThrowIfNull(orchestrator);
            _orchestrator = orchestrator;
            if (reload)
                Reload(_selectedCategoryId);
        }

        public void ReloadIfClean()
        {
            if (_orchestrator is null)
                return;
            var generation = Interlocked.Increment(ref _reloadGeneration);
            var selectId = _selectedCategoryId;
            Reload(selectId, refreshBudgets: false);
            _ = ReloadCleanAsync(generation, selectId);
        }

        private async Task ReloadCleanAsync(int generation, Guid? selectId)
        {
            try
            {
                await Task.Run(() => _budgets.RefreshAllActive());
            }
            catch (Exception ex)
            {
                if (generation == _reloadGeneration && !IsDisposed && IsHandleCreated)
                    BeginInvoke(() => ExceptionDialog.Show(FindForm(), "Categories", ex));
                return;
            }

            if (generation != _reloadGeneration || IsDisposed)
                return;
            if (!IsHandleCreated)
                return;
            if (InvokeRequired)
                BeginInvoke(Apply);
            else
                Apply();

            void Apply()
            {
                if (generation != _reloadGeneration || IsDisposed)
                    return;
                Reload(selectId, refreshBudgets: false);
            }
        }

        public void RefreshLayout()
        {
            PerformLayout();
            ApplySplitLayout();
        }

        public void SetHistoryPeriod(string period)
        {
            _ = period;
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
            ApplySplit(splitMain, leadingParts: 3, trailingParts: 2, panelMin: 80);
            ApplySplit(splitDetail, leadingParts: 1, trailingParts: 1, panelMin: 160);
        }

        private static void ApplySplit(SplitContainer split, int leadingParts, int trailingParts, int panelMin)
        {
            var span = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
            var available = span - split.SplitterWidth;
            if (available < panelMin * 2)
                return;

            split.Panel1MinSize = panelMin;
            split.Panel2MinSize = panelMin;
            var leading = available * leadingParts / (leadingParts + trailingParts);
            split.SplitterDistance = Math.Clamp(leading, panelMin, available - panelMin);
        }

        private CategoryOrchestrator Orchestrator =>
            _orchestrator ??= new CategoryOrchestrator();

        private void ConfigureGrids()
        {
            DataGridViewUtil.EnableDoubleBuffering(gridCategories);
            DataGridViewUtil.EnableDoubleBuffering(gridPeriods);
            gridCategories.Columns.AddRange(
                TextColumn(nameof(CategoryBudgetRow.Name), "Category", DataGridViewAutoSizeColumnMode.Fill, 180),
                TextColumn(nameof(CategoryBudgetRow.Role), "Role", DataGridViewAutoSizeColumnMode.AllCells, 90),
                TextColumn(nameof(CategoryBudgetRow.Active), "Active", DataGridViewAutoSizeColumnMode.AllCells, 70),
                CurrencyColumn(nameof(CategoryBudgetRow.Remaining), "Remaining"),
                DateColumn(nameof(CategoryBudgetRow.PeriodStart), "Start"),
                DateColumn(nameof(CategoryBudgetRow.PeriodEnd), "End"),
                CurrencyColumn(nameof(CategoryBudgetRow.Recommended), "Recommended"),
                TextColumn(nameof(CategoryBudgetRow.Status), "Status", DataGridViewAutoSizeColumnMode.AllCells, 120));
            gridCategories.DataSource = _categoriesSource;
            gridCategories.SelectionChanged += (_, _) => OnCategorySelectionChanged();
            gridCategories.CellDoubleClick += OnCategoryDoubleClick;
            gridCategories.CellFormatting += OnCategoryCellFormatting;
            gridCategories.MouseDown += OnCategoryGridMouseDown;

            gridPeriods.Columns.AddRange(
                TextColumn(nameof(CategoryBudgetPeriodRow.Period), "Period", DataGridViewAutoSizeColumnMode.AllCells, 160),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Starting), "Starting"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.BudgetAmount), "Budget"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Actual), "Actual"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Remaining), "Remaining"),
                CurrencyColumn(nameof(CategoryBudgetPeriodRow.Recommended), "Recommended"),
                TextColumn(nameof(CategoryBudgetPeriodRow.Status), "Status", DataGridViewAutoSizeColumnMode.Fill, 80));
            gridPeriods.DataSource = _periodsSource;
            gridPeriods.SelectionChanged += (_, _) => OnPeriodSelectionChanged();
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
            gridTransactions.MouseDown += OnTransactionGridMouseDown;
            gridTransactions.KeyDown += OnTransactionKeyDown;
        }

        private void Reload(Guid? selectId = null, bool refreshBudgets = true)
        {
            if (refreshBudgets)
                _budgets.RefreshAllActive();
            var selected = selectId ?? _selectedCategoryId;
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
            try
            {
                _categoriesSource.DataSource = rows;
                SelectCategory(selected);
                if (SelectedCategory() is CategoryBudgetRow shown)
                    _selectedCategoryId = shown.CategoryId;
            }
            finally
            {
                _loading = false;
            }

            LoadDetails();
            UpdateBudgetButton();
        }

        private void SelectCategory(Guid? categoryId)
        {
            DataGridViewRow? match = null;
            if (categoryId is Guid id)
            {
                foreach (DataGridViewRow row in gridCategories.Rows)
                {
                    if (row.DataBoundItem is CategoryBudgetRow bound && bound.CategoryId == id)
                    {
                        match = row;
                        break;
                    }
                }
            }

            if (match is null && gridCategories.Rows.Count > 0)
                match = gridCategories.Rows[0];
            if (match is null)
                return;

            DataGridViewUtil.SelectOnlyCell(gridCategories, match.Index, 0);
        }

        private CategoryBudgetRow? SelectedCategory() =>
            gridCategories.CurrentRow?.DataBoundItem as CategoryBudgetRow;

        private void OnCategoryGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = gridCategories.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;

            if (!DataGridViewUtil.CellIsSelected(gridCategories, hit.RowIndex, hit.ColumnIndex)
                && !DataGridViewUtil.SelectOnlyCell(gridCategories, hit.RowIndex, hit.ColumnIndex))
                return;

            var rowIndex = hit.RowIndex;
            var location = e.Location;
            gridCategories.BeginInvoke(() => ShowCategoryMenu(rowIndex, location));
        }

        private void ShowCategoryMenu(int rowIndex, Point location)
        {
            if (gridCategories.IsDisposed || rowIndex < 0 || rowIndex >= gridCategories.RowCount)
                return;
            if (gridCategories.Rows[rowIndex].DataBoundItem is not CategoryBudgetRow row)
                return;
            if (row.CategoryId == DefaultExpenseCategories.UncategorizedId)
                return;

            var menu = new ContextMenuStrip();
            var delete = new ToolStripMenuItem("Delete");
            delete.Click += (_, _) => DeleteSelectedCategory(row);
            menu.Items.Add(delete);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(gridCategories, location);
        }

        private void DeleteSelectedCategory(CategoryBudgetRow row)
        {
            if (row.CategoryId == DefaultExpenseCategories.UncategorizedId)
                return;

            var category = Orchestrator.GetCategory(row.CategoryId);
            if (category is null)
                return;

            var usage = Orchestrator.GetUsage(category.Id);
            var childCount = Orchestrator.GetAllCategories()
                .Count(candidate => candidate.ParentCategoryId == category.Id);
            var notes = new List<string>();
            if (usage.TransactionCount > 0)
            {
                notes.Add(usage.TransactionCount == 1
                    ? "1 transaction uses it and will be changed to Uncategorized."
                    : $"{usage.TransactionCount} transactions use it and will be changed to Uncategorized.");
            }

            if (usage.RecurringRuleCount > 0)
            {
                notes.Add(usage.RecurringRuleCount == 1
                    ? "1 recurring rule uses it and will be changed to Uncategorized."
                    : $"{usage.RecurringRuleCount} recurring rules use it and will be changed to Uncategorized.");
            }

            if (childCount > 0)
            {
                notes.Add(childCount == 1
                    ? "Its child category will move up one level."
                    : "Its child categories will move up one level.");
            }

            var message = notes.Count == 0
                ? $"Delete '{category.Name}'?"
                : $"Delete '{category.Name}'? {string.Join(" ", notes)}";
            if (MessageBox.Show(
                    FindForm(),
                    message,
                    "Delete Category",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var parentId = category.ParentCategoryId;
                Orchestrator.DeleteCategory(category.Id);
                NoteCatalogChanged();
                _selectedCategoryId = parentId;
                Reload(parentId);
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(FindForm(), "Delete Category", ex);
            }
        }

        private CategoryBudgetPeriodRow? SelectedPeriod() =>
            gridPeriods.CurrentRow?.DataBoundItem as CategoryBudgetPeriodRow;

        private void OnCategorySelectionChanged()
        {
            if (_loading)
                return;
            if (Visible)
                _selectedCategoryId = SelectedCategory()?.CategoryId;
            LoadDetails();
            UpdateBudgetButton();
        }

        private void UpdateBudgetButton()
        {
            var row = SelectedCategory();
            btnAddBudget.Enabled = row is not null;
            btnAddBudget.Text = row?.HasBudget == true ? "Edit Budget" : "Add Budget";
        }

        private void LoadDetails() => LoadPeriods();

        private void OnPeriodSelectionChanged()
        {
            if (_loading)
                return;
            _selectedPeriod = gridPeriods.CurrentRow?.DataBoundItem as CategoryBudgetPeriodRow;
            LoadTransactions();
        }

        private void LoadPeriods()
        {
            var row = SelectedCategory();
            var previous = _selectedPeriod;
            if (row is null)
            {
                _categoryTransactions = [];
                BindPeriods([], previous);
                lblPeriodHint.Text = "Select a category.";
                return;
            }

            _categoryTransactions = CategoryTransactions(row);
            var periods = row.HasBudget
                ? CategoryBudgetComposer.BuildPeriods(_budgets.GetHistory(row.BudgetRuleId!.Value))
                : [];
            CategoryBudgetComposer.AppendHistoryPeriod(periods, _categoryTransactions.Select(t => t.Date));
            BindPeriods(periods, previous);

            var name = CategoryName(row);
            if (!row.HasBudget)
            {
                lblPeriodHint.Text = $"The category '{name}' has no budget. Use Add Budget to track periods.";
                return;
            }

            var current = _budgets.GetActivePeriod(row.BudgetRuleId!.Value);
            lblPeriodHint.Text = current is null
                ? $"Budget periods for the category '{name}'."
                : $"Current period {current.PeriodStart:d} – {current.PeriodEnd:d} for the category '{name}'. Remaining counts spend in this window, including child categories.";
        }

        private void BindPeriods(List<CategoryBudgetPeriodRow> periods, CategoryBudgetPeriodRow? previous)
        {
            _selectedPeriod = ChoosePeriod(periods, previous);
            _loading = true;
            try
            {
                _periodsSource.DataSource = periods;
                SelectPeriodRow(_selectedPeriod);
            }
            finally
            {
                _loading = false;
            }

            LoadTransactions();
        }

        private static CategoryBudgetPeriodRow? ChoosePeriod(
            IReadOnlyList<CategoryBudgetPeriodRow> periods,
            CategoryBudgetPeriodRow? previous)
        {
            if (periods.Count == 0)
                return null;
            if (previous?.IsHistory == true)
                return periods.FirstOrDefault(p => p.IsHistory) ?? periods[0];
            if (previous is not null)
            {
                var match = periods.FirstOrDefault(p => !p.IsHistory && p.HistoryId == previous.HistoryId);
                if (match is not null)
                    return match;
            }

            return periods[0];
        }

        private void SelectPeriodRow(CategoryBudgetPeriodRow? period)
        {
            if (period is null)
                return;

            foreach (DataGridViewRow row in gridPeriods.Rows)
            {
                if (!ReferenceEquals(row.DataBoundItem, period))
                    continue;

                DataGridViewUtil.SelectOnlyCell(gridPeriods, row.Index, 0);
                return;
            }
        }

        private List<UnifiedTransactionView> CategoryTransactions(CategoryBudgetRow row)
        {
            var activity = _transactions.GetPostedActivity(DateTime.MinValue, DateTime.MaxValue);
            var views = UnifiedTransactionViewBuilder.BuildCategoryRows(activity.Posted, activity.Transfers);
            ApplyDisplayNames(views);
            var filter = row.CategoryId == DefaultExpenseCategories.UncategorizedId
                ? CategoryFilterChoice.Uncategorized
                : new CategoryFilterChoice(row.Name, row.CategoryId);
            return UnifiedTransactionViewBuilder.FilterCategoryRows(
                views,
                filter,
                Orchestrator.GetAllCategories(includeInactive: true));
        }

        private void LoadTransactions()
        {
            var period = _selectedPeriod;
            if (period is null)
            {
                lblTransactionHint.Text = "";
                _transactionsSource.DataSource = new List<UnifiedTransactionView>();
                return;
            }

            lblTransactionHint.Text = TransactionHint(period, HasDefinedPeriods(), SelectedCategory());
            var filtered = _categoryTransactions.Where(t => period.Contains(t.Date));
            _transactionsSource.DataSource = UnifiedTransactionView.OrderForDisplay(filtered).ToList();
        }

        private bool HasDefinedPeriods() =>
            _periodsSource.List.OfType<CategoryBudgetPeriodRow>().Any(period => !period.IsHistory);

        private static string CategoryName(CategoryBudgetRow? row) =>
            row?.Name.Trim() ?? "";

        private static string TransactionHint(
            CategoryBudgetPeriodRow period,
            bool hasDefinedPeriods,
            CategoryBudgetRow? category)
        {
            var name = CategoryName(category);
            if (!period.IsHistory)
                return $"Transactions from {period.Period} for the category '{name}'.";

            if (!hasDefinedPeriods)
                return $"Posted activity for the category '{name}' and its children.";

            if (period.PeriodStart is null)
                return $"Transactions before the first budget period for the category '{name}'.";

            return $"Transactions before the first budget period ({period.Period}) for the category '{name}'.";
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
            if (SelectedPeriod() is not CategoryBudgetPeriodRow period || period.IsHistory)
                return;
            OpenPeriod(period.BudgetRuleId, period.HistoryId);
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
                AppStatus.Set("Importing categories...", busy: true);
                var import = new CategoryImportOrchestrator();
                var rows = import.LoadCategoriesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(FindForm(), "The selected file did not contain any categories.", "Import Categories",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = import.ImportCategories(rows, AppStatus.ForImport());
                NoteCatalogChanged();
                Reload(_selectedCategoryId);
                AppStatus.Set(ImportStatusText.Imported(result, "category", "categories"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                ExceptionDialog.Show(FindForm(), "Import Categories", ex, "Could not parse the file.");
            }
        }

        private void OnClose(object? sender, EventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);

        private UnifiedTransactionView? SelectedTransaction() =>
            gridTransactions.CurrentRow?.DataBoundItem as UnifiedTransactionView;

        private void OnTransactionGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = gridTransactions.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;

            if (!DataGridViewUtil.CellIsSelected(gridTransactions, hit.RowIndex, hit.ColumnIndex)
                && !DataGridViewUtil.SelectOnlyCell(gridTransactions, hit.RowIndex, hit.ColumnIndex))
                return;

            var rowIndex = hit.RowIndex;
            var location = e.Location;
            gridTransactions.BeginInvoke(() => ShowTransactionMenu(rowIndex, location));
        }

        private void ShowTransactionMenu(int rowIndex, Point location)
        {
            if (gridTransactions.IsDisposed || rowIndex < 0 || rowIndex >= gridTransactions.RowCount)
                return;
            if (gridTransactions.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view)
                return;

            var selected = DataGridViewUtil.SelectedDataRows(gridTransactions)
                .Select(row => row.DataBoundItem)
                .OfType<UnifiedTransactionView>()
                .ToList();
            if (selected.All(row => row.Id != view.Id))
                selected.Add(view);
            var targets = selected
                .Where(CanAssignTransactionCategory)
                .DistinctBy(row => (row.LookupId, row.SplitRowId))
                .ToList();

            var menu = new ContextMenuStrip();
            if (targets.Count > 0)
            {
                Guid? shared = targets.Select(row => row.CategoryId).Distinct().Count() == 1
                    ? targets[0].CategoryId
                    : null;
                menu.Items.Add(CategoryTreeUi.CreateSetCategoryItem(
                    Orchestrator.GetActiveCategories(),
                    targets.Count,
                    shared,
                    category => AssignTransactionCategory(targets, category.Id),
                    () => CreateAndAssignCategory(targets),
                    OpenCategoryManager));
            }

            if (CanSplit(view))
            {
                if (menu.Items.Count > 0)
                    menu.Items.Add(new ToolStripSeparator());
                var split = new ToolStripMenuItem("Split Transaction");
                split.Click += (_, _) => OpenSplitEditor(view);
                menu.Items.Add(split);
            }

            if (menu.Items.Count == 0)
            {
                menu.Dispose();
                return;
            }

            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(gridTransactions, location);
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
            if (e.Button != MouseButtons.Left)
                return;
            if ((Control.ModifierKeys & (Keys.Control | Keys.Shift)) != 0)
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
            var splitSelected = !view.IsSplitRow && view.Category == UnifiedTransactionView.SplitCategory;
            var menu = CategoryTreeUi.CreateAssignMenu(
                Orchestrator.GetActiveCategories(),
                splitSelected ? null : view.CategoryId ?? suggestion?.CategoryId,
                splitSelected ? null : suggestion?.CategoryId,
                category => AssignTransactionCategory([view], category.Id),
                () => CreateAndAssignCategory([view]),
                OpenCategoryManager,
                () => OpenSplitEditor(view),
                splitSelected);

            var cell = gridTransactions.GetCellDisplayRectangle(
                _transactionCategoryColumn.Index, rowIndex, cutOverflow: false);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(gridTransactions, new Point(cell.Left, cell.Bottom));
        }

        private static bool CanAssignTransactionCategory(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;

        private void AssignTransactionCategory(IReadOnlyList<UnifiedTransactionView> views, Guid categoryId)
        {
            foreach (var view in views)
                Orchestrator.AssignToPosted(view.LookupId, categoryId, splitRowId: view.SplitRowId);
            NoteCatalogChanged();
            Reload(_selectedCategoryId);
        }

        private void CreateAndAssignCategory(IReadOnlyList<UnifiedTransactionView> views)
        {
            using var editor = new CategoryEditor(Orchestrator);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK || editor.CreatedCategory is null)
                return;
            AssignTransactionCategory(views, editor.CreatedCategory.Id);
        }

        private void OpenCategoryManager()
        {
            if (FindForm() is CategoryManager)
                return;

            using var manager = new CategoryManager(Orchestrator);
            if (manager.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            NoteCatalogChanged();
            Reload(_selectedCategoryId);
        }

        private void OpenSplitEditor(UnifiedTransactionView view)
        {
            if (!CanSplit(view))
                return;

            var parent = _transactions.GetParent(view.LookupId);
            if (parent is null)
                return;

            var categories = Orchestrator.GetActiveCategories();
            var accounts = _accounts.GetAllAccounts().ToList();
            var existing = parent.Splits.Select(s => s.Clone()).ToList();
            if (existing.Count == 0)
            {
                var ownerId = parent is BaseSingleAccountTransaction single && single.AccountId != Guid.Empty
                    ? single.AccountId
                    : view.AccountId;
                var counterpart = _transactions.FindTransferCounterpartAccount(parent, ownerId, accounts);
                existing = [SplitTransactionSeed.Create(parent, counterpart, categories)];
            }

            using var editor = new SplitTransactionEditor(
                parent.Description ?? view.Description,
                parent.Amount,
                existing,
                categories,
                accounts);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                if (!_transactions.TryApplyAsWholeTransfer(parent.Id, editor.Result))
                    _transactions.ApplySplits(parent.Id, editor.Result);

                NoteCatalogChanged();
                Reload(_selectedCategoryId);
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(FindForm(), "Split Transaction", ex, icon: MessageBoxIcon.Information);
            }
        }

        private void OnCategoryCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (gridCategories.CurrentCell?.RowIndex == e.RowIndex)
            {
                e.CellStyle.BackColor = DataGridViewUtil.SelectionBack;
                e.CellStyle.ForeColor = DataGridViewUtil.SelectionFore;
                e.CellStyle.SelectionBackColor = DataGridViewUtil.SelectionBack;
                e.CellStyle.SelectionForeColor = DataGridViewUtil.SelectionFore;
            }

            if (gridCategories.Columns[e.ColumnIndex].DataPropertyName != nameof(CategoryBudgetRow.Remaining))
                return;
            if (gridCategories.Rows[e.RowIndex].DataBoundItem is CategoryBudgetRow row &&
                row.Role != CategoryRole.Expense.ToString())
                return;
            if (e.Value is decimal remaining && remaining < 0)
                DataGridViewUtil.SetContentForeColor(e.CellStyle, Color.Firebrick);
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

        private static DataGridViewTextBoxColumn DateColumn(string property, string header)
        {
            var column = TextColumn(property, header, DataGridViewAutoSizeColumnMode.AllCells, 90);
            column.DefaultCellStyle.Format = "d";
            column.DefaultCellStyle.NullValue = "";
            return column;
        }
    }
}
