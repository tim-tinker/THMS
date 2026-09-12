using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class CashFlowForecastControl : UserControl
    {
        private PlanningOrchestrator _orchestrator = new();
        private Func<PlannedPaymentView?> _selectedPayment = () => null;

        public event EventHandler? DataChanged;

        public CashFlowForecastControl()
        {
            InitializeComponent();
        }

        public void Bind(PlanningOrchestrator orchestrator, Func<PlannedPaymentView?> selectedPayment)
        {
            _orchestrator = orchestrator;
            _selectedPayment = selectedPayment;
            RefreshForecast();
        }

        public void RefreshForecast()
        {
            var until = _orchestrator.SuggestPlanUntil();
            var planned = _orchestrator.GetPlannedPaymentViews();
            if (planned.Count > 0)
            {
                var last = planned.Max(p => p.PlannedDate);
                if (last > until)
                    until = last;
            }

            var forecast = _orchestrator.ComputeCashFlow(until);
            txtCurrent.Text = forecast.CurrentBalance.ToString("c2");
            txtNextPayday.Text = FormatBalance(forecast.ForecastedBalanceNextPayday, forecast.NextPayday);
            txtTwoPaydays.Text = FormatBalance(forecast.ForecastedBalanceTwoPaydays, forecast.SecondPayday);
            txtAfterPlanned.Text = forecast.ForecastedBalanceAfterPlanned.ToString("c2");
        }

        private static string FormatBalance(decimal amount, DateTime? date) =>
            date is DateTime payday ? $"{amount:c2}  ({payday:d})" : amount.ToString("c2");

        private void OnRefresh(object sender, EventArgs e) => RefreshForecast();

        private void OnCommit(object sender, EventArgs e)
        {
            try
            {
                var committed = _orchestrator.CommitDuePlannedPayments();
                RefreshForecast();
                DataChanged?.Invoke(this, EventArgs.Empty);
                MessageBox.Show(
                    FindForm(),
                    committed == 0
                        ? "No due planned payments to commit. Future-dated items stay in the plan."
                        : $"Committed {committed} planned payment{(committed == 1 ? "" : "s")} to the ledger.",
                    "Planning Center",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError($"Commit failed.\n{ex.Message}");
            }
        }

        private void OnReconcile(object sender, EventArgs e)
        {
            var selected = _selectedPayment();
            if (selected is null)
            {
                ShowError("Select a planned payment to reconcile.");
                return;
            }

            using var dialog = new ManualReconcileDialog(selected);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _orchestrator.ReconcileManualPayment(selected.Id, dialog.PostedDate);
                RefreshForecast();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ShowError($"Reconcile failed.\n{ex.Message}");
            }
        }

        private void ShowError(string message) =>
            MessageBox.Show(FindForm(), message, "Planning Center", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
