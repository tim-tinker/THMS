using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionUpdaterControl : UserControl
    {

        public TransactionUpdaterControl()
        {
            InitializeComponent();
        }

        private void OnRunUpdate(object sender, EventArgs e)
        {
            lblStatus.Text = "Running update...";
            lblStatus.Refresh();

            try
            {
                var orchestrator = new TransactionUpdaterOrchestrator();
                var result = orchestrator.RunLedgerUpdate();

                txtSummary.Text =
                    $"Accounts Updated: {result.AccountsUpdated}\r\n" +
                    $"Transactions Imported: {result.TransactionsImported}\r\n" +
                    $"Transfers Detected: {result.TransfersDetected}\r\n" +
                    $"Recurring Rules Updated: {result.RecurringRulesUpdated}\r\n" +
                    $"Rules Reconciled: {result.ForecastUpdated}\r\n";

                lblStatus.Text = "Update complete.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error during update.";
                txtSummary.Text = ex.ToString();
            }
        }

        private void OnClearSummary(object sender, EventArgs e)
        {
            txtSummary.Clear();
            lblStatus.Text = "Ready.";
        }
    }
}
