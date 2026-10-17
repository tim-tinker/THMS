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
        private TransactionOrchestrator? _transactionOrchestrator;

        private Guid? _existingSingleId;
        private Guid? _existingTransferId;
        private List<SplitTransactionRow> _pendingSplits = [];
        private Label? _lblTargetCategory;
        private ComboBox? _cmbTargetCategory;
        private bool _targetCategoryShifted;
        private bool _loadingCategories;
        private Guid? _categoryId;
        private Guid? _targetCategoryId;

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

        private TransactionOrchestrator Transactions =>
            _transactionOrchestrator ??= new TransactionOrchestrator();

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

            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.DropDownWidth = Math.Max(cmbCategory.Width, 320);
            cmbCategory.SelectionChangeCommitted += OnCategoryCommitted;
            BindCategoryLists();

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
            _categoryId = rule.CategoryId;
            _pendingSplits = rule.Splits.Select(s => s.Clone()).ToList();
            BindCategoryLists();
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
            _categoryId = rule.CategoryId;
            _targetCategoryId = rule.TargetCategoryId ?? rule.CategoryId;
            _pendingSplits = rule.Splits.Select(s => s.Clone()).ToList();
            BindCategoryLists();
            cmbFrequency.SelectedItem = ToFrequencyLabel(rule.Frequency);
            dtNextOccurrence.Value = rule.NextOccurrence == default ? DateTime.Today : rule.NextOccurrence;
            _pendingSplits = rule.Splits.Select(s => s.Clone()).ToList();
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
            lblCategory.Text = "Category:";
            if (_lblTargetCategory is not null)
                _lblTargetCategory.Visible = false;
            if (_cmbTargetCategory is not null)
                _cmbTargetCategory.Visible = false;
            ShiftForTargetCategory(shown: false);
            btnEditSplits.Enabled = true;
        }

        private void ShowTransferFields()
        {
            EnsureTargetCategoryControls();
            lblAccount.Visible = cmbAccount.Visible = false;
            lblFromAccount.Visible = cmbFromAccount.Visible = true;
            lblToAccount.Visible = cmbToAccount.Visible = true;
            lblCategory.Text = "From category:";
            _lblTargetCategory!.Visible = true;
            _cmbTargetCategory!.Visible = true;
            ShiftForTargetCategory(shown: true);
            btnEditSplits.Enabled = true;
        }

        private void EnsureTargetCategoryControls()
        {
            if (_cmbTargetCategory is not null)
                return;

            _lblTargetCategory = new Label
            {
                AutoSize = true,
                Location = new Point(lblCategory.Left, lblCategory.Top + 40),
                Text = "To category:"
            };
            _cmbTargetCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Location = new Point(cmbCategory.Left, cmbCategory.Top + 40),
                Size = cmbCategory.Size,
                DropDownWidth = Math.Max(cmbCategory.Width, 320)
            };
            _cmbTargetCategory.SelectionChangeCommitted += OnTargetCategoryCommitted;
            Controls.Add(_lblTargetCategory);
            Controls.Add(_cmbTargetCategory);
            _lblTargetCategory.BringToFront();
            _cmbTargetCategory.BringToFront();
            BindCategoryLists();
        }

        private void ShiftForTargetCategory(bool shown)
        {
            if (_targetCategoryShifted == shown)
                return;

            var delta = shown ? 40 : -40;
            foreach (var control in new Control[] { lblFrequency, cmbFrequency, lblNextOccurrence, dtNextOccurrence, btnEditSplits, pnlButtons })
                control.Top += delta;
            Height += delta;
            _targetCategoryShifted = shown;
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

            try
            {
                if (single is not null)
                {
                    Rules.AddSingleRule(single);
                    PersistPendingSplits(single.Id);
                    _existingSingleId = single.Id;
                    _existingTransferId = null;
                }
                else if (transfer is not null)
                {
                    Rules.AddTransferRule(transfer);
                    PersistPendingSplits(transfer.Id);
                    _existingTransferId = transfer.Id;
                    _existingSingleId = null;
                }
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(this, "Recurring Rule", ex, icon: MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (!TryBuildRule(out var single, out var transfer, newId: false))
                return;

            try
            {
                if (single is not null)
                {
                    single.IsUserCreated = true;
                    Rules.UpdateSingleRule(single);
                    PersistPendingSplits(single.Id);
                }
                else if (transfer is not null)
                {
                    transfer.IsUserCreated = true;
                    Rules.UpdateTransferRule(transfer);
                    PersistPendingSplits(transfer.Id);
                }
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(this, "Recurring Rule", ex, icon: MessageBoxIcon.Information);
                return;
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

            if (_pendingSplits.Count > 0 &&
                !SplitTransactionMath.AmountsMatch(numAmount.Value, _pendingSplits))
            {
                MessageBox.Show(this, "Split amounts must equal the rule amount. Open Edit Splits to auto-balance.",
                    "Recurring Rule", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                ApplyRememberedCategory(transfer, _categoryId);
                if (CategoryById(_targetCategoryId) is ExpenseCategory target)
                {
                    transfer.TargetCategoryId = target.Id;
                    transfer.TargetCategory = target.Name;
                }
                transfer.Splits = _pendingSplits.Select(s => s.Clone()).ToList();
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
            ApplyRememberedCategory(single);
            single.Splits = _pendingSplits.Select(s => s.Clone()).ToList();
            return true;
        }

        private void OnEditSplits(object? sender, EventArgs e) =>
            EditSplits();

        private void OnCategoryCommitted(object? sender, EventArgs e) =>
            HandleCategoryChoice(cmbCategory, target: false);

        private void OnTargetCategoryCommitted(object? sender, EventArgs e)
        {
            if (_cmbTargetCategory is not null)
                HandleCategoryChoice(_cmbTargetCategory, target: true);
        }

        private void HandleCategoryChoice(ComboBox combo, bool target)
        {
            if (_loadingCategories || combo.SelectedItem is not CategoryListItem item)
                return;

            switch (item.Action)
            {
                case CategoryListAction.Split:
                    BeginInvoke(EditSplits);
                    break;
                case CategoryListAction.NewCategory:
                    BeginInvoke(() => CreateCategory(target));
                    break;
                case CategoryListAction.ManageCategories:
                    BeginInvoke(() => ManageCategories());
                    break;
                default:
                    if (target)
                        _targetCategoryId = item.CategoryId;
                    else
                    {
                        _categoryId = item.CategoryId;
                        _pendingSplits.Clear();
                    }
                    break;
            }
        }

        private void EditSplits()
        {
            var hadSplits = _pendingSplits.Count > 0;
            using var editor = new SplitTransactionEditor(
                txtDescription.Text.Trim(),
                numAmount.Value,
                _pendingSplits,
                ActiveCategories(),
                Accounts.GetAllAccounts().ToList());
            if (editor.ShowDialog(this) != DialogResult.OK)
            {
                if (!hadSplits)
                    _pendingSplits.Clear();
                BindCategoryLists();
                return;
            }

            _pendingSplits = editor.Result;
            BindCategoryLists();
        }

        private void CreateCategory(bool target)
        {
            using var editor = new CategoryEditor(new CategoryOrchestrator());
            if (editor.ShowDialog(this) != DialogResult.OK || editor.CreatedCategory is null)
            {
                BindCategoryLists();
                return;
            }

            if (target)
                _targetCategoryId = editor.CreatedCategory.Id;
            else
            {
                _categoryId = editor.CreatedCategory.Id;
                _pendingSplits.Clear();
            }

            BindCategoryLists();
        }

        private void ManageCategories()
        {
            using var manager = new CategoryManager(new CategoryOrchestrator());
            manager.ShowDialog(this);
            BindCategoryLists();
        }

        private void BindCategoryLists()
        {
            BindCategoryCombo(cmbCategory, includeSplit: true, _categoryId, selectSplit: _pendingSplits.Count > 0);
            if (_cmbTargetCategory is not null)
                BindCategoryCombo(_cmbTargetCategory, includeSplit: false, _targetCategoryId, selectSplit: false);
        }

        private void BindCategoryCombo(ComboBox combo, bool includeSplit, Guid? selectedId, bool selectSplit)
        {
            var categories = ActiveCategories();
            if (selectedId is Guid id && categories.All(c => c.Id != id) && CategoryById(id) is ExpenseCategory current)
                categories.Add(current);

            var items = CategoryTreeUi.ComboItems(categories, includeSplit);
            _loadingCategories = true;
            combo.DisplayMember = nameof(CategoryListItem.Label);
            combo.DataSource = items;
            combo.DropDownWidth = Math.Max(combo.Width, 320);
            CategoryListItem? match = selectSplit
                ? items.FirstOrDefault(i => i.Action == CategoryListAction.Split)
                : items.FirstOrDefault(i => i.Action == CategoryListAction.None && i.CategoryId == selectedId);
            match ??= items.FirstOrDefault(i => i.CategoryId == DefaultExpenseCategories.UncategorizedId)
                ?? items.FirstOrDefault(i => i.Action == CategoryListAction.None);
            if (match is not null)
                combo.SelectedItem = match;
            if (!selectSplit && match is { Action: CategoryListAction.None, CategoryId: Guid chosen })
            {
                if (ReferenceEquals(combo, cmbCategory))
                    _categoryId ??= chosen;
                else if (ReferenceEquals(combo, _cmbTargetCategory))
                    _targetCategoryId ??= chosen;
            }
            _loadingCategories = false;
        }

        private List<ExpenseCategory> ActiveCategories() =>
            Rules.GetCategories().Where(c => c.IsActive).ToList();

        private ExpenseCategory? CategoryById(Guid? categoryId) =>
            categoryId is Guid id
                ? Rules.GetCategories().FirstOrDefault(c => c.Id == id)
                : null;

        private void ApplyRememberedCategory(BaseTransaction transaction, Guid? categoryId = null)
        {
            if (CategoryById(categoryId ?? _categoryId) is ExpenseCategory category)
                transaction.ApplyCategory(category);
        }

        private void PersistPendingSplits(Guid parentId)
        {
            if (_pendingSplits.Count == 0)
            {
                Transactions.ClearSplits(parentId);
                return;
            }

            Transactions.ApplySplits(parentId, _pendingSplits.Select(s => s.Clone()).ToList());
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
