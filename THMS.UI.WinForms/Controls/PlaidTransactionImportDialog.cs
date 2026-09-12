using System.ComponentModel;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class PlaidTransactionImportDialog : Form
    {
        private readonly PlaidTransactionOrchestrator _orchestrator;
        private BindingList<PlaidTransactionViewModel> _rows = [];
        private readonly DataGridView _grid = new();
        private readonly DateTimePicker _dtStart = new();
        private readonly DateTimePicker _dtEnd = new();
        private readonly Label _status = new();
        private readonly ProgressBar _progress = new();
        private readonly Button _btnDownload = new();
        private readonly Button _btnOk = new();
        private readonly Button _btnCancel = new();
        private readonly Button _btnDelete = new();

        public int ImportedCount { get; private set; }

        public PlaidTransactionImportDialog()
            : this(new PlaidTransactionOrchestrator())
        {
        }

        public PlaidTransactionImportDialog(PlaidTransactionOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;

            Text = "Import from Plaid";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(980, 560);
            MinimumSize = new Size(640, 400);

            var heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 36,
                Padding = new Padding(8, 8, 8, 0),
                Text = "Plaid Transactions (Preview)",
                TextAlign = ContentAlignment.MiddleLeft
            };

            _dtStart.Format = DateTimePickerFormat.Short;
            _dtStart.Value = DateTime.Today.AddDays(-30);
            _dtEnd.Format = DateTimePickerFormat.Short;
            _dtEnd.Value = DateTime.Today;
            _btnDownload.Text = "Download";
            _btnDownload.AutoSize = true;
            _btnDownload.Click += OnDownload;

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8, 4, 8, 4),
                WrapContents = false
            };
            toolbar.Controls.Add(new Label
            {
                Text = "From",
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0)
            });
            toolbar.Controls.Add(_dtStart);
            toolbar.Controls.Add(new Label
            {
                Text = "To",
                AutoSize = true,
                Padding = new Padding(8, 6, 0, 0)
            });
            toolbar.Controls.Add(_dtEnd);
            toolbar.Controls.Add(_btnDownload);

            ConfigureGrid();

            _btnDelete.Text = "Delete Row";
            _btnDelete.AutoSize = true;
            _btnDelete.Click += OnDeleteRow;
            _btnOk.Text = "OK";
            _btnOk.AutoSize = true;
            _btnOk.Click += OnImport;
            _btnCancel.Text = "Cancel";
            _btnCancel.DialogResult = DialogResult.Cancel;
            _btnCancel.AutoSize = true;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 48,
                Padding = new Padding(8)
            };
            buttons.Controls.Add(_btnOk);
            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnDelete);

            _status.Dock = DockStyle.Bottom;
            _status.Height = 24;
            _status.Padding = new Padding(8, 0, 8, 0);
            _status.Text = "Choose a date range and click Download.";
            _status.TextAlign = ContentAlignment.MiddleLeft;

            var progressHost = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Padding = new Padding(8, 4, 8, 4)
            };
            _progress.Dock = DockStyle.Fill;
            _progress.Style = ProgressBarStyle.Continuous;
            progressHost.Controls.Add(_progress);

            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(progressHost);
            Controls.Add(buttons);
            Controls.Add(toolbar);
            Controls.Add(heading);
            CancelButton = _btnCancel;
        }

        private void ConfigureGrid()
        {
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = true;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Dock = DockStyle.Fill;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.DataError += (_, e) => e.ThrowException = false;
            _grid.Columns.AddRange(
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
                ReadOnly = true
            };

        private static DataGridViewTextBoxColumn DateColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "d";
            return column;
        }

        private static DataGridViewTextBoxColumn AmountColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "c2";
            return column;
        }

        private async void OnDownload(object? sender, EventArgs e)
        {
            _btnDownload.Enabled = false;
            _status.Text = "Downloading Plaid transactions...";
            try
            {
                var downloaded = await _orchestrator.DownloadNewTransactions(
                    _dtStart.Value.Date,
                    _dtEnd.Value.Date);
                _rows = new BindingList<PlaidTransactionViewModel>(downloaded);
                _grid.DataSource = _rows;
                _status.Text = $"Downloaded {_rows.Count} transaction{(_rows.Count == 1 ? "" : "s")}. Pending rows are skipped on import.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Plaid download failed.\n{ex.Message}", "Import from Plaid",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _status.Text = "Download failed.";
            }
            finally
            {
                _btnDownload.Enabled = true;
            }
        }

        private void OnDeleteRow(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is not PlaidTransactionViewModel row)
            {
                MessageBox.Show(this, "Select a row to delete.", "Import from Plaid",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _rows.Remove(row);
            _status.Text = $"{_rows.Count} transaction{(_rows.Count == 1 ? "" : "s")} remaining.";
        }

        private void OnImport(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            if (_rows.Count == 0)
            {
                MessageBox.Show(this, "Download Plaid transactions before importing.", "Import from Plaid",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true);
            try
            {
                var snapshot = _rows.ToList();
                var progress = new ActionProgress<TransactionImportProgress>(ShowProgress);
                ImportedCount = _orchestrator.ImportTransactions(snapshot, progress);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                SetBusy(false);
                MessageBox.Show(this, $"Plaid import failed.\n{ex.Message}", "Import from Plaid",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowProgress(TransactionImportProgress progress)
        {
            _progress.Maximum = Math.Max(1, progress.Total);
            _progress.Value = Math.Clamp(progress.Completed, 0, _progress.Maximum);
            if (progress.Completed >= progress.Total)
                _status.Text = "Finishing...";
            else if (progress.Total > 0 && progress.Completed == progress.Total - 1)
                _status.Text = "Updating ledger...";
            else
                _status.Text = $"Importing {progress.Completed:N0} of {progress.Total:N0}...";
            _progress.Update();
            Application.DoEvents();
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            _btnOk.Enabled = !busy;
            _btnCancel.Enabled = !busy;
            _btnDelete.Enabled = !busy;
            _btnDownload.Enabled = !busy;
            _grid.Enabled = !busy;
            CancelButton = busy ? null : _btnCancel;
        }
    }
}
