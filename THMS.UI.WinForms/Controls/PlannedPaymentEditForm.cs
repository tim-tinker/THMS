using THMS.Domain.Finance.Accounts;

namespace THMS.UI.WinForms.Controls
{
    public class PlannedPaymentEditForm : Form
    {
        private readonly ComboBox _accounts = new();
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _note = new();

        public Guid AccountId { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime Date { get; private set; }
        public string Note { get; private set; } = "";

        public PlannedPaymentEditForm(IReadOnlyList<Account> accounts)
        {
            Text = "Add Planned Payment";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(420, 220);
            ShowInTaskbar = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _accounts.Dock = DockStyle.Fill;
            _accounts.DropDownStyle = ComboBoxStyle.DropDownList;
            _accounts.DisplayMember = nameof(Account.Name);
            _accounts.ValueMember = nameof(Account.Id);
            _accounts.DataSource = accounts.ToList();

            _amount.Dock = DockStyle.Fill;
            _amount.DecimalPlaces = 2;
            _amount.Maximum = 1_000_000;
            _amount.Minimum = 0.01m;
            _amount.Value = 25;

            _date.Dock = DockStyle.Fill;
            _date.Format = DateTimePickerFormat.Short;
            _date.Value = DateTime.Today.AddDays(7);

            _note.Dock = DockStyle.Fill;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var ok = new Button { Text = "Add", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            ok.Click += (_, _) =>
            {
                if (_accounts.SelectedItem is not Account account)
                {
                    DialogResult = DialogResult.None;
                    return;
                }

                AccountId = account.Id;
                Amount = _amount.Value;
                Date = _date.Value.Date;
                Note = string.IsNullOrWhiteSpace(_note.Text) ? "Planned payment" : _note.Text.Trim();
            };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);

            layout.Controls.Add(new Label { Text = "Account", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            layout.Controls.Add(_accounts, 1, 0);
            layout.Controls.Add(new Label { Text = "Amount", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            layout.Controls.Add(_amount, 1, 1);
            layout.Controls.Add(new Label { Text = "Date", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            layout.Controls.Add(_date, 1, 2);
            layout.Controls.Add(new Label { Text = "Notes", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
            layout.Controls.Add(_note, 1, 3);
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 2);
            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
