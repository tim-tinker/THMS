using THMS.Domain.Finance.Accounts;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class BalanceTransferDialog : Form
    {
        private readonly TableLayoutPanel _fields = new();
        private readonly FlowLayoutPanel _buttons = new();
        private readonly ComboBox _to = new();
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _description = new();
        private readonly ThmsButton _transfer = new() { Text = "Transfer" };

        public Guid ToAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime Date { get; private set; }
        public string Description { get; private set; } = "";

        public BalanceTransferDialog(
            Account source,
            IReadOnlyList<Account> destinations,
            decimal? statementBalance)
        {
            ArgumentNullException.ThrowIfNull(source);
            Text = "Balance transfer";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(520, 520);

            var from = new Label
            {
                Text = source.Name,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            _to.DropDownStyle = ComboBoxStyle.DropDownList;
            _to.DisplayMember = nameof(PayFromChoice.Name);
            _to.ValueMember = nameof(PayFromChoice.Id);
            _to.SelectedIndexChanged += (_, _) => UpdateSaveEnabled();

            var choices = new List<PayFromChoice> { PayFromChoice.Unspecified };
            choices.AddRange(destinations.Select(PayFromChoice.From));

            _amount.DecimalPlaces = 2;
            _amount.Minimum = 0.01m;
            _amount.Maximum = 99_999_999;
            _amount.MinimumSize = new Size(0, 28);
            if (statementBalance is decimal balance && balance >= _amount.Minimum)
                _amount.Value = Math.Min(balance, _amount.Maximum);

            _date.Format = DateTimePickerFormat.Short;
            _date.Value = DateTime.Today;
            _description.Text = "Balance transfer";

            _fields.AutoSize = true;
            _fields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _fields.Dock = DockStyle.Top;
            _fields.ColumnCount = 2;
            _fields.Padding = new Padding(16, 16, 16, 8);
            _fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var hint = new Label
            {
                Text = "This card is charged, so the amount owed on it increases. Another card is paid down. A bank account receives a deposit.",
                AutoSize = true,
                MaximumSize = new Size(470, 0),
                Margin = new Padding(0, 0, 0, 8)
            };
            _fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var hintRow = _fields.RowCount++;
            _fields.Controls.Add(hint, 0, hintRow);
            _fields.SetColumnSpan(hint, 2);

            AddRow(_fields, "From card", from);
            AddRow(_fields, "To account", _to);
            if (statementBalance is decimal owed)
            {
                AddRow(_fields, "Statement balance", new Label
                {
                    Text = owed.ToString("c2"),
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }

            AddRow(_fields, "Amount", _amount);
            AddRow(_fields, "Date", _date);
            AddRow(_fields, "Description", _description);

            _transfer.Click += OnSave;
            var cancel = new ThmsButton { Text = "Cancel" };
            cancel.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            _buttons.FlowDirection = FlowDirection.RightToLeft;
            _buttons.Dock = DockStyle.Bottom;
            _buttons.AutoSize = true;
            _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _buttons.WrapContents = false;
            _buttons.Padding = new Padding(16, 8, 16, 16);
            _buttons.MinimumSize = new Size(0, 56);
            _buttons.Controls.Add(cancel);
            _buttons.Controls.Add(_transfer);

            Controls.Add(_fields);
            Controls.Add(_buttons);
            _to.DataSource = choices;
            if (destinations.Count == 1)
                SelectDestination(destinations[0].Id);
            AcceptButton = _transfer;
            CancelButton = cancel;
            UpdateSaveEnabled();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // AutoScale runs when the handle is created. Measuring before that
            // leaves the button bar below the client area.
            PerformLayout();
            var width = ClientSize.Width;
            var fieldsHeight = Math.Max(_fields.Height, _fields.PreferredSize.Height);
            var buttonsHeight = Math.Max(_buttons.Height, _buttons.PreferredSize.Height);
            if (fieldsHeight < 200 || buttonsHeight < 40)
                return;
            ClientSize = new Size(width, fieldsHeight + buttonsHeight);
        }

        private void UpdateSaveEnabled() =>
            _transfer.Enabled = SelectedDestination() is not null;

        private void SelectDestination(Guid accountId)
        {
            if (_to.DataSource is not IList<PayFromChoice> choices)
                return;

            EnsureComboItems(_to);
            for (var i = 0; i < choices.Count; i++)
            {
                if (choices[i].Id != accountId)
                    continue;
                if (i < _to.Items.Count)
                    _to.SelectedIndex = i;
                return;
            }
        }

        private static void EnsureComboItems(ComboBox combo)
        {
            if (combo.Items.Count > 0 || combo.DataSource is null)
                return;
            combo.BindingContext ??= new BindingContext();
        }

        private PayFromChoice? SelectedDestination() =>
            _to.SelectedItem is PayFromChoice choice && choice.Id != Guid.Empty
                ? choice
                : null;

        private static void AddRow(TableLayoutPanel layout, string label, Control field)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var row = layout.RowCount++;
            layout.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 12, 8)
            }, 0, row);
            field.Dock = DockStyle.Fill;
            field.Margin = new Padding(0, 4, 0, 4);
            if (field.MinimumSize.Height < 28)
                field.MinimumSize = new Size(field.MinimumSize.Width, 28);
            layout.Controls.Add(field, 1, row);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (SelectedDestination() is not PayFromChoice destination)
            {
                MessageBox.Show(this, "Select an account.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ToAccountId = destination.Id;
            Amount = _amount.Value;
            Date = _date.Value.Date;
            Description = _description.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
