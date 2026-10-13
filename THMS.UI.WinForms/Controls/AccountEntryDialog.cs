using THMS.Domain.Finance.Transactions;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class AccountEntryDialog : Form
    {
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _description = new();
        private readonly ComboBox _category = new();
        private readonly bool _requireCategory;
        private readonly Guid? _lockedCategoryValue;
        private readonly int _categoryIndex;

        public decimal Amount { get; private set; }
        public DateTime Date { get; private set; }
        public string Description { get; private set; } = "";
        public Guid? CategoryId { get; private set; }

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
            bool categoryIsSplit = false)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(460, 316);

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

            Controls.Add(LabelAt("Date", 16, y));
            _date.Left = 140;
            _date.Top = y;
            _date.Width = 280;
            _date.Format = DateTimePickerFormat.Short;
            _date.Value = initialDate is DateTime date && date.Year > 1 ? date.Date : DateTime.Today;
            Controls.Add(_date);
            y += 40;

            Controls.Add(LabelAt("Description", 16, y));
            _description.Left = 140;
            _description.Top = y;
            _description.Width = 280;
            _description.Text = defaultDescription ?? "";
            Controls.Add(_description);
            y += 40;

            Controls.Add(LabelAt("Category", 16, y));
            _category.Left = 140;
            _category.Top = y;
            _category.Width = 280;
            _category.DropDownStyle = ComboBoxStyle.DropDownList;
            _category.DisplayMember = nameof(ExpenseCategory.Name);
            _category.ValueMember = nameof(ExpenseCategory.Id);
            var items = categories.OrderBy(c => c.Name).ToList();
            if (categoryIsSplit)
                items.Insert(0, new ExpenseCategory { Name = UnifiedTransactionView.SplitCategory });
            _category.DataSource = items;
            if (_category.Items.Count == 0 && _category.DataSource is not null)
                _category.BindingContext ??= new BindingContext();
            _requireCategory = lockedCategoryId is Guid;
            _lockedCategoryValue = lockedCategoryId ?? (lockCategory ? initialCategoryId : null);
            _categoryIndex = CategoryIndex(items, lockedCategoryId, initialCategoryId, categoryIsSplit);
            SelectCategory();
            _category.Enabled = !lockCategory && lockedCategoryId is null;
            Controls.Add(_category);

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
            SelectCategory();
        }

        private static int CategoryIndex(
            IReadOnlyList<ExpenseCategory> items,
            Guid? lockedCategoryId,
            Guid? initialCategoryId,
            bool categoryIsSplit)
        {
            if (categoryIsSplit)
                return items.Count == 0 ? -1 : 0;

            var id = lockedCategoryId ?? initialCategoryId ?? DefaultExpenseCategories.UncategorizedId;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id)
                    return i;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Id == DefaultExpenseCategories.UncategorizedId)
                    return i;
            }

            return items.Count == 0 ? -1 : 0;
        }

        private void SelectCategory()
        {
            if (_category.Items.Count == 0 && _category.DataSource is not null)
                _category.BindingContext ??= new BindingContext();
            if (_categoryIndex >= 0 && _categoryIndex < _category.Items.Count)
                _category.SelectedIndex = _categoryIndex;
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
                ?? (_category.SelectedItem is ExpenseCategory category && category.Id != Guid.Empty
                    ? category.Id
                    : null);
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
