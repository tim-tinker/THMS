using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionIngestionControl : UserControl
    {
        private readonly TransactionImportOrchestrator _importOrchestrator;
        private readonly PlaidTransactionOrchestrator _plaidTransactionOrchestrator;

        public TransactionIngestionControl()
            : this(new TransactionImportOrchestrator(), new PlaidTransactionOrchestrator())
        {
        }

        public TransactionIngestionControl(
            TransactionImportOrchestrator importOrchestrator,
            PlaidTransactionOrchestrator plaidTransactionOrchestrator)
        {
            _importOrchestrator = importOrchestrator;
            _plaidTransactionOrchestrator = plaidTransactionOrchestrator;
            InitializeComponent();
        }

        private void OnBrowseFiles(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Select transaction spreadsheets",
                Multiselect = true
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            txtFilePaths.Text = string.Join("; ", dialog.FileNames);
        }

        private void OnImportFiles(object sender, EventArgs e)
        {
            var paths = SplitPaths(txtFilePaths.Text);
            if (paths.Count == 0)
            {
                ShowError("Select one or more .xlsx files.");
                return;
            }

            try
            {
                AppStatus.Set("Importing transactions...", busy: true);
                var rows = _importOrchestrator.LoadTransactionsFromFiles(paths);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    ShowError("The selected file(s) did not contain any transactions.");
                    return;
                }

                var imported = _importOrchestrator.ImportTransactions(rows, AppStatus.ForImport());
                AppStatus.Set(ImportStatusText.Imported(imported, "transaction", "transactions"));
            }
            catch (Exception ex)
            {
                ShowError($"Could not parse the file(s).\n{ex.Message}");
                AppStatus.Set("Import failed.");
            }
        }

        private async void OnSyncPlaid(object sender, EventArgs e)
        {
            btnSyncPlaid.Enabled = false;
            AppStatus.Set("Syncing Plaid...", busy: true);
            try
            {
                var progress = new Progress<PlaidSyncProgress>(AppStatus.Report);
                var result = await _plaidTransactionOrchestrator.SyncIncrementalAsync(progress: progress);
                AppStatus.Set(result.Summary);
            }
            catch (Exception ex)
            {
                ShowError($"Plaid sync failed.\n{ex.Message}");
                AppStatus.Set("Plaid sync failed.");
            }
            finally
            {
                btnSyncPlaid.Enabled = true;
            }
        }

        private static List<string> SplitPaths(string text)
        {
            return text
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(FindForm(), message, "Transactions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
