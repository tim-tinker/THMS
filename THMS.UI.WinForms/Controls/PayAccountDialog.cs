using System.ComponentModel;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Planning;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class PayAccountDialog : Form
    {
        private readonly ComboBox _from = new();
        private readonly Label _to = new() { TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label _statementBalance = new() { TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label _nonPromotion = new() { TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label _promoTotal = new() { TextAlign = ContentAlignment.MiddleLeft };
        private readonly BindingList<PromotionPayRow> _promotions = [];
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _date = new();
        private readonly TextBox _description = new();
        private readonly ThmsButton _save = new() { Text = "Save" };

        public Guid FundingAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime Date { get; private set; }
        public string Description { get; private set; } = "";

        public PayAccountDialog(
            Account destination,
            IReadOnlyList<Account> banks,
            decimal? statementBalance,
            decimal? amountDue,
            string? defaultDescription,
            IReadOnlyList<PromotionalBalance>? promotions = null,
            DateTime? asOf = null)
        {
            ArgumentNullException.ThrowIfNull(destination);
            var hasPromotions = promotions is { Count: > 0 };
            Text = hasPromotions ? "Pay Promotions" : "Pay";
            FormBorderStyle = hasPromotions ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = hasPromotions ? new Size(980, 720) : new Size(540, 400);
            ClientSize = hasPromotions ? new Size(980, 720) : new Size(520, 380);

            _from.DropDownStyle = ComboBoxStyle.DropDownList;
            _from.DisplayMember = nameof(PayFromChoice.Name);
            _from.ValueMember = nameof(PayFromChoice.Id);
            _from.SelectedIndexChanged += (_, _) => UpdateSaveEnabled();
            var choices = new List<PayFromChoice> { PayFromChoice.Unspecified };
            choices.AddRange(banks.Select(PayFromChoice.From));

            _to.Text = destination.Name;
            _statementBalance.Text = statementBalance is decimal balance
                ? balance.ToString("c2")
                : AccountStatementListRow.NotApplicable;

            _amount.DecimalPlaces = 2;
            _amount.Minimum = 0.01m;
            _amount.Maximum = 99_999_999;
            _amount.MinimumSize = new Size(0, 28);
            if (amountDue is decimal due && due >= _amount.Minimum)
                _amount.Value = Math.Min(due, _amount.Maximum);

            _date.Format = DateTimePickerFormat.Short;
            _date.Value = DateTime.Today;
            _description.Text = defaultDescription ?? "";

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 0,
                Padding = new Padding(16, 16, 16, 16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            AddRow(layout, "From account", _from);
            AddRow(layout, "To account", _to);
            AddRow(layout, "Statement balance", _statementBalance);

            if (hasPromotions)
            {
                var promoAsOf = asOf is DateTime date && date.Year > 1 ? date.Date : DateTime.Today;
                foreach (var plan in PromotionPaymentPlanner.Plan(promotions!, promoAsOf))
                    _promotions.Add(PromotionPayRow.From(plan));

                _nonPromotion.Text = PromotionPaymentPlanner
                    .NonPromotionBalance(statementBalance ?? 0m, promotions!)
                    .ToString("c2");
                AddRow(layout, "Non-promotion", _nonPromotion);
                AddRow(layout, "Promotion payments", _promoTotal);

                var grid = CreatePromotionGrid();
                var gridHeight = grid.ColumnHeadersHeight + grid.RowTemplate.Height * 3 + 10;
                grid.MinimumSize = new Size(0, gridHeight);
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                var gridRow = layout.RowCount++;
                layout.Controls.Add(grid, 0, gridRow);
                layout.SetColumnSpan(grid, 2);
                RefreshPromoTotal();
            }
            AddRow(layout, "Amount", _amount);
            AddRow(layout, "Date", _date);
            AddRow(layout, "Description", _description);

            _save.Click += OnSave;
            var cancel = new ThmsButton { Text = "Cancel" };
            cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(12, 12, 12, 16)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(_save);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var buttonRow = layout.RowCount++;
            layout.Controls.Add(buttons, 0, buttonRow);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);
            _from.DataSource = choices;
            if (_from.Items.Count > 0)
                _from.SelectedIndex = 0;
            AcceptButton = _save;
            CancelButton = cancel;
            UpdateSaveEnabled();
        }

        private void UpdateSaveEnabled()
        {
            _save.Enabled = SelectedFunding() is not null;
        }

        private PayFromChoice? SelectedFunding() =>
            _from.SelectedItem is PayFromChoice choice && choice.Id != Guid.Empty
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
                AutoEllipsis = false,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 12, 8)
            }, 0, row);
            field.Dock = DockStyle.Fill;
            field.Margin = new Padding(0, 4, 0, 4);
            if (field.MinimumSize.Height < 28)
                field.MinimumSize = new Size(field.MinimumSize.Width, 28);
            layout.Controls.Add(field, 1, row);
        }

        private DataGridView CreatePromotionGrid()
        {
            var grid = new DataGridView
            {
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 8),
                ScrollBars = ScrollBars.Vertical
            };
            grid.RowTemplate.Height = 28;
            DataGridViewUtil.EnableDoubleBuffering(grid);
            var promotion = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PromotionPayRow.Promotion),
                HeaderText = "Promotion",
                Name = nameof(PromotionPayRow.Promotion),
                ReadOnly = true,
                FillWeight = 280,
                MinimumWidth = 360
            };
            grid.Columns.Add(promotion);
            grid.Columns.Add(FrequencyColumn());
            var required = AmountColumn(nameof(PromotionPayRow.RequiredAmount), "Required");
            required.ReadOnly = true;
            grid.Columns.Add(required);
            grid.Columns.Add(AmountColumn(nameof(PromotionPayRow.SelectedAmount), "Pay"));
            grid.DataSource = _promotions;
            grid.DataBindingComplete += (_, _) => ApplyFrequencyEditability(grid);
            grid.CellBeginEdit += OnFrequencyBeginEdit;
            grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                RefreshPromoTotal();
            };
            grid.DataError += (_, e) => e.ThrowException = false;
            return grid;
        }

        private static DataGridViewComboBoxColumn FrequencyColumn()
        {
            var choices = PromotionPaymentPlanner.LumpSumFrequencies
                .Select(frequency => new FrequencyChoice(
                    PromotionPaymentPlanner.FrequencyName(frequency),
                    frequency))
                .ToList();
            return new DataGridViewComboBoxColumn
            {
                DataPropertyName = nameof(PromotionPayRow.Frequency),
                HeaderText = "Frequency",
                Name = nameof(PromotionPayRow.Frequency),
                DataSource = choices,
                DisplayMember = nameof(FrequencyChoice.Name),
                ValueMember = nameof(FrequencyChoice.Frequency),
                ValueType = typeof(RecurrenceFrequency),
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                DisplayStyleForCurrentCellOnly = true,
                FlatStyle = FlatStyle.Flat,
                FillWeight = 90
            };
        }

        private static void ApplyFrequencyEditability(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.DataBoundItem is not PromotionPayRow pay)
                    continue;
                if (row.Cells[nameof(PromotionPayRow.Frequency)] is not DataGridViewComboBoxCell cell)
                    continue;
                cell.ReadOnly = !pay.FrequencyEditable;
                cell.DisplayStyle = pay.FrequencyEditable
                    ? DataGridViewComboBoxDisplayStyle.DropDownButton
                    : DataGridViewComboBoxDisplayStyle.Nothing;
            }
        }

        private void OnFrequencyBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (sender is not DataGridView grid)
                return;
            if (grid.Columns[e.ColumnIndex].Name != nameof(PromotionPayRow.Frequency))
                return;
            if (grid.Rows[e.RowIndex].DataBoundItem is PromotionPayRow pay && !pay.FrequencyEditable)
                e.Cancel = true;
        }

        private static DataGridViewTextBoxColumn AmountColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ValueType = typeof(decimal),
                DefaultCellStyle = { Format = "c2", Alignment = DataGridViewContentAlignment.MiddleRight },
                FillWeight = 70
            };

        private void RefreshPromoTotal()
        {
            _promoTotal.Text = _promotions.Sum(row => row.SelectedAmount).ToString("c2");
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (SelectedFunding() is not PayFromChoice funding)
            {
                MessageBox.Show(this, "Select a bank account.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FundingAccountId = funding.Id;
            Amount = _amount.Value;
            Date = _date.Value.Date;
            Description = _description.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class FrequencyChoice(string name, RecurrenceFrequency frequency)
        {
            public string Name { get; } = name;
            public RecurrenceFrequency Frequency { get; } = frequency;
        }

        private sealed class PromotionPayRow : INotifyPropertyChanged
        {
            private RecurrenceFrequency _frequency = RecurrenceFrequency.Monthly;
            private decimal _requiredAmount;
            private decimal _selectedAmount;
            private bool _frequencyReady;

            public event PropertyChangedEventHandler? PropertyChanged;

            public string Promotion { get; init; } = "";
            public PromoType Type { get; init; }
            public DateTime AsOf { get; init; }
            public DateTime Deadline { get; init; }
            public bool FrequencyEditable { get; init; }
            public decimal CurrentBalance { get; init; }

            public RecurrenceFrequency Frequency
            {
                get => _frequency;
                set
                {
                    if (!FrequencyEditable)
                        value = RecurrenceFrequency.Monthly;
                    if (_frequencyReady && _frequency == value)
                        return;

                    _frequency = value;
                    _frequencyReady = true;
                    ApplyRequired(PromotionPaymentPlanner.RequiredThisPayment(
                        Type, CurrentBalance, AsOf, Deadline, value));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frequency)));
                }
            }

            public decimal RequiredAmount
            {
                get => _requiredAmount;
                private set
                {
                    _requiredAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RequiredAmount)));
                }
            }

            public decimal SelectedAmount
            {
                get => _selectedAmount;
                set
                {
                    var clamped = Math.Clamp(value, 0m, Math.Max(0m, CurrentBalance));
                    _selectedAmount = clamped;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedAmount)));
                }
            }

            public static PromotionPayRow From(PromotionPaymentPlan plan)
            {
                var row = new PromotionPayRow
                {
                    Promotion = plan.Promotion,
                    Type = plan.Type,
                    AsOf = plan.AsOf,
                    Deadline = plan.Deadline,
                    FrequencyEditable = plan.FrequencyEditable,
                    CurrentBalance = plan.CurrentBalance
                };
                row.Frequency = plan.Frequency;
                return row;
            }

            private void ApplyRequired(decimal required)
            {
                RequiredAmount = required;
                SelectedAmount = Math.Min(required, CurrentBalance);
            }
        }
    }
}
