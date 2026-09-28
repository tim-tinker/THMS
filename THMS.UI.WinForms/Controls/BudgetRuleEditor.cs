using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class BudgetRuleEditor : Form
    {
        private BudgetOrchestrator? _orchestrator;
        private ExpenseBudgetRule? _existing;
        private ExpenseCategory? _seedCategory;

        public BudgetRuleEditor()
        {
            InitializeComponent();
        }

        public BudgetRuleEditor(BudgetOrchestrator orchestrator, ExpenseBudgetRule? existing = null)
            : this(orchestrator, existing, seedCategory: null)
        {
        }

        public BudgetRuleEditor(BudgetOrchestrator orchestrator, ExpenseCategory seedCategory)
            : this(orchestrator, existing: null, seedCategory)
        {
        }

        public BudgetRuleEditor(
            BudgetOrchestrator orchestrator,
            ExpenseBudgetRule? existing,
            ExpenseCategory? seedCategory)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            _existing = existing;
            _seedCategory = seedCategory;
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
            if (_seedCategory is not null)
                selected.Add(_seedCategory.Id);
            BindCategoryTree(selected);

            dtPeriodStart.Value = DateTime.Today;
            dtPeriodStart.Checked = false;

            if (_existing is null)
            {
                txtName.Text = _seedCategory?.Name ?? "";
                numAmount.Value = 0;
                chkActive.Checked = true;
                Text = "Add Budget";
                btnAdd.Visible = true;
                btnSave.Visible = false;
                btnDelete.Visible = false;
                AcceptButton = btnAdd;
                UpdatePeriodStartVisibility();
                return;
            }

            txtName.Text = _existing.BudgetName;
            numAmount.Value = Clamp(_existing.DefaultBudgetAmount);
            chkActive.Checked = _existing.IsActive;
            cmbFrequency.SelectedItem = ToLabel(_existing.BudgetFrequency);
            if (_existing.PeriodStart is DateTime start)
            {
                dtPeriodStart.Value = start.Date;
                dtPeriodStart.Checked = true;
            }
            Text = "Edit Budget";
            btnAdd.Visible = false;
            btnSave.Visible = true;
            btnDelete.Visible = true;
            btnSave.Location = new Point(12, 12);
            btnDelete.Location = new Point(110, 12);
            AcceptButton = btnSave;
            UpdatePeriodStartVisibility();
        }

        private void OnFrequencyChanged(object? sender, EventArgs e) =>
            UpdatePeriodStartVisibility();

        private void UpdatePeriodStartVisibility()
        {
            var cycle = UsesCycleStart(ParseFrequency(cmbFrequency.SelectedItem?.ToString()));
            lblPeriodStart.Enabled = cycle;
            dtPeriodStart.Enabled = cycle;
            if (!cycle)
                dtPeriodStart.Checked = false;
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

            var frequency = ParseFrequency(cmbFrequency.SelectedItem?.ToString());
            rule = new ExpenseBudgetRule
            {
                Id = newId || _existing is null ? Guid.NewGuid() : _existing.Id,
                BudgetName = txtName.Text.Trim(),
                IncludedCategoryIds = categories,
                BudgetFrequency = frequency,
                DefaultBudgetAmount = numAmount.Value,
                PeriodStart = UsesCycleStart(frequency) && dtPeriodStart.Checked
                    ? dtPeriodStart.Value.Date
                    : null,
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

        private static bool UsesCycleStart(BudgetFrequency frequency) =>
            frequency is BudgetFrequency.Weekly or BudgetFrequency.Biweekly;

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
