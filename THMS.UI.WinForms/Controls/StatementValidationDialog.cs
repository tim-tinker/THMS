using System.Globalization;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class StatementValidationDialog : Form
    {
        public StatementValidationDialog(string accountName, StatementValidation validation)
        {
            ArgumentNullException.ThrowIfNull(validation);
            Text = $"{accountName} validation";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(720, 480);
            ClientSize = new Size(840, 560);

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(12, 12, 12, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = $"Starting balance {validation.PreviousStatementDate:d}: {validation.OpeningBalance:c2}    Statement {validation.StatementDate:d}: {validation.StatementBalance:c2}"
            };

            var chronological = validation.Rows.ToList();
            var dateDescending = true;
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            var date = Column(nameof(StatementValidationRow.Date), "Date", "d");
            date.SortMode = DataGridViewColumnSortMode.Programmatic;
            grid.Columns.Add(date);
            var amount = MoneyColumn(nameof(StatementValidationRow.Amount), "Amount");
            grid.Columns.Add(amount);
            var description = Column(nameof(StatementValidationRow.Description), "Description", format: null);
            description.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns.Add(description);
            var balance = MoneyColumn(nameof(StatementValidationRow.Balance), "Balance");
            grid.Columns.Add(balance);
            // Date and amount are switched to visible-row sizing when the column is added,
            // before this dialog is on screen, so that first width ignores the cell text.
            void FitColumn(DataGridViewColumn column)
            {
                if (!grid.IsHandleCreated || column.Index < 0)
                    return;

                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                grid.AutoResizeColumn(column.Index, DataGridViewAutoSizeColumnMode.AllCells);
                var width = column.GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, fixedHeight: true);
                if (width > column.MinimumWidth)
                    column.MinimumWidth = width;
            }

            void FitContentColumns()
            {
                FitColumn(date);
                FitColumn(amount);
                FitColumn(balance);
            }

            void ApplyDateSort()
            {
                grid.DataSource = (dateDescending
                    ? chronological.AsEnumerable().Reverse()
                    : chronological).ToList();
                date.HeaderCell.SortGlyphDirection = dateDescending ? SortOrder.Descending : SortOrder.Ascending;
            }

            grid.DataBindingComplete += (_, _) => FitContentColumns();
            grid.ColumnHeaderMouseClick += (_, e) =>
            {
                if (e.ColumnIndex != date.Index)
                    return;
                dateDescending = !dateDescending;
                ApplyDateSort();
            };
            Shown += (_, _) => BeginInvoke(FitContentColumns);
            ApplyDateSort();

            var summary = new ValidationSummary(validation);
            void UpdateSelectedSum()
            {
                var sum = DataGridViewUtil.SelectedDataRows(grid)
                    .Select(row => row.DataBoundItem)
                    .OfType<StatementValidationRow>()
                    .Sum(row => row.Amount);
                summary.SetSelectedSum(sum);
            }

            grid.SelectionChanged += (_, _) => UpdateSelectedSum();
            Controls.Add(grid);
            Controls.Add(summary);
            Controls.Add(header);
            UpdateSelectedSum();
            KeyPreview = true;
            KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Escape)
                    return;
                Close();
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
        }

        private sealed class ValidationSummary : Control
        {
            private const TextFormatFlags Flags =
                TextFormatFlags.NoPadding
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.SingleLine
                | TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter;

            private readonly (string Label, string Text, int DecimalIndex, bool Highlight)[] _rows;
            private int _rowHeight;
            private decimal _selectedSum;

            public void SetSelectedSum(decimal sum)
            {
                if (_selectedSum == sum)
                    return;
                _selectedSum = sum;
                Invalidate();
            }

            public ValidationSummary(StatementValidation validation)
            {
                var difference = validation.Difference;
                _rows =
                [
                    Row("Running balance:", validation.EndingBalance, highlight: false),
                    Row("Statement balance:", validation.StatementBalance, highlight: false),
                    Row("Difference:", difference, highlight: difference != 0)
                ];
                Dock = DockStyle.Bottom;
                TabStop = false;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                    true);
                Measure();
            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                Measure();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
                const int padLeft = 12;
                const int gap = 16;
                var labelWidth = 0;
                var beforeWidth = 0;
                foreach (var row in _rows)
                {
                    labelWidth = Math.Max(labelWidth, TextWidth(e.Graphics, row.Label));
                    beforeWidth = Math.Max(beforeWidth, TextWidth(e.Graphics, row.Text[..row.DecimalIndex]));
                }

                var decimalX = padLeft + labelWidth + gap + beforeWidth;
                var y = 8;
                for (var i = 0; i < _rows.Length; i++)
                {
                    var row = _rows[i];
                    TextRenderer.DrawText(
                        e.Graphics,
                        row.Label,
                        Font,
                        new Rectangle(padLeft, y, labelWidth, _rowHeight),
                        ForeColor,
                        Flags);
                    var prefix = TextWidth(e.Graphics, row.Text[..row.DecimalIndex]);
                    var valueWidth = TextWidth(e.Graphics, row.Text);
                    var color = row.Highlight ? ThmsButton.DestructiveBack : ForeColor;
                    TextRenderer.DrawText(
                        e.Graphics,
                        row.Text,
                        Font,
                        new Rectangle(decimalX - prefix, y, valueWidth + 2, _rowHeight),
                        color,
                        Flags);
                    if (i == _rows.Length - 1)
                    {
                        var selected = $"Selected Sum: {_selectedSum:c2}";
                        var selectedWidth = TextWidth(e.Graphics, selected);
                        var right = ClientSize.Width - padLeft;
                        TextRenderer.DrawText(
                            e.Graphics,
                            selected,
                            Font,
                            new Rectangle(right - selectedWidth, y, selectedWidth + 2, _rowHeight),
                            ForeColor,
                            Flags);
                    }

                    y += _rowHeight;
                }
            }

            private void Measure()
            {
                _rowHeight = Font.Height + 6;
                Height = 8 + _rows.Length * _rowHeight + 10;
                Invalidate();
            }

            private int TextWidth(Graphics graphics, string text) =>
                TextRenderer.MeasureText(graphics, text, Font, Size.Empty, Flags).Width;

            private static (string Label, string Text, int DecimalIndex, bool Highlight) Row(
                string label,
                decimal value,
                bool highlight)
            {
                var text = value.ToString("c2");
                var separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;
                var index = text.LastIndexOf(separator, StringComparison.Ordinal);
                return (label, text, index < 0 ? text.Length : index, highlight);
            }
        }

        private static DataGridViewTextBoxColumn Column(string property, string header, string? format)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            if (format is not null)
                column.DefaultCellStyle.Format = format;
            return column;
        }

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
        {
            var column = Column(property, header, "c2");
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }
    }
}
