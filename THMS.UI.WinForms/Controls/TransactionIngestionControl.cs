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
        private List<PlaidTransactionViewModel> _plaidPreviewRows = [];

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
            ConfigurePlaidPreviewGrid();
            dtStart.Value = DateTime.Today.AddDays(-30);
            dtEnd.Value = DateTime.Today;
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

        private void ConfigurePlaidPreviewGrid()
        {
            gridPlaidPreview.AutoGenerateColumns = false;
            gridPlaidPreview.Columns.Clear();
            gridPlaidPreview.Columns.AddRange(
                DateColumn(nameof(PlaidTransactionViewModel.Date), "Date"),
                TextColumn(nameof(PlaidTransactionViewModel.Description), "Description"),
                AmountColumn(nameof(PlaidTransactionViewModel.Amount), "Amount"),
                TextColumn(nameof(PlaidTransactionViewModel.Account), "Account"),
                TextColumn(nameof(PlaidTransactionViewModel.Category), "Category"),
                new DataGridViewCheckBoxColumn
                {
                    DataPropertyName = nameof(PlaidTransactionViewModel.Pending),
                    HeaderText = "Pending",
                    Name = nameof(PlaidTransactionViewModel.Pending),
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                });
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
                SetFileStatus($"Imported {imported} transaction{(imported == 1 ? "" : "s")}.");
            }
            catch (Exception ex)
            {
                ShowError($"Import failed.\n{ex.Message}");
                SetFileStatus("Import failed.");
            }
        }

        private async void OnDownloadPlaid(object sender, EventArgs e)
        {
            btnDownloadPlaid.Enabled = false;
            SetPlaidStatus("Downloading Plaid transactions...");
            try
            {
                _plaidPreviewRows = await _plaidTransactionOrchestrator.DownloadNewTransactions(
                    dtStart.Value.Date,
                    dtEnd.Value.Date);
                gridPlaidPreview.DataSource = _plaidPreviewRows;
                SetPlaidStatus($"Downloaded {_plaidPreviewRows.Count} Plaid transaction{(_plaidPreviewRows.Count == 1 ? "" : "s")}.");
            }
            catch (Exception ex)
            {
                ShowError($"Plaid download failed.\n{ex.Message}");
                SetPlaidStatus("Download failed.");
            }
            finally
            {
                btnDownloadPlaid.Enabled = true;
            }
        }

        private void OnImportPlaid(object sender, EventArgs e)
        {
            if (_plaidPreviewRows.Count == 0)
            {
                ShowError("Download Plaid transactions before importing.");
                return;
            }

            try
            {
                var imported = _plaidTransactionOrchestrator.ImportTransactions(_plaidPreviewRows);
                SetPlaidStatus($"Imported {imported} Plaid transaction{(imported == 1 ? "" : "s")}.");
            }
            catch (Exception ex)
            {
                ShowError($"Plaid import failed.\n{ex.Message}");
                SetPlaidStatus("Import failed.");
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

        private void SetPlaidStatus(string message) => lblPlaidStatus.Text = message;
    }
}
