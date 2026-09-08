using System.ComponentModel;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class BudgetPeriodEditor : Form
    {
        private BudgetOrchestrator? _orchestrator;
        private ExpenseBudgetHistory? _history;
        private bool _changed;

        public BudgetPeriodEditor()
        {
            InitializeComponent();
        }

        public BudgetPeriodEditor(Guid ruleId)
            : this(new BudgetOrchestrator(), ruleId)
        {
        }

        public BudgetPeriodEditor(BudgetOrchestrator orchestrator, Guid ruleId)
            : this()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            _orchestrator = orchestrator;
            _history = orchestrator.GetActivePeriod(ruleId);
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
                btnClosePeriod.Enabled = false;
                btnRollForward.Enabled = false;
                btnResetStarting.Enabled = false;
                btnTransfer.Enabled = false;
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
            btnClosePeriod.Enabled = open;
            btnRollForward.Enabled = true;
            btnResetStarting.Enabled = open;
            btnTransfer.Enabled = open;
            numStarting.Enabled = open;
            numBudgetAmount.Enabled = open;

            numStarting.ValueChanged += OnEnvelopeChanged;
            numBudgetAmount.ValueChanged += OnEnvelopeChanged;
        }

        private void OnEnvelopeChanged(object? sender, EventArgs e) =>
            UpdateDerived();

        private void UpdateDerived()
        {
            if (_history is null)
                return;

            var remaining = numStarting.Value + Math.Abs(numBudgetAmount.Value) - _history.ActualExpenses;
            txtRemaining.Text = remaining.ToString("c2");
            txtEnding.Text = remaining.ToString("c2");
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

        private void OnResetStarting(object? sender, EventArgs e)
        {
            if (_history is null)
                return;

            if (MessageBox.Show(this, "Set starting balance to $0.00 for this period?", "Reset Starting Balance",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Orchestrator.SetStartingBalance(_history.Id, 0);
            _changed = true;
            _history = Orchestrator.GetActivePeriod(_history.BudgetRuleId);
            Bind();
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

        private void OnClosePeriod(object? sender, EventArgs e)
        {
            if (_history is null)
                return;

            Orchestrator.ClosePeriod(_history.Id);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnRollForward(object? sender, EventArgs e)
        {
            if (_history is null)
                return;

            Orchestrator.RollForward(_history.BudgetRuleId);
            DialogResult = DialogResult.OK;
            Close();
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
