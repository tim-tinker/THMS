using System.ComponentModel;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public sealed class TransactionImportPreviewDialog : Form
    {
        private readonly TransactionImportOrchestrator _orchestrator;
        private readonly BindingList<TransactionImportPreview> _rows;
        private readonly DataGridView _grid = new();
        private readonly Label _status = new();
        private readonly ProgressBar _progress = new();
        private readonly Button _btnOk = new();
        private readonly Button _btnCancel = new();
        private readonly Button _btnDelete = new();

        public int ImportedCount { get; private set; }

        public TransactionImportPreviewDialog(IList<TransactionImportPreview> rows)
            : this(rows, new TransactionImportOrchestrator())
        {
        }

        public TransactionImportPreviewDialog(
            IList<TransactionImportPreview> rows,
            TransactionImportOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            _rows = new BindingList<TransactionImportPreview>(rows.ToList());

            Text = "Import Transactions";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(980, 520);
            MinimumSize = new Size(640, 360);

            var heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 36,
                Padding = new Padding(8, 8, 8, 0),
                Text = "Transactions to Import (Preview)",
                TextAlign = ContentAlignment.MiddleLeft
            };

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
                Padding = new Padding(8),
                WrapContents = false
            };
            buttons.Controls.Add(_btnOk);
            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnDelete);

            _status.Dock = DockStyle.Bottom;
            _status.Height = 24;
            _status.Padding = new Padding(8, 0, 8, 0);
            _status.Text = $"{_rows.Count:N0} transaction{(_rows.Count == 1 ? "" : "s")} loaded. Edit cells or delete rows, then click OK to import.";
            _status.TextAlign = ContentAlignment.MiddleLeft;

            var progressHost = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                Padding = new Padding(8, 4, 8, 4)
            };
            _progress.Dock = DockStyle.Fill;
            _progress.Minimum = 0;
            _progress.Maximum = Math.Max(1, _rows.Count);
            _progress.Style = ProgressBarStyle.Continuous;
            progressHost.Controls.Add(_progress);

            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(progressHost);
            Controls.Add(buttons);
            Controls.Add(heading);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void ConfigureGrid()
        {
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = true;
            _grid.AutoGenerateColumns = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.Dock = DockStyle.Fill;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.DataError += (_, e) => e.ThrowException = false;
            _grid.Columns.AddRange(
                DateColumn(nameof(TransactionImportPreview.Date), "Date"),
                TextColumn(nameof(TransactionImportPreview.Description), "Description", readOnly: false),
                AmountColumn(nameof(TransactionImportPreview.Amount), "Amount"),
                TextColumn(nameof(TransactionImportPreview.Account), "Account", readOnly: true),
                TextColumn(nameof(TransactionImportPreview.Category), "Category", readOnly: false));
            _grid.DataSource = _rows;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header, bool readOnly) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = readOnly
            };

        private static DataGridViewTextBoxColumn DateColumn(string property, string header)
        {
            var column = TextColumn(property, header, readOnly: false);
            column.DefaultCellStyle.Format = "d";
            return column;
        }

        private static DataGridViewTextBoxColumn AmountColumn(string property, string header)
        {
            var column = TextColumn(property, header, readOnly: false);
            column.DefaultCellStyle.Format = "c2";
            return column;
        }

        private void OnDeleteRow(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is not TransactionImportPreview row)
            {
                MessageBox.Show(this, "Select a row to delete.", "Import Transactions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _rows.Remove(row);
            _status.Text = $"{_rows.Count:N0} transaction{(_rows.Count == 1 ? "" : "s")} remaining.";
        }

        private void OnImport(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            if (_rows.Count == 0)
            {
                MessageBox.Show(this, "There are no transactions to import.", "Import Transactions",
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
                MessageBox.Show(this, $"Import failed.\n{ex.Message}", "Import Transactions",
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
            _grid.Enabled = !busy;
            CancelButton = busy ? null : _btnCancel;
        }
    }

    internal sealed class ActionProgress<T>(Action<T> action) : IProgress<T>
    {
        public void Report(T value) => action(value);
    }
}
