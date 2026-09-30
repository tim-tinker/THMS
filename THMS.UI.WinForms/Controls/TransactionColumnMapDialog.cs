using THMS.Domain.Finance.Transactions;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class TransactionColumnMapDialog : Form
    {
        private readonly Guid _accountId;
        private readonly IReadOnlyList<string> _headers;
        private readonly ComboBox _date = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _description = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly RadioButton _amountMode = new() { Text = "One amount column", AutoSize = true };
        private readonly RadioButton _debitCreditMode = new() { Text = "Debit and credit columns", AutoSize = true };
        private readonly ComboBox _amount = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _debit = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _credit = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox _flip = new() { Text = "Flip sign", AutoSize = true };

        public TransactionFileColumnMap Map { get; private set; }

        public TransactionColumnMapDialog(string accountName, TransactionFileSheet sheet, TransactionFileColumnMap current)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(current);
            _accountId = current.AccountId;
            _headers = sheet.Headers;
            Map = current;

            Text = $"Import Transactions — {accountName}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(760, 560);
            Size = new Size(980, 700);

            var mapping = BuildMapping(current);
            var buttons = BuildButtons();
            var file = BuildFileGrid(sheet);

            Controls.Add(file);
            Controls.Add(buttons);
            Controls.Add(mapping);
            CancelButton = buttons.Controls.OfType<Button>().First(button => button.DialogResult == DialogResult.Cancel);
            _amountMode.CheckedChanged += (_, _) => UpdateAmountMode();
            _debitCreditMode.CheckedChanged += (_, _) => UpdateAmountMode();
            UpdateAmountMode();
        }

        private Control BuildMapping(TransactionFileColumnMap current)
        {
            Fill(_date, current.DateColumn);
            Fill(_description, current.DescriptionColumn);
            Fill(_amount, current.AmountColumn);
            Fill(_debit, current.DebitColumn);
            Fill(_credit, current.CreditColumn);
            _flip.Checked = current.FlipSign;
            if (current.UseDebitCredit)
                _debitCreditMode.Checked = true;
            else
                _amountMode.Checked = true;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Padding = new Padding(16, 12, 16, 8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var intro = Wrap(
                "Map the file's column headings to the transaction. The account is the one you selected. A positive amount increases this account's ledger balance.");
            layout.Controls.Add(intro, 0, 0);
            layout.SetColumnSpan(intro, 2);

            layout.Controls.Add(FieldLabel("Date"), 0, 1);
            layout.Controls.Add(_date, 1, 1);
            layout.Controls.Add(FieldLabel("Description"), 0, 2);
            layout.Controls.Add(_description, 1, 2);
            layout.Controls.Add(_amountMode, 0, 3);
            layout.Controls.Add(_amount, 1, 3);
            layout.Controls.Add(_flip, 1, 4);
            layout.Controls.Add(_debitCreditMode, 0, 5);

            var debitCredit = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            debitCredit.Controls.Add(FieldLabel("Debit"));
            _debit.Width = 180;
            debitCredit.Controls.Add(_debit);
            debitCredit.Controls.Add(FieldLabel("Credit"));
            _credit.Width = 180;
            debitCredit.Controls.Add(_credit);
            layout.Controls.Add(debitCredit, 1, 5);

            var hint = Wrap("Flip sign applies to the amount column. Credits increase the ledger balance and debits decrease it.");
            layout.Controls.Add(hint, 0, 6);
            layout.SetColumnSpan(hint, 2);

            foreach (var combo in new[] { _date, _description, _amount })
                combo.Dock = DockStyle.Top;
            return layout;
        }

        private Control BuildFileGrid(TransactionFileSheet sheet)
        {
            var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 0) };
            var caption = new Label
            {
                Text = "File contents",
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.BottomLeft
            };
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            foreach (var header in sheet.Headers)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = header,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }

            foreach (var row in sheet.Rows)
            {
                var values = new object[sheet.Headers.Count];
                for (var i = 0; i < values.Length; i++)
                    values[i] = i < row.Cells.Count ? row.Cells[i] : "";
                grid.Rows.Add(values);
            }

            host.Controls.Add(grid);
            host.Controls.Add(caption);
            return host;
        }

        private FlowLayoutPanel BuildButtons()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(16, 8, 16, 16)
            };
            var cancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
            var continueButton = new ThmsButton { Text = "Continue" };
            continueButton.Click += OnContinue;
            panel.Controls.Add(cancel);
            panel.Controls.Add(continueButton);
            return panel;
        }

        private void OnContinue(object? sender, EventArgs e)
        {
            var map = new TransactionFileColumnMap
            {
                AccountId = _accountId,
                DateColumn = Selected(_date),
                DescriptionColumn = Selected(_description),
                UseDebitCredit = _debitCreditMode.Checked,
                AmountColumn = Selected(_amount),
                DebitColumn = Selected(_debit),
                CreditColumn = Selected(_credit),
                FlipSign = _flip.Checked
            };
            var error = TransactionFileColumnMapping.Validate(_headers, map);
            if (error is not null)
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Map = map;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateAmountMode()
        {
            var amount = _amountMode.Checked;
            _amount.Enabled = amount;
            _flip.Enabled = amount;
            _debit.Enabled = !amount;
            _credit.Enabled = !amount;
        }

        private void Fill(ComboBox box, string selected)
        {
            box.Items.Clear();
            foreach (var header in _headers)
                box.Items.Add(header);
            box.SelectedIndex = TransactionFileColumnMapping.ColumnIndex(_headers, selected);
        }

        private static string Selected(ComboBox box) =>
            box.SelectedItem as string ?? "";

        private static Label FieldLabel(string text) =>
            new() { Text = text, AutoSize = true, Margin = new Padding(0, 8, 8, 0) };

        private static Label Wrap(string text) =>
            new()
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(900, 0),
                Margin = new Padding(0, 0, 0, 8)
            };
    }
}
