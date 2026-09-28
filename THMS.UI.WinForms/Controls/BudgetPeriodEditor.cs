using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class BudgetPeriodEditor : Form
    {
        private BudgetOrchestrator? _orchestrator;
        private ExpenseBudgetHistory? _history;
        private Guid _ruleId;
        private bool _changed;

        public BudgetPeriodEditor()
        {
            InitializeComponent();
        }

        public BudgetPeriodEditor(Guid ruleId)
            : this(new BudgetOrchestrator(), ruleId)
        {
        }

        public BudgetPeriodEditor(BudgetOrchestrator orchestrator, Guid ruleId, Guid? historyId = null)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            _ruleId = ruleId;
            _history = historyId is Guid id
                ? orchestrator.GetPeriod(id) ?? orchestrator.GetActivePeriod(ruleId)
                : orchestrator.GetActivePeriod(ruleId);
            Bind();
        }

        private BudgetOrchestrator Orchestrator =>
            _orchestrator ??= new BudgetOrchestrator();

        private void Bind()
        {
            numStarting.ValueChanged -= OnEnvelopeChanged;
            numBudgetAmount.ValueChanged -= OnEnvelopeChanged;

            if (_history is null)
            {
                lblPeriod.Text = "No active period.";
                btnSave.Enabled = false;
                btnTransfer.Enabled = false;
                btnEditBudget.Enabled = _ruleId != Guid.Empty;
                numStarting.Enabled = false;
                numBudgetAmount.Enabled = false;
                return;
            }

            var open = !_history.IsClosed;
            lblPeriod.Text = $"{_history.PeriodStart:d} – {_history.PeriodEnd:d}";
            numStarting.Value = Clamp(_history.StartingBalance);
            txtActual.Text = _history.ActualExpenses.ToString("c2");
            txtRecommended.Text = _history.RecommendedAmount.ToString("c2");
            numBudgetAmount.Value = Clamp(_history.BudgetAmount);
            UpdateDerived();
            btnSave.Enabled = open;
            btnEditBudget.Enabled = true;
            numStarting.Enabled = open;
            numBudgetAmount.Enabled = open;
            UpdateTransferEnabled();

            numStarting.ValueChanged += OnEnvelopeChanged;
            numBudgetAmount.ValueChanged += OnEnvelopeChanged;
        }

        private void OnEnvelopeChanged(object? sender, EventArgs e)
        {
            UpdateDerived();
            UpdateTransferEnabled();
        }

        private void UpdateDerived()
        {
            if (_history is null)
                return;

            var remaining = numStarting.Value + Math.Abs(numBudgetAmount.Value) - _history.ActualExpenses;
            txtRemaining.Text = remaining.ToString("c2");
        }

        private void UpdateTransferEnabled() =>
            btnTransfer.Enabled = _history is { IsClosed: false } && numStarting.Value > 0;

        private void OnEditBudget(object? sender, EventArgs e)
        {
            var rule = Orchestrator.GetRule(_history?.BudgetRuleId ?? _ruleId);
            if (rule is null)
                return;

            using var editor = new BudgetRuleEditor(Orchestrator, rule);
            if (editor.ShowDialog(this) != DialogResult.OK)
                return;

            _changed = true;
            var ruleId = _history?.BudgetRuleId ?? _ruleId;
            if (Orchestrator.GetRule(ruleId) is null)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _history = _history is null
                ? Orchestrator.GetActivePeriod(ruleId)
                : Orchestrator.GetPeriod(_history.Id) ?? Orchestrator.GetActivePeriod(ruleId);
            Bind();
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_history is null)
                return;

            _history.StartingBalance = numStarting.Value;
            _history.BudgetAmount = numBudgetAmount.Value;
            Orchestrator.SavePeriod(_history);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnTransfer(object? sender, EventArgs e)
        {
            if (_history is null)
                return;

            using var dialog = new BudgetBalanceTransferDialog(Orchestrator, _history.BudgetRuleId);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _changed = true;
            _history = Orchestrator.GetActivePeriod(_history.BudgetRuleId);
            Bind();
        }

        private void OnClose(object? sender, EventArgs e)
        {
            if (_changed)
                DialogResult = DialogResult.OK;
            else if (DialogResult == DialogResult.None)
                DialogResult = DialogResult.Cancel;
            Close();
        }

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
