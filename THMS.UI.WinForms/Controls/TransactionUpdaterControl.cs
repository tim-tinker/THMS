using THMS.Data.Stores;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionUpdaterControl : UserControl
    {
        private readonly ITransactionDataStore _transactionStore;
        private readonly IAccountDataStore _accountStore;

        public TransactionUpdaterControl(
            ITransactionDataStore transactionStore,
            IAccountDataStore accountStore)
        {
            InitializeComponent();

            _transactionStore = transactionStore;
            _accountStore = accountStore;
        }

        private void OnRunUpdate(object sender, EventArgs e)
        {
            lblStatus.Text = "Running update...";
            lblStatus.Refresh();

            try
            {
                var orchestrator = new TransactionUpdaterOrchestrator(_transactionStore, _accountStore);
                var result = orchestrator.RunFullUpdate();

                txtSummary.Text =
                    $"Accounts Updated: {result.AccountsUpdated}\r\n" +
                    $"Transactions Imported: {result.TransactionsImported}\r\n" +
                    $"Transfers Detected: {result.TransfersDetected}\r\n" +
                    $"Recurring Rules Updated: {result.RecurringRulesUpdated}\r\n" +
                    $"Forecast Updated: {result.ForecastUpdated}\r\n" +
                    $"Roll-Off Completed: {result.RollOffCompleted}\r\n";

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
