using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class FinanceDiagnosticsControl : UserControl
    {
        private readonly AccountDiagnosticsOrchestrator _accounts;
        private readonly TransactionDiagnosticsOrchestrator _transactions;
        private readonly SplitDiagnosticsOrchestrator _splits;
        private readonly LoanDiagnosticsOrchestrator _loans;
        private readonly ForecastDiagnosticsOrchestrator _forecast;

        public FinanceDiagnosticsControl()
            : this(
                new AccountDiagnosticsOrchestrator(),
                new TransactionDiagnosticsOrchestrator(),
                new SplitDiagnosticsOrchestrator(),
                new LoanDiagnosticsOrchestrator(),
                new ForecastDiagnosticsOrchestrator())
        {
        }

        public FinanceDiagnosticsControl(
            AccountDiagnosticsOrchestrator accounts,
            TransactionDiagnosticsOrchestrator transactions,
            SplitDiagnosticsOrchestrator splits,
            LoanDiagnosticsOrchestrator loans,
            ForecastDiagnosticsOrchestrator forecast)
        {
            _accounts = accounts;
            _transactions = transactions;
            _splits = splits;
            _loans = loans;
            _forecast = forecast;
            InitializeComponent();
        }

        private void OnRunAccountDiagnostics(object sender, EventArgs e) =>
            RunGroup("Account Diagnostics", txtAccounts, () => _accounts.Run());

        private void OnRunTransactionDiagnostics(object sender, EventArgs e) =>
            RunGroup("Transaction Diagnostics", txtTransactions, () => _transactions.Run());

        private void OnRunSplitDiagnostics(object sender, EventArgs e) =>
            RunGroup("Split Diagnostics", txtSplits, () => _splits.Run());

        private void OnRunLoanDiagnostics(object sender, EventArgs e) =>
            RunGroup("Loan Diagnostics", txtLoans, () => _loans.Run());

        private void OnRunForecastDiagnostics(object sender, EventArgs e) =>
            RunGroup("Forecast Diagnostics", txtForecast, () => _forecast.Run());

        private void RunGroup(string name, TextBox output, Func<List<string>> run)
        {
            try
            {
                var findings = run();
                output.Text = string.Join(Environment.NewLine, findings);
                SetStatus($"{name} complete ({findings.Count} line{(findings.Count == 1 ? "" : "s")}).");
            }
            catch (Exception ex)
            {
                var message = $"{name} failed.\n{ex.Message}";
                ShowError(message);
                output.Text = message;
                SetStatus($"{name} failed.");
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(FindForm(), message, "Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetStatus(string message) => lblStatus.Text = message;
    }
}
