using System.ComponentModel;
using THMS.Logic.Orchestrators;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms.Controls
{
    public sealed class EvChargeSessionImportPreviewDialog : Form
    {
        private readonly EvChargeSessionImportOrchestrator _orchestrator;
        private readonly BindingList<EvChargeSessionImportPreview> _rows;
        private readonly DataGridView _grid = new();
        private readonly Label _status = new();
        private readonly Button _btnOk = new();
        private readonly Button _btnCancel = new();
        private readonly Button _btnDelete = new();

        public int ImportedCount { get; private set; }

        public EvChargeSessionImportPreviewDialog(IList<EvChargeSessionImportPreview> rows)
            : this(rows, new EvChargeSessionImportOrchestrator())
        {
        }

        public EvChargeSessionImportPreviewDialog(
            IList<EvChargeSessionImportPreview> rows,
            EvChargeSessionImportOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            _rows = new BindingList<EvChargeSessionImportPreview>(rows.ToList());

            Text = "Import EV Charge Sessions";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(1100, 520);
            MinimumSize = new Size(720, 360);

            var heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 36,
                Padding = new Padding(8, 8, 8, 0),
                Text = "EV Charge Sessions to Import (Preview)",
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
            _status.Text = $"{_rows.Count:N0} session{(_rows.Count == 1 ? "" : "s")} loaded. Edit cells or delete rows, then click OK to import.";
            _status.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(_grid);
            Controls.Add(_status);
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
                DateTimeColumn(nameof(EvChargeSessionImportPreview.StartTime), "Start"),
                DateTimeColumn(nameof(EvChargeSessionImportPreview.EndTime), "End"),
                TextColumn(nameof(EvChargeSessionImportPreview.VehicleName), "Vehicle", readOnly: true),
                TextColumn(nameof(EvChargeSessionImportPreview.ChargeType), "Type", readOnly: true),
                TextColumn(nameof(EvChargeSessionImportPreview.Charger), "Charger", readOnly: true),
                NumberColumn(nameof(EvChargeSessionImportPreview.OdometerMiles), "Odometer", "N1"),
                NumberColumn(nameof(EvChargeSessionImportPreview.StartSoc), "Start SOC", "0'%'"),
                NumberColumn(nameof(EvChargeSessionImportPreview.EndSoc), "End SOC", "0'%'"),
                NumberColumn(nameof(EvChargeSessionImportPreview.KwhAdded), "Charge kWh", "N2"),
                NumberColumn(nameof(EvChargeSessionImportPreview.KwhDrawn), "Drawn kWh", "N2"),
                AmountColumn(nameof(EvChargeSessionImportPreview.SessionCost), "Cost"),
                NumberColumn(nameof(EvChargeSessionImportPreview.LastOdometer), "Last Odo", "N1"),
                NumberColumn(nameof(EvChargeSessionImportPreview.LastSoc), "Last SOC", "0'%'"));
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

        private static DataGridViewTextBoxColumn DateTimeColumn(string property, string header)
        {
            var column = TextColumn(property, header, readOnly: false);
            column.DefaultCellStyle.Format = "g";
            return column;
        }

        private static DataGridViewTextBoxColumn NumberColumn(string property, string header, string format)
        {
            var column = TextColumn(property, header, readOnly: false);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.DefaultCellStyle.Format = format;
            return column;
        }

        private static DataGridViewTextBoxColumn AmountColumn(string property, string header)
        {
            var column = TextColumn(property, header, readOnly: false);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.DefaultCellStyle.Format = "c2";
            return column;
        }

        private void OnDeleteRow(object? sender, EventArgs e)
        {
            if (_grid.CurrentRow?.DataBoundItem is not EvChargeSessionImportPreview row)
            {
                MessageBox.Show(this, "Select a row to delete.", "Import EV Charge Sessions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _rows.Remove(row);
            _status.Text = $"{_rows.Count:N0} session{(_rows.Count == 1 ? "" : "s")} remaining.";
        }

        private void OnImport(object? sender, EventArgs e)
        {
            _grid.EndEdit();
            if (_rows.Count == 0)
            {
                MessageBox.Show(this, "There are no charge sessions to import.", "Import EV Charge Sessions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true);
            try
            {
                ImportedCount = _orchestrator.ImportSessions(_rows.ToList());
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                SetBusy(false);
                MessageBox.Show(this, $"Import failed.\n{ex.Message}", "Import EV Charge Sessions",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
}
