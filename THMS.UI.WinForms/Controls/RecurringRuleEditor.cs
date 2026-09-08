using System.ComponentModel;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class RecurringRuleEditor : Form
    {
        private RecurringRuleOrchestrator? _ruleOrchestrator;
        private AccountOrchestrator? _accountOrchestrator;

        private Guid? _existingSingleId;
        private Guid? _existingTransferId;

        public RecurringRuleEditor()
        {
            InitializeComponent();
        }

        public RecurringRuleEditor(
            Guid? preferredAccountId,
            RecurringSingleTransactionRule? existingSingle = null,
            RecurringTransferRule? existingTransfer = null)
            : this(new RecurringRuleOrchestrator(), new AccountOrchestrator(),
                preferredAccountId, existingSingle, existingTransfer)
        {
        }

        public RecurringRuleEditor(
            RecurringRuleOrchestrator ruleOrchestrator,
            AccountOrchestrator accountOrchestrator,
            Guid? preferredAccountId = null,
            RecurringSingleTransactionRule? existingSingle = null,
            RecurringTransferRule? existingTransfer = null)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _ruleOrchestrator = ruleOrchestrator;
            _accountOrchestrator = accountOrchestrator;

            PopulateLookups(preferredAccountId);

            if (existingSingle is not null)
                BindSingle(existingSingle);
            else if (existingTransfer is not null)
                BindTransfer(existingTransfer);
            else
                ShowSingleFields();

            UpdateButtonState();
        }

        private RecurringRuleOrchestrator Rules =>
            _ruleOrchestrator ??= new RecurringRuleOrchestrator();

        private AccountOrchestrator Accounts =>
            _accountOrchestrator ??= new AccountOrchestrator();

        private void PopulateLookups(Guid? preferredAccountId)
        {
            var accounts = Accounts.GetAllAccounts().OrderBy(a => a.Name).ToList();
            BindAccountCombo(cmbAccount, accounts, preferredAccountId);
            BindAccountCombo(cmbFromAccount, accounts, preferredAccountId);
            BindAccountCombo(cmbToAccount, accounts, accounts.FirstOrDefault(a => a.Id != preferredAccountId)?.Id);

            cmbRuleType.Items.Clear();
            cmbRuleType.Items.Add("Single");
            cmbRuleType.Items.Add("Transfer");
            cmbRuleType.SelectedIndex = 0;

            cmbFrequency.Items.Clear();
            cmbFrequency.Items.AddRange(["Weekly", "Biweekly", "Monthly", "Quarterly", "Yearly"]);
            cmbFrequency.SelectedIndex = 2;

            cmbCategory.DisplayMember = nameof(ExpenseCategory.Name);
            cmbCategory.ValueMember = nameof(ExpenseCategory.Id);
            cmbCategory.DataSource = Rules.GetCategories().ToList();

            dtNextOccurrence.Value = DateTime.Today;
            numAmount.Value = 0;
        }

        private static void BindAccountCombo(ComboBox combo, List<Account> accounts, Guid? selectedId)
        {
            combo.DisplayMember = nameof(Account.Name);
            combo.ValueMember = nameof(Account.Id);
            combo.DataSource = new List<Account>(accounts);
            if (selectedId is Guid id)
            {
                var match = accounts.FirstOrDefault(a => a.Id == id);
                if (match is not null)
                    combo.SelectedItem = match;
            }
        }

        private void BindSingle(RecurringSingleTransactionRule rule)
        {
            _existingSingleId = rule.Id;
            _existingTransferId = null;
            cmbRuleType.SelectedItem = "Single";
            ShowSingleFields();
            SelectAccount(cmbAccount, rule.AccountId);
            txtDescription.Text = rule.Description ?? "";
            numAmount.Value = ClampAmount(rule.Amount);
            SelectCategory(rule.CategoryId, rule.Category);
            cmbFrequency.SelectedItem = ToFrequencyLabel(rule.Frequency);
            dtNextOccurrence.Value = rule.NextOccurrence == default ? DateTime.Today : rule.NextOccurrence;
        }

        private void BindTransfer(RecurringTransferRule rule)
        {
            _existingTransferId = rule.Id;
            _existingSingleId = null;
            cmbRuleType.SelectedItem = "Transfer";
            ShowTransferFields();
            SelectAccount(cmbFromAccount, rule.FromAccountId);
            SelectAccount(cmbToAccount, rule.ToAccountId);
            txtDescription.Text = rule.Description ?? "";
            numAmount.Value = ClampAmount(rule.Amount);
            SelectCategory(rule.CategoryId, rule.Category);
            cmbFrequency.SelectedItem = ToFrequencyLabel(rule.Frequency);
            dtNextOccurrence.Value = rule.NextOccurrence == default ? DateTime.Today : rule.NextOccurrence;
        }

        private void SelectCategory(Guid? categoryId, string? name)
        {
            if (cmbCategory.DataSource is not IEnumerable<ExpenseCategory> categories)
                return;

            var match = categories.FirstOrDefault(c => c.Id == categoryId)
                ?? categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                cmbCategory.SelectedItem = match;
        }

        private void ApplySelectedCategory(BaseTransaction transaction)
        {
            if (cmbCategory.SelectedItem is ExpenseCategory category)
                transaction.ApplyCategory(category);
        }

        private static void SelectAccount(ComboBox combo, Guid accountId)
        {
            if (combo.DataSource is IEnumerable<Account> accounts)
            {
                var match = accounts.FirstOrDefault(a => a.Id == accountId);
                if (match is not null)
                    combo.SelectedItem = match;
            }
        }

        private static decimal ClampAmount(decimal amount)
        {
            if (amount < -100_000_000m)
                return -100_000_000m;
            if (amount > 100_000_000m)
                return 100_000_000m;
            return amount;
        }

        private void OnRuleTypeChanged(object? sender, EventArgs e)
        {
            if (cmbRuleType.SelectedItem?.ToString() == "Transfer")
                ShowTransferFields();
            else
                ShowSingleFields();
        }

        private void ShowSingleFields()
        {
            lblAccount.Visible = cmbAccount.Visible = true;
            lblFromAccount.Visible = cmbFromAccount.Visible = false;
            lblToAccount.Visible = cmbToAccount.Visible = false;
        }

        private void ShowTransferFields()
        {
            lblAccount.Visible = cmbAccount.Visible = false;
            lblFromAccount.Visible = cmbFromAccount.Visible = true;
            lblToAccount.Visible = cmbToAccount.Visible = true;
        }

        private void UpdateButtonState()
        {
            var hasExisting = _existingSingleId.HasValue || _existingTransferId.HasValue;
            btnSave.Enabled = hasExisting;
            btnDelete.Enabled = hasExisting;
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            if (!TryBuildRule(out var single, out var transfer, newId: true))
                return;

            if (single is not null)
            {
                Rules.AddSingleRule(single);
                _existingSingleId = single.Id;
                _existingTransferId = null;
            }
            else if (transfer is not null)
            {
                Rules.AddTransferRule(transfer);
                _existingTransferId = transfer.Id;
                _existingSingleId = null;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (!TryBuildRule(out var single, out var transfer, newId: false))
                return;

            if (single is not null)
            {
                single.IsUserCreated = true;
                Rules.UpdateSingleRule(single);
            }
            else if (transfer is not null)
            {
                transfer.IsUserCreated = true;
                Rules.UpdateTransferRule(transfer);
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                    this,
                    "Delete this recurring rule?",
                    "Delete Rule",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (_existingSingleId is Guid singleId)
                Rules.DeleteSingleRule(singleId);
            else if (_existingTransferId is Guid transferId)
                Rules.DeleteTransferRule(transferId);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnClose(object? sender, EventArgs e)
        {
            if (DialogResult == DialogResult.None)
                DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool TryBuildRule(
            out RecurringSingleTransactionRule? single,
            out RecurringTransferRule? transfer,
            bool newId)
        {
            single = null;
            transfer = null;

            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show(this, "Description is required.", "Recurring Rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            var frequency = ParseFrequency(cmbFrequency.SelectedItem?.ToString());
            var isTransfer = cmbRuleType.SelectedItem?.ToString() == "Transfer";

            if (isTransfer)
            {
                if (cmbFromAccount.SelectedItem is not Account from || cmbToAccount.SelectedItem is not Account to)
                {
                    MessageBox.Show(this, "From and To accounts are required.", "Recurring Rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                if (from.Id == to.Id)
                {
                    MessageBox.Show(this, "From and To accounts must be different.", "Recurring Rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                transfer = new RecurringTransferRule
                {
                    Id = newId || _existingTransferId is null ? Guid.NewGuid() : _existingTransferId.Value,
                    FromAccountId = from.Id,
                    ToAccountId = to.Id,
                    Description = txtDescription.Text.Trim(),
                    Amount = numAmount.Value,
                    Frequency = frequency,
                    NextOccurrence = dtNextOccurrence.Value.Date,
                    IsActive = true,
                    IsUserCreated = true
                };
                ApplySelectedCategory(transfer);
                return true;
            }

            if (cmbAccount.SelectedItem is not Account account)
            {
                MessageBox.Show(this, "Account is required.", "Recurring Rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            single = new RecurringSingleTransactionRule
            {
                Id = newId || _existingSingleId is null ? Guid.NewGuid() : _existingSingleId.Value,
                AccountId = account.Id,
                Description = txtDescription.Text.Trim(),
                Amount = numAmount.Value,
                Frequency = frequency,
                NextOccurrence = dtNextOccurrence.Value.Date,
                IsActive = true,
                IsUserCreated = true
            };
            ApplySelectedCategory(single);
            return true;
        }

        private static RecurrenceFrequency ParseFrequency(string? label) => label switch
        {
            "Weekly" => RecurrenceFrequency.Weekly,
            "Biweekly" => RecurrenceFrequency.BiWeekly,
            "Quarterly" => RecurrenceFrequency.Quarterly,
            "Yearly" => RecurrenceFrequency.Yearly,
            _ => RecurrenceFrequency.Monthly
        };

        private static string ToFrequencyLabel(RecurrenceFrequency frequency) => frequency switch
        {
            RecurrenceFrequency.Weekly => "Weekly",
            RecurrenceFrequency.BiWeekly => "Biweekly",
            RecurrenceFrequency.Quarterly => "Quarterly",
            RecurrenceFrequency.Yearly => "Yearly",
            _ => "Monthly"
        };
    }
}
