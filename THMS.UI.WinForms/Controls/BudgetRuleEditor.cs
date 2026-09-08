using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class BudgetRuleEditor : Form
    {
        private BudgetOrchestrator? _orchestrator;
        private Guid _accountId;
        private ExpenseBudgetRule? _existing;

        public BudgetRuleEditor()
        {
            InitializeComponent();
        }

        public BudgetRuleEditor(Guid accountId, ExpenseBudgetRule? existing = null)
            : this(new BudgetOrchestrator(), accountId, existing)
        {
        }

        public BudgetRuleEditor(BudgetOrchestrator orchestrator, Guid accountId, ExpenseBudgetRule? existing = null)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            _accountId = accountId;
            _existing = existing;
            Bind();
        }

        private BudgetOrchestrator Orchestrator =>
            _orchestrator ??= new BudgetOrchestrator();

        private void Bind()
        {
            cmbFrequency.Items.Clear();
            cmbFrequency.Items.AddRange(["Weekly", "Biweekly", "Monthly", "Quarterly", "Annual"]);
            cmbFrequency.SelectedIndex = 2;

            var selected = new HashSet<Guid>(_existing?.IncludedCategoryIds ?? []);
            BindCategoryTree(selected);

            if (_existing is null)
            {
                txtName.Text = "";
                numAmount.Value = 0;
                chkActive.Checked = true;
                btnSave.Enabled = false;
                btnDelete.Enabled = false;
                return;
            }

            txtName.Text = _existing.BudgetName;
            numAmount.Value = Clamp(_existing.DefaultBudgetAmount);
            chkActive.Checked = _existing.IsActive;
            cmbFrequency.SelectedItem = ToLabel(_existing.BudgetFrequency);
            btnSave.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void BindCategoryTree(HashSet<Guid> selected)
        {
            CategoryTreeUi.Fill(treeCategories, Orchestrator.GetCategories(), selected);
        }

        private void OnManageCategories(object? sender, EventArgs e)
        {
            var selected = CheckedCategoryIds().ToHashSet();
            using var manager = new CategoryManager(new CategoryOrchestrator());
            manager.ShowDialog(this);
            BindCategoryTree(selected);
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            if (!TryBuild(out var rule, newId: true))
                return;

            Orchestrator.AddRule(rule);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (!TryBuild(out var rule, newId: false))
                return;

            Orchestrator.UpdateRule(rule);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            if (_existing is null)
                return;

            if (MessageBox.Show(this, "Delete this budget and its history?", "Delete Budget",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            Orchestrator.DeleteRule(_existing.Id);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnClose(object? sender, EventArgs e)
        {
            if (DialogResult == DialogResult.None)
                DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool TryBuild(out ExpenseBudgetRule rule, bool newId)
        {
            rule = null!;
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Budget name is required.", "Budget", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            var categories = CheckedCategoryIds().ToList();
            if (categories.Count == 0)
            {
                MessageBox.Show(this, "Select at least one category.", "Budget", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            rule = new ExpenseBudgetRule
            {
                Id = newId || _existing is null ? Guid.NewGuid() : _existing.Id,
                AccountId = _accountId,
                BudgetName = txtName.Text.Trim(),
                IncludedCategoryIds = categories,
                BudgetFrequency = ParseFrequency(cmbFrequency.SelectedItem?.ToString()),
                DefaultBudgetAmount = numAmount.Value,
                IsActive = chkActive.Checked
            };
            return true;
        }

        private IEnumerable<Guid> CheckedCategoryIds() =>
            CategoryTreeUi.CheckedIds(treeCategories.Nodes);

        private static BudgetFrequency ParseFrequency(string? label) => label switch
        {
            "Weekly" => BudgetFrequency.Weekly,
            "Biweekly" => BudgetFrequency.Biweekly,
            "Quarterly" => BudgetFrequency.Quarterly,
            "Annual" => BudgetFrequency.Annual,
            _ => BudgetFrequency.Monthly
        };

        private static string ToLabel(BudgetFrequency frequency) => frequency switch
        {
            BudgetFrequency.Weekly => "Weekly",
            BudgetFrequency.Biweekly => "Biweekly",
            BudgetFrequency.Quarterly => "Quarterly",
            BudgetFrequency.Annual => "Annual",
            _ => "Monthly"
        };

        private static decimal Clamp(decimal value)
        {
            if (value < -100_000_000m)
                return -100_000_000m;
            if (value > 100_000_000m)
                return 100_000_000m;
            return value;
        }
    }
}
