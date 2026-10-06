using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class BalanceProjectionDialog : Form
    {
        public BalanceProjectionDialog(string accountName, BalanceProjection projection)
        {
            ArgumentNullException.ThrowIfNull(projection);
            Text = $"{accountName} projection";
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
                Text = $"Balance as of {projection.AsOf:d}: {projection.OpeningBalance:c2}    Through {projection.Through:d}"
            };

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
                MultiSelect = false,
                DataSource = projection.Rows.ToList()
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            grid.Columns.Add(Column(nameof(BalanceProjectionRow.Date), "Date", "d"));
            grid.Columns.Add(MoneyColumn(nameof(BalanceProjectionRow.Amount), "Amount"));
            var description = Column(nameof(BalanceProjectionRow.Description), "Description", format: null);
            description.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns.Add(description);
            grid.Columns.Add(MoneyColumn(nameof(BalanceProjectionRow.Balance), "Balance"));

            var close = new ThmsButton { Text = "Close", Margin = new Padding(0, 4, 12, 12) };
            close.Click += (_, _) => Close();
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(12, 8, 0, 4)
            };
            buttons.Controls.Add(close);

            Controls.Add(grid);
            Controls.Add(buttons);
            Controls.Add(header);
            AcceptButton = close;
            CancelButton = close;
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
