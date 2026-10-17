using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class AccountEntryDialog : Form
    {
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _description = new();
        private readonly ComboBox _category = new();
        private readonly bool _requireCategory;
        private readonly bool _lockCategory;
        private readonly bool _lockAmount;
        private readonly Guid? _lockedCategoryValue;
        private readonly IReadOnlyList<Account> _accounts;
        private readonly List<SplitTransactionRow> _existingSplits;
        private List<ExpenseCategory> _categories;
        private List<SplitTransactionRow> _pendingSplits = [];
        private Guid? _categoryId;
        private bool _displaySplit;
        private bool _loadingCategories;

        public decimal Amount { get; private set; }
        public DateTime Date { get; private set; }
        public string Description { get; private set; } = "";
        public Guid? CategoryId { get; private set; }
        public bool HasPendingSplits { get; private set; }
        public IReadOnlyList<SplitTransactionRow> PendingSplits => _pendingSplits;

        public AccountEntryDialog(
            string title,
            string hint,
            IReadOnlyList<ExpenseCategory> categories,
            Guid? lockedCategoryId = null,
            string? defaultDescription = null,
            decimal? initialAmount = null,
            DateTime? initialDate = null,
            Guid? initialCategoryId = null,
            bool lockAmount = false,
            bool lockCategory = false,
            bool categoryIsSplit = false,
            IReadOnlyList<Account>? accounts = null,
            IEnumerable<SplitTransactionRow>? existingSplits = null)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(460, 316);

            _categories = categories.ToList();
            _accounts = accounts ?? [];
            _existingSplits = existingSplits?.Select(split => split.Clone()).ToList() ?? [];
            _categoryId = lockedCategoryId ?? initialCategoryId;
            _displaySplit = categoryIsSplit;
            _lockCategory = lockCategory || lockedCategoryId is not null;
            _lockAmount = lockAmount;
            _requireCategory = lockedCategoryId is not null;
            _lockedCategoryValue = lockedCategoryId ?? (lockCategory ? initialCategoryId : null);

            const int margin = 16;
            const int contentWidth = 428;
            var y = margin;
            if (!string.IsNullOrWhiteSpace(hint))
            {
                var hintLabel = new Label
                {
                    Text = hint,
                    Left = margin,
                    Top = y,
                    AutoSize = true,
                    MaximumSize = new Size(contentWidth, 0)
                };
                Controls.Add(hintLabel);
                y += hintLabel.Height + 12;
            }

            Controls.Add(LabelAt("Date", 16, y));
            _date.Left = 140;
            _date.Top = y;
            _date.Width = 280;
            _date.Format = DateTimePickerFormat.Short;
            _date.Value = initialDate is DateTime date && date.Year > 1 ? date.Date : DateTime.Today;
            Controls.Add(_date);
            y += 40;

            Controls.Add(LabelAt("Amount", 16, y));
            _amount.Left = 140;
            _amount.Top = y;
            _amount.Width = 280;
            _amount.DecimalPlaces = 2;
            _amount.Minimum = -1_000_000;
            _amount.Maximum = 1_000_000;
            if (initialAmount is decimal amount)
                _amount.Value = Math.Clamp(amount, _amount.Minimum, _amount.Maximum);
            _amount.Enabled = !lockAmount;
            Controls.Add(_amount);
            y += 40;

            Controls.Add(LabelAt("Category", 16, y));
            _category.Left = 140;
            _category.Top = y;
            _category.Width = 280;
            _category.DropDownStyle = ComboBoxStyle.DropDownList;
            _category.SelectionChangeCommitted += OnCategoryCommitted;
            BindCategories();
            _category.Enabled = !_lockCategory;
            Controls.Add(_category);
            y += 40;

            Controls.Add(LabelAt("Description", 16, y));
            _description.Left = 140;
            _description.Top = y;
            _description.Width = 280;
            _description.Text = defaultDescription ?? "";
            Controls.Add(_description);

            y += 48;
            var save = new ThmsButton { Text = "Save", Top = y };
            save.Click += OnSave;
            var cancel = new ThmsButton { Text = "Cancel", Top = y };
            cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(save);
            Controls.Add(cancel);
            var cancelWidth = Math.Max(cancel.Width, cancel.PreferredSize.Width);
            var saveWidth = Math.Max(save.Width, save.PreferredSize.Width);
            cancel.Left = margin + contentWidth - cancelWidth;
            save.Left = cancel.Left - saveWidth - 12;
            ClientSize = new Size(margin + contentWidth + margin, y + save.Height + margin);
            AcceptButton = save;
            CancelButton = cancel;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BindCategories();
        }

        private void BindCategories()
        {
            var active = _categories.Where(category => category.IsActive).ToList();
            var items = CategoryTreeUi.ComboItems(active, includeSplit: !_lockCategory || _displaySplit);
            if (_categoryId is Guid selectedId && items.All(item => item.CategoryId != selectedId))
            {
                var extra = _categories.FirstOrDefault(category => category.Id == selectedId);
                if (extra is not null)
                {
                    var choice = new CategoryListItem(
                        ExpenseCategoryTree.IndentedName(_categories, extra),
                        extra.Id,
                        CategoryListAction.None);
                    var insertAt = items.FindIndex(item => item.Action == CategoryListAction.NewCategory);
                    if (insertAt >= 0)
                        items.Insert(insertAt, choice);
                    else
                        items.Add(choice);
                }
            }

            _loadingCategories = true;
            _category.DisplayMember = nameof(CategoryListItem.Label);
            _category.DataSource = items;
            var longest = items.Count == 0
                ? 0
                : items.Max(item => TextRenderer.MeasureText(item.Label, _category.Font).Width);
            _category.DropDownWidth = Math.Max(_category.Width, longest + SystemInformation.VerticalScrollBarWidth);

            var showSplit = HasPendingSplits || _displaySplit;
            CategoryListItem? match = showSplit
                ? items.FirstOrDefault(item => item.Action == CategoryListAction.Split)
                : items.FirstOrDefault(item => item.Action == CategoryListAction.None && item.CategoryId == _categoryId);
            match ??= items.FirstOrDefault(item => item.CategoryId == DefaultExpenseCategories.UncategorizedId)
                ?? items.FirstOrDefault(item => item.Action == CategoryListAction.None);
            if (match is not null)
                _category.SelectedItem = match;
            if (!showSplit && match is { Action: CategoryListAction.None, CategoryId: Guid chosen })
                _categoryId ??= chosen;
            _loadingCategories = false;
        }

        private void OnCategoryCommitted(object? sender, EventArgs e)
        {
            if (_loadingCategories || _lockCategory || _category.SelectedItem is not CategoryListItem item)
                return;

            switch (item.Action)
            {
                case CategoryListAction.Split:
                    BeginInvoke(EditSplits);
                    break;
                case CategoryListAction.NewCategory:
                    BeginInvoke(CreateCategory);
                    break;
                case CategoryListAction.ManageCategories:
                    BeginInvoke(ManageCategories);
                    break;
                default:
                    _displaySplit = false;
                    HasPendingSplits = false;
                    _pendingSplits = [];
                    _categoryId = item.CategoryId;
                    _amount.Enabled = !_lockAmount;
                    break;
            }
        }

        private void EditSplits()
        {
            var hadSplits = HasPendingSplits;
            var seed = hadSplits ? _pendingSplits : _existingSplits;
            using var editor = new SplitTransactionEditor(
                _description.Text.Trim(),
                _amount.Value,
                seed,
                _categories.Where(category => category.IsActive).ToList(),
                _accounts);
            if (editor.ShowDialog(this) != DialogResult.OK)
            {
                BindCategories();
                return;
            }

            _pendingSplits = editor.Result;
            HasPendingSplits = _pendingSplits.Count > 0;
            _displaySplit = HasPendingSplits;
            _amount.Enabled = !_lockAmount && !HasPendingSplits;
            BindCategories();
        }

        private void CreateCategory()
        {
            using var editor = new CategoryEditor(new CategoryOrchestrator());
            if (editor.ShowDialog(this) != DialogResult.OK || editor.CreatedCategory is null)
            {
                BindCategories();
                return;
            }

            _displaySplit = false;
            HasPendingSplits = false;
            _pendingSplits = [];
            _categoryId = editor.CreatedCategory.Id;
            _amount.Enabled = !_lockAmount;
            ReloadCategories();
        }

        private void ManageCategories()
        {
            using var manager = new CategoryManager(new CategoryOrchestrator());
            manager.ShowDialog(this);
            ReloadCategories();
        }

        private void ReloadCategories()
        {
            var orchestrator = new CategoryOrchestrator();
            var categories = orchestrator.GetActiveCategories().ToList();
            if (_categoryId is Guid id && categories.All(category => category.Id != id))
            {
                var current = orchestrator.GetCategory(id);
                if (current is not null)
                    categories.Add(current);
            }

            _categories = categories;
            BindCategories();
        }

        private static Label LabelAt(string text, int left, int top) =>
            new() { Text = text, Left = left, Top = top + 4, AutoSize = true };

        private void OnSave(object? sender, EventArgs e)
        {
            if (_amount.Value == 0)
            {
                MessageBox.Show(this, "Amount cannot be zero.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Amount = _amount.Value;
            Date = _date.Value.Date;
            Description = _description.Text.Trim();
            CategoryId = _lockedCategoryValue
                ?? (HasPendingSplits
                    ? _categoryId
                    : _category.SelectedItem is CategoryListItem { Action: CategoryListAction.None, CategoryId: Guid id }
                        ? id
                        : _categoryId);
            if (_requireCategory && CategoryId is null)
            {
                MessageBox.Show(this, "A category is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
