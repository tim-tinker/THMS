using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionIngestionControl : UserControl
    {
        private readonly TransactionImportOrchestrator _importOrchestrator;
        private readonly PlaidTransactionOrchestrator _plaidTransactionOrchestrator;
        private List<TransactionImportPreview> _filePreviewRows = [];

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
            ConfigureFilePreviewGrid();
        }

        private void ConfigureFilePreviewGrid()
        {
            gridFilePreview.AutoGenerateColumns = false;
            gridFilePreview.Columns.Clear();
            gridFilePreview.Columns.AddRange(
                DateColumn(nameof(TransactionImportPreview.Date), "Date"),
                TextColumn(nameof(TransactionImportPreview.Description), "Description"),
                AmountColumn(nameof(TransactionImportPreview.Amount), "Amount"),
                TextColumn(nameof(TransactionImportPreview.Account), "Account"),
                TextColumn(nameof(TransactionImportPreview.Category), "Category"));
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

        private static DataGridViewTextBoxColumn DateColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "d";
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            return column;
        }

        private static DataGridViewTextBoxColumn AmountColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "c2";
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            return column;
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

        private void OnLoadFiles(object sender, EventArgs e)
        {
            var paths = SplitPaths(txtFilePaths.Text);
            if (paths.Count == 0)
            {
                ShowError("Select one or more .xlsx files.");
                return;
            }

            try
            {
                _filePreviewRows = _importOrchestrator.LoadTransactionsFromFiles(paths);
                gridFilePreview.DataSource = _filePreviewRows;
                SetFileStatus($"Loaded {_filePreviewRows.Count} transaction{(_filePreviewRows.Count == 1 ? "" : "s")}.");
            }
            catch (Exception ex)
            {
                ShowError($"Could not parse the file(s).\n{ex.Message}");
                SetFileStatus("Load failed.");
            }
        }

        private void OnImportFiles(object sender, EventArgs e)
        {
            if (_filePreviewRows.Count == 0)
            {
                ShowError("Load files before importing transactions.");
                return;
            }

            try
            {
                var imported = _importOrchestrator.ImportTransactions(_filePreviewRows);
                SetFileStatus(ImportStatusText.Imported(imported, "transaction", "transactions"));
            }
            catch (Exception ex)
            {
                ShowError($"Import failed.\n{ex.Message}");
                SetFileStatus("Import failed.");
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

        private void SetFileStatus(string message) => lblFileStatus.Text = message;
    }
}
