using THMS.Domain.Finance.Accounts;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class AccountCounterpartyDialog : Form
    {
        private readonly ComboBox _counterparty = new();
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _description = new();

        public Guid CounterpartyAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime Date { get; private set; }
        public string Description { get; private set; } = "";

        public AccountCounterpartyDialog(
            string title,
            string counterpartyLabel,
            IReadOnlyList<Account> counterparties,
            Guid? preferredCounterpartyId = null,
            decimal? defaultAmount = null,
            DateTime? defaultDate = null,
            string? defaultDescription = null)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(460, 276);

            var y = 16;
            Controls.Add(LabelAt(counterpartyLabel, 16, y));
            _counterparty.Left = 140;
            _counterparty.Top = y;
            _counterparty.Width = 280;
            _counterparty.DropDownStyle = ComboBoxStyle.DropDownList;
            _counterparty.DisplayMember = nameof(Account.Name);
            _counterparty.ValueMember = nameof(Account.Id);
            var items = counterparties.ToList();
            _counterparty.DataSource = items;
            if (preferredCounterpartyId is Guid id)
                _counterparty.SelectedItem = items.FirstOrDefault(a => a.Id == id) ?? items.FirstOrDefault();
            Controls.Add(_counterparty);
            y += 40;

            Controls.Add(LabelAt("Amount", 16, y));
            _amount.Left = 140;
            _amount.Top = y;
            _amount.Width = 280;
            _amount.DecimalPlaces = 2;
            _amount.Minimum = 0.01m;
            _amount.Maximum = 1_000_000;
            if (defaultAmount is decimal amount && amount >= _amount.Minimum)
                _amount.Value = Math.Min(amount, _amount.Maximum);
            Controls.Add(_amount);
            y += 40;

            Controls.Add(LabelAt("Date", 16, y));
            _date.Left = 140;
            _date.Top = y;
            _date.Width = 280;
            _date.Format = DateTimePickerFormat.Short;
            _date.Value = defaultDate is DateTime date && date.Year > 1 ? date.Date : DateTime.Today;
            Controls.Add(_date);
            y += 40;

            Controls.Add(LabelAt("Description", 16, y));
            _description.Left = 140;
            _description.Top = y;
            _description.Width = 280;
            _description.Text = defaultDescription ?? "";
            Controls.Add(_description);

            var save = new ThmsButton { Text = "Save", Left = 220, Top = 204 };
            save.Click += OnSave;
            var cancel = new ThmsButton { Text = "Cancel", Left = 330, Top = 204 };
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
            if (_counterparty.SelectedItem is not Account counterparty)
            {
                MessageBox.Show(this, "Select an account.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CounterpartyAccountId = counterparty.Id;
            Amount = _amount.Value;
            Date = _date.Value.Date;
            Description = _description.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
