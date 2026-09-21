using System.ComponentModel;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms.Controls
{
    public abstract class ImportPreviewDialog<TRow> : Form
    {
        private readonly Label _status = new();
        private readonly ProgressBar _progress = new();
        private readonly Panel _progressHost = new();
        private readonly ThmsButton _btnOk = new();
        private readonly ThmsButton _btnCancel = new();
        private readonly ThmsButton _btnDelete = new() { Destructive = true };

        protected ImportPreviewDialog(IList<TRow> rows)
        {
            Rows = new BindingList<TRow>(rows.ToList());
            Text = WindowTitle;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(WindowWidth, 520);
            MinimumSize = new Size(640, 360);

            var heading = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Height = 36,
                Padding = new Padding(8, 8, 8, 0),
                Text = Heading,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ConfigureGrid();
            Grid.DataSource = Rows;

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
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12, 12, 12, 16),
                WrapContents = false
            };
            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnDelete);
            buttons.Controls.Add(_btnOk);

            _status.Dock = DockStyle.Bottom;
            _status.Height = 24;
            _status.Padding = new Padding(8, 0, 8, 0);
            _status.Text = ImportStatusText.Loaded(Rows.Count, Singular, Plural);
            _status.TextAlign = ContentAlignment.MiddleLeft;

            _progressHost.Dock = DockStyle.Bottom;
            _progressHost.Height = 28;
            _progressHost.Padding = new Padding(8, 4, 8, 4);
            _progressHost.Visible = false;
            _progress.Dock = DockStyle.Fill;
            _progress.Minimum = 0;
            _progress.Maximum = Math.Max(1, Rows.Count);
            _progress.Style = ProgressBarStyle.Continuous;
            _progressHost.Controls.Add(_progress);

            Controls.Add(Grid);
            Controls.Add(_status);
            Controls.Add(_progressHost);
            Controls.Add(buttons);
            Controls.Add(heading);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        protected BindingList<TRow> Rows { get; }
        protected DataGridView Grid { get; } = new();

        public ImportResult Result { get; private set; } = ImportResult.Empty;
        public int ImportedCount => Result.Count;

        protected abstract string WindowTitle { get; }
        protected abstract string Heading { get; }
        protected abstract string Singular { get; }
        protected abstract string Plural { get; }
        protected virtual int WindowWidth => 980;

        protected abstract void ConfigureGrid();
        protected abstract ImportResult ImportRows(IReadOnlyList<TRow> rows, IProgress<ImportProgress> progress);

        protected void PrepareGrid()
        {
            Grid.AllowUserToAddRows = false;
            Grid.AllowUserToDeleteRows = true;
            Grid.AutoGenerateColumns = false;
            Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            Grid.Dock = DockStyle.Fill;
            Grid.EditMode = DataGridViewEditMode.EditOnEnter;
            Grid.RowHeadersVisible = false;
            Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            Grid.DataError += (_, e) => e.ThrowException = false;
        }

        protected static DataGridViewTextBoxColumn TextColumn(string property, string header, bool readOnly = false) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = readOnly
            };

        protected static DataGridViewTextBoxColumn DateColumn(string property, string header, string format = "d")
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = format;
            return column;
        }

        protected static DataGridViewTextBoxColumn NumberColumn(string property, string header, string format)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.DefaultCellStyle.Format = format;
            return column;
        }

        private void OnDeleteRow(object? sender, EventArgs e)
        {
            if (Grid.CurrentRow?.DataBoundItem is not TRow row)
            {
                MessageBox.Show(this, "Select a row to delete.", WindowTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Rows.Remove(row);
            _status.Text = ImportStatusText.Remaining(Rows.Count, Singular, Plural);
        }

        private void OnImport(object? sender, EventArgs e)
        {
            Grid.EndEdit();
            if (Rows.Count == 0)
            {
                MessageBox.Show(this, $"There are no {Plural} to import.", WindowTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true);
            _progressHost.Visible = true;
            try
            {
                Result = ImportRows(Rows.ToList(), new ActionProgress<ImportProgress>(ShowProgress));
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                SetBusy(false);
                _progressHost.Visible = false;
                MessageBox.Show(this, $"Import failed.\n{ex.Message}", WindowTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowProgress(ImportProgress progress)
        {
            _progress.Maximum = Math.Max(1, progress.Total);
            _progress.Value = Math.Clamp(progress.Completed, 0, _progress.Maximum);
            _status.Text = ImportStatusText.Importing(progress);
            _progress.Update();
            Application.DoEvents();
        }

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            _btnOk.Enabled = !busy;
            _btnCancel.Enabled = !busy;
            _btnDelete.Enabled = !busy;
            Grid.Enabled = !busy;
            CancelButton = busy ? null : _btnCancel;
        }
    }

    internal sealed class ActionProgress<T>(Action<T> action) : IProgress<T>
    {
        public void Report(T value) => action(value);
    }
}
