using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class BudgetBalanceTransferDialog : Form
    {
        private BudgetOrchestrator? _orchestrator;
        private Guid _sourceRuleId;
        private ExpenseBudgetRule? _sourceRule;
        private ExpenseBudgetHistory? _sourcePeriod;

        public BudgetBalanceTransferDialog()
        {
            InitializeComponent();
        }

        public BudgetBalanceTransferDialog(Guid sourceRuleId)
            : this(new BudgetOrchestrator(), sourceRuleId)
        {
        }

        public BudgetBalanceTransferDialog(BudgetOrchestrator orchestrator, Guid sourceRuleId)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            _sourceRuleId = sourceRuleId;
            Bind();
        }

        private BudgetOrchestrator Orchestrator =>
            _orchestrator ??= new BudgetOrchestrator();

        private void Bind()
        {
            _sourceRule = Orchestrator.GetRule(_sourceRuleId);
            _sourcePeriod = Orchestrator.GetActivePeriod(_sourceRuleId);
            if (_sourceRule is null || _sourcePeriod is null || _sourcePeriod.IsClosed)
            {
                lblSource.Text = "This budget does not have an open period.";
                cmbTarget.Enabled = false;
                numAmount.Enabled = false;
                btnTransfer.Enabled = false;
                return;
            }

            lblSource.Text =
                $"{_sourceRule.BudgetName}: starting {_sourcePeriod.StartingBalance:c2}, remaining {_sourcePeriod.Remaining:c2}";

            var targets = Orchestrator.GetRules()
                .Where(r => r.Id != _sourceRuleId && r.IsActive)
                .Select(r => new TransferTarget(r, Orchestrator.GetActivePeriod(r.Id)))
                .Where(t => t.Period is { IsClosed: false })
                .ToList();

            var available = Math.Max(0, _sourcePeriod.StartingBalance);
            cmbTarget.DisplayMember = nameof(TransferTarget.Label);
            cmbTarget.DataSource = targets;
            numAmount.Minimum = 0;
            numAmount.Maximum = available;
            numAmount.Value = available;
            var canTransfer = targets.Count > 0 && available > 0;
            cmbTarget.Enabled = canTransfer;
            numAmount.Enabled = canTransfer;
            btnTransfer.Enabled = canTransfer;
            if (available <= 0)
                lblHelp.Text = "Starting balance must be greater than zero to transfer.";
            else if (targets.Count == 0)
                lblHelp.Text = "No other open budgets are available to receive a transfer.";
        }

        private void OnTransfer(object? sender, EventArgs e)
        {
            if (cmbTarget.SelectedItem is not TransferTarget target || _sourcePeriod is null)
            {
                MessageBox.Show(this, "Select a destination budget.", "Transfer Balance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var amount = numAmount.Value;
            if (amount <= 0)
            {
                MessageBox.Show(this, "Enter an amount greater than zero.", "Transfer Balance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (amount > _sourcePeriod.StartingBalance)
            {
                MessageBox.Show(this, "Amount cannot be greater than the starting balance.", "Transfer Balance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sourceName = _sourceRule?.BudgetName ?? "this budget";
            if (MessageBox.Show(this,
                    $"Move {amount:c2} of starting balance from '{sourceName}' to '{target.Rule.BudgetName}'?",
                    "Transfer Balance",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                Orchestrator.TransferBalance(_sourceRuleId, target.Rule.Id, amount);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Transfer Balance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnCancel(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private sealed class TransferTarget
        {
            public TransferTarget(ExpenseBudgetRule rule, ExpenseBudgetHistory? period)
            {
                Rule = rule;
                Period = period;
            }

            public ExpenseBudgetRule Rule { get; }
            public ExpenseBudgetHistory? Period { get; }
            public string Label =>
                Period is null
                    ? Rule.BudgetName
                    : $"{Rule.BudgetName} (starting {Period.StartingBalance:c2})";
        }
    }
}
