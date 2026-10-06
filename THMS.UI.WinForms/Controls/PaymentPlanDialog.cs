using System.ComponentModel;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Planning;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class PaymentPlanDialog : Form
    {
        private readonly ComboBox _from = new();
        private readonly ComboBox _frequency = new();
        private readonly NumericUpDown _amount = new();
        private readonly DateTimePicker _next = new();
        private readonly DateTimePicker _end = new();
        private readonly Label _requiredTotal = new() { TextAlign = ContentAlignment.MiddleLeft };
        private readonly BindingList<PlanPromoRow> _rows = [];
        private readonly IReadOnlyList<PromotionalBalance> _promotions;
        private readonly DateTime _asOf;
        private readonly RecurrenceFrequency _initialFrequency;
        private readonly Guid? _fundingAccountId;
        private readonly decimal? _savedAmount;
        private bool _suppressFrequency;

        public bool DeletePlan { get; private set; }
        public Guid FundingAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public RecurrenceFrequency Frequency { get; private set; }
        public DateTime NextDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public PaymentPlanDialog(
            Account destination,
            IReadOnlyList<Account> banks,
            decimal? statementBalance,
            IReadOnlyList<PromotionalBalance> promotions,
            DateTime? asOf,
            RecurringTransferRule? existing)
        {
            ArgumentNullException.ThrowIfNull(destination);
            ArgumentNullException.ThrowIfNull(banks);
            ArgumentNullException.ThrowIfNull(promotions);
            _promotions = promotions;
            _asOf = asOf is DateTime date && date.Year > 1 ? date.Date : DateTime.Today;
            _initialFrequency = existing is not null
                && PromotionPaymentPlanner.LumpSumFrequencies.Contains(existing.Frequency)
                ? existing.Frequency
                : RecurrenceFrequency.Monthly;
            _fundingAccountId = existing?.FromAccountId is Guid fromId && fromId != Guid.Empty ? fromId : null;
            _savedAmount = existing is not null && existing.Amount > 0 ? existing.Amount : null;

            Text = "Payment plan";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(980, 720);
            ClientSize = new Size(980, 720);

            var choices = new List<PayFromChoice> { PayFromChoice.Unspecified };
            choices.AddRange(banks.Select(PayFromChoice.From));
            _from.DropDownStyle = ComboBoxStyle.DropDownList;
            _from.DisplayMember = nameof(PayFromChoice.Name);
            _from.ValueMember = nameof(PayFromChoice.Id);
            _from.DataSource = choices;
            _from.SelectedIndexChanged += (_, _) => UpdateSaveEnabled();

            var frequencyChoices = PromotionPaymentPlanner.LumpSumFrequencies
                .Select(frequency => new FrequencyChoice(PromotionPaymentPlanner.FrequencyName(frequency), frequency))
                .ToList();
            _frequency.DropDownStyle = ComboBoxStyle.DropDownList;
            _frequency.DisplayMember = nameof(FrequencyChoice.Name);
            _frequency.ValueMember = nameof(FrequencyChoice.Frequency);
            _frequency.DataSource = frequencyChoices;

            _amount.DecimalPlaces = 2;
            _amount.Minimum = 0.01m;
            _amount.Maximum = 99_999_999;
            _amount.MinimumSize = new Size(0, 28);
            _next.Format = DateTimePickerFormat.Short;
            _end.Format = DateTimePickerFormat.Short;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            AddRow(layout, "From account", _from);
            AddRow(layout, "To account", new Label
            {
                Text = destination.Name,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            });
            AddRow(layout, "Statement balance", new Label
            {
                Text = statementBalance is decimal balance
                    ? balance.ToString("c2")
                    : AccountStatementListRow.NotApplicable,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            });
            if (promotions.Count > 0)
            {
                AddRow(layout, "Non-promotion", new Label
                {
                    Text = PromotionPaymentPlanner
                        .NonPromotionBalance(statementBalance ?? 0m, promotions)
                        .ToString("c2"),
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }

            AddRow(layout, "Frequency", _frequency);
            var grid = CreateGrid();
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            var gridRow = layout.RowCount++;
            layout.Controls.Add(grid, 0, gridRow);
            layout.SetColumnSpan(grid, 2);
            AddRow(layout, "Required total", _requiredTotal);
            AddRow(layout, "Amount", _amount);
            AddRow(layout, "Next date", _next);
            AddRow(layout, "End date", _end);

            var save = new ThmsButton { Text = "Save" };
            save.Click += (_, _) => OnSave();
            var cancel = new ThmsButton { Text = "Cancel" };
            cancel.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(12, 12, 12, 16)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            if (existing is not null)
            {
                var delete = new ThmsButton { Text = "Delete", Destructive = true };
                delete.Click += (_, _) => OnDelete(destination.Name);
                buttons.Controls.Add(delete);
            }

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var buttonRow = layout.RowCount++;
            layout.Controls.Add(buttons, 0, buttonRow);
            layout.SetColumnSpan(buttons, 2);
            Controls.Add(layout);

            _frequency.SelectedIndexChanged += (_, _) =>
            {
                if (_suppressFrequency || _frequency.SelectedItem is not FrequencyChoice choice)
                    return;
                ApplyFrequency(choice.Frequency, useRecommendedAmount: true);
            };
            _next.Value = existing?.NextOccurrence.Date ?? DateTime.Today;
            _end.Value = ExistingEnd(existing) ?? DefaultEnd();
            SelectFrequency(_initialFrequency);
            ApplyFrequency(_initialFrequency, useRecommendedAmount: _savedAmount is null);
            if (_savedAmount is decimal saved)
                SetAmount(saved);
            SelectFunding(_fundingAccountId);

            AcceptButton = save;
            CancelButton = cancel;
            UpdateSaveEnabled();
        }

        private void OnSave()
        {
            if (SelectedFunding() is not PayFromChoice funding)
            {
                MessageBox.Show(this, "Select a bank account.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_end.Value.Date < _next.Value.Date)
            {
                MessageBox.Show(this, "End date must be on or after the next payment.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            FundingAccountId = funding.Id;
            Amount = _amount.Value;
            Frequency = SelectedFrequency();
            NextDate = _next.Value.Date;
            EndDate = _end.Value.Date;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnDelete(string accountName)
        {
            var answer = MessageBox.Show(this, $"Delete the payment plan for {accountName}?", Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
                return;
            DeletePlan = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RestoreSavedChoices();
            BeginInvoke(new Action(RestoreSavedChoices));
        }

        private void RestoreSavedChoices()
        {
            SelectFrequency(_initialFrequency);
            ApplyFrequency(SelectedFrequency(), useRecommendedAmount: _savedAmount is null);
            if (_savedAmount is decimal saved)
                SetAmount(saved);
            SelectFunding(_fundingAccountId);
        }

        private void SelectFrequency(RecurrenceFrequency frequency)
        {
            if (_frequency.Items.Cast<object>().FirstOrDefault(item =>
                    item is FrequencyChoice choice && choice.Frequency == frequency) is not FrequencyChoice match)
                return;

            _suppressFrequency = true;
            _frequency.SelectedItem = match;
            _suppressFrequency = false;
        }

        private RecurrenceFrequency SelectedFrequency() =>
            _frequency.SelectedItem is FrequencyChoice choice
                ? choice.Frequency
                : _initialFrequency;

        private void ApplyFrequency(RecurrenceFrequency frequency, bool useRecommendedAmount)
        {
            _rows.RaiseListChangedEvents = false;
            _rows.Clear();
            foreach (var promo in _promotions)
            {
                _rows.Add(new PlanPromoRow
                {
                    Promotion = Describe(promo),
                    Frequency = PromotionPaymentPlanner.FrequencyName(frequency),
                    RequiredAmount = PromotionPaymentPlanner.RequiredThisPayment(promo, _asOf, frequency)
                });
            }

            _rows.RaiseListChangedEvents = true;
            _rows.ResetBindings();
            var recommended = PromotionPaymentPlanner.RecommendedPayment(_promotions, _asOf, frequency);
            _requiredTotal.Text = recommended.ToString("c2");
            if (useRecommendedAmount)
                SetAmount(recommended);
        }

        private void SetAmount(decimal amount)
        {
            if (amount < _amount.Minimum)
                return;
            _amount.Value = Math.Min(amount, _amount.Maximum);
        }

        private DateTime? ExistingEnd(RecurringTransferRule? existing) =>
            existing?.EndDate is DateTime end && end.Year > 1 ? end.Date : null;

        private DateTime DefaultEnd()
        {
            var deadlines = _promotions
                .Select(promo => promo.Deadline.Date)
                .Where(deadline => deadline.Year > 1)
                .ToList();
            return deadlines.Count > 0 ? deadlines.Max() : _next.Value.Date.AddYears(1);
        }

        private void SelectFunding(Guid? accountId)
        {
            if (accountId is not Guid id || _from.DataSource is not IList<PayFromChoice> choices)
                return;

            for (var i = 0; i < choices.Count; i++)
            {
                if (choices[i].Id != id)
                    continue;
                _from.SelectedIndex = i;
                return;
            }
        }

        private PayFromChoice? SelectedFunding() =>
            _from.SelectedItem is PayFromChoice choice && choice.Id != Guid.Empty ? choice : null;

        private void UpdateSaveEnabled()
        {
            if (AcceptButton is ThmsButton save)
                save.Enabled = SelectedFunding() is not null;
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 8),
                ScrollBars = ScrollBars.Vertical,
                DataSource = _rows
            };
            grid.RowTemplate.Height = 28;
            DataGridViewUtil.EnableDoubleBuffering(grid);
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PlanPromoRow.Promotion),
                HeaderText = "Promotion",
                Name = nameof(PlanPromoRow.Promotion),
                FillWeight = 280,
                MinimumWidth = 280
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PlanPromoRow.Frequency),
                HeaderText = "Frequency",
                Name = nameof(PlanPromoRow.Frequency),
                FillWeight = 90
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(PlanPromoRow.RequiredAmount),
                HeaderText = "Required",
                Name = nameof(PlanPromoRow.RequiredAmount),
                DefaultCellStyle = { Format = "c2", Alignment = DataGridViewContentAlignment.MiddleRight },
                FillWeight = 70
            });
            return grid;
        }

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

        private static string Describe(PromotionalBalance promo)
        {
            var type = PromoTypeDisplay.Name(promo.Type);
            var due = promo.Deadline.Year > 1 ? $" due {promo.Deadline:d}" : "";
            return $"{type} {promo.CurrentBalance:c2}{due}";
        }

        private sealed class FrequencyChoice(string name, RecurrenceFrequency frequency)
        {
            public string Name { get; } = name;
            public RecurrenceFrequency Frequency { get; } = frequency;
        }

        private sealed class PlanPromoRow
        {
            public string Promotion { get; init; } = "";
            public string Frequency { get; init; } = "";
            public decimal RequiredAmount { get; init; }
        }
    }
}
