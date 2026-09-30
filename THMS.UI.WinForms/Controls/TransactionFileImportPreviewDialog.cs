using System.ComponentModel;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class TransactionFileImportPreviewDialog : Form
    {
        private readonly BindingList<TransactionFileImportRow> _rows;
        private readonly DataGridView _grid = new();

        public IReadOnlyList<TransactionFileImportRow> Selected { get; private set; } = [];

        public TransactionFileImportPreviewDialog(string accountName, IReadOnlyList<TransactionFileImportRow> rows)
        {
            _rows = new BindingList<TransactionFileImportRow>(rows.ToList());

            Text = $"Import Transactions — {accountName}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(760, 480);
            Size = new Size(980, 640);

            var header = BuildHeader();
            var buttons = BuildButtons();
            ConfigureGrid();

            Controls.Add(_grid);
            Controls.Add(buttons);
            Controls.Add(header);
            CancelButton = buttons.Controls.OfType<Button>().First(button => button.DialogResult == DialogResult.Cancel);
        }

        private Control BuildHeader()
        {
            var ready = _rows.Count(row => row.Problem is null && !row.IsDuplicate);
            var duplicates = _rows.Count(row => row.IsDuplicate);
            var problems = _rows.Count(row => row.Problem is not null);
            var text = $"{ready} ready to import. {duplicates} look like duplicates and are unchecked. Check a duplicate to import it anyway.";
            if (problems > 0)
                text += $" {problems} could not be read.";

            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(940, 0),
                Padding = new Padding(16, 12, 16, 8)
            };
        }

        private FlowLayoutPanel BuildButtons()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(16, 8, 16, 16)
            };
            var cancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel };
            var import = new ThmsButton { Text = "Import" };
            import.Click += OnImport;
            panel.Controls.Add(cancel);
            panel.Controls.Add(import);
            return panel;
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.AutoGenerateColumns = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.DataError += (_, e) => e.ThrowException = false;
            _grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_grid.IsCurrentCellDirty)
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellBeginEdit += (_, e) =>
            {
                if (e.RowIndex >= 0 && _rows[e.RowIndex].Problem is not null)
                    e.Cancel = true;
            };
            _grid.CellFormatting += OnCellFormatting;
            _grid.DataBindingComplete += (_, _) =>
            {
                for (var i = 0; i < _grid.Rows.Count && i < _rows.Count; i++)
                {
                    if (_rows[i].Problem is not null)
                        _grid.Rows[i].ReadOnly = true;
                }
            };
            DataGridViewUtil.EnableDoubleBuffering(_grid);

            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(TransactionFileImportRow.Import),
                HeaderText = "Import",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });
            _grid.Columns.Add(TextColumn(nameof(TransactionFileImportRow.SourceRow), "Row"));
            _grid.Columns.Add(TextColumn(nameof(TransactionFileImportRow.DateText), "Date"));
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(TransactionFileImportRow.Description),
                HeaderText = "Description",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            _grid.Columns.Add(TextColumn(nameof(TransactionFileImportRow.AmountText), "Amount"));
            _grid.Columns.Add(TextColumn(nameof(TransactionFileImportRow.Status), "Status"));
            _grid.DataSource = _rows;
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;

            var row = _rows[e.RowIndex];
            if (row.Problem is not null)
                e.CellStyle.ForeColor = Color.Gray;
            else if (row.IsDuplicate)
                e.CellStyle.BackColor = Color.FromArgb(255, 243, 205);
        }

        private void OnImport(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            var selected = _rows.Where(row => row.Import && row.Problem is null).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Select at least one transaction to import.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Selected = selected;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                ReadOnly = true
            };
    }
}
