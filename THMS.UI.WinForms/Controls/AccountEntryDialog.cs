using THMS.Domain.Finance.Transactions;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class AccountEntryDialog : Form
    {
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _description = new();
        private readonly ComboBox _category = new();
        private readonly bool _requireCategory;

        public decimal Amount { get; private set; }
        public DateTime Date { get; private set; }
        public string Description { get; private set; } = "";
        public Guid? CategoryId { get; private set; }

        public AccountEntryDialog(
            string title,
            string hint,
            IReadOnlyList<ExpenseCategory> categories,
            Guid? lockedCategoryId = null,
            string? defaultDescription = null)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(460, 316);

            var y = 16;
            if (!string.IsNullOrWhiteSpace(hint))
            {
                Controls.Add(new Label
                {
                    Text = hint,
                    Left = 16,
                    Top = y,
                    Width = 420,
                    Height = 40
                });
                y += 44;
            }

            Controls.Add(LabelAt("Amount", 16, y));
            _amount.Left = 140;
            _amount.Top = y;
            _amount.Width = 280;
            _amount.DecimalPlaces = 2;
            _amount.Minimum = -1_000_000;
            _amount.Maximum = 1_000_000;
            Controls.Add(_amount);
            y += 40;

            Controls.Add(LabelAt("Date", 16, y));
            _date.Left = 140;
            _date.Top = y;
            _date.Width = 280;
            _date.Format = DateTimePickerFormat.Short;
            _date.Value = DateTime.Today;
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
            _category.DataSource = items;
            _requireCategory = lockedCategoryId is Guid;
            if (lockedCategoryId is Guid locked)
            {
                _category.SelectedItem = items.FirstOrDefault(c => c.Id == locked);
                _category.Enabled = false;
            }
            else
            {
                _category.SelectedItem = items.FirstOrDefault(c => c.Id == DefaultExpenseCategories.UncategorizedId)
                    ?? items.FirstOrDefault();
            }
            Controls.Add(_category);

            var save = new ThmsButton { Text = "Save", Left = 220, Top = 244 };
            save.Click += OnSave;
            var cancel = new ThmsButton { Text = "Cancel", Left = 330, Top = 244 };
            cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(save);
            Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;
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
            CategoryId = _category.SelectedItem is ExpenseCategory category ? category.Id : null;
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
