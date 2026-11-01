using THMS.Domain.Finance.Investments;
using THMS.Logic.Finance.Investments;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class InvestmentPositionsDialog : Form
    {
        private readonly InvestmentImportOrchestrator _orchestrator;
        private readonly Guid _accountId;
        private readonly Label _summary = new() { AutoSize = false, Dock = DockStyle.Fill };
        private readonly DataGridView _positions = Grid();
        private readonly DataGridView _income = Grid();
        private readonly DataGridView _review = Grid();
        private readonly ComboBox _kind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        private readonly ThmsButton _classify = new() { Text = "Apply classification" };

        public InvestmentPositionsDialog(InvestmentImportOrchestrator orchestrator, Guid accountId, string accountName)
        {
            _orchestrator = orchestrator;
            _accountId = accountId;
            Text = $"{accountName} positions";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(980, 640);

            _kind.Items.AddRange(
            [
                new KindChoice(InvestmentActionKind.Contribution, "Contribution"),
                new KindChoice(InvestmentActionKind.Distribution, "Distribution"),
                new KindChoice(InvestmentActionKind.Dividend, "Dividend"),
                new KindChoice(InvestmentActionKind.Interest, "Interest"),
                new KindChoice(InvestmentActionKind.Income, "Income"),
                new KindChoice(InvestmentActionKind.Buy, "Buy"),
                new KindChoice(InvestmentActionKind.Sell, "Sell"),
                new KindChoice(InvestmentActionKind.Reinvestment, "Reinvestment"),
                new KindChoice(InvestmentActionKind.Split, "Split")
            ]);
            _kind.SelectedIndex = 0;
            _classify.Click += (_, _) => ClassifySelected();

            var summary = new Panel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(16, 12, 16, 0) };
            _summary.Text = "Loading positions...";
            summary.Controls.Add(_summary);

            var reviewBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(8),
                WrapContents = false
            };
            reviewBar.Controls.Add(_kind);
            reviewBar.Controls.Add(_classify);
            var reviewPage = new Panel { Dock = DockStyle.Fill };
            reviewPage.Controls.Add(_review);
            reviewPage.Controls.Add(reviewBar);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(Page("Positions", _positions));
            tabs.TabPages.Add(Page("Estimated annual income", _income));
            tabs.TabPages.Add(Page("Review", reviewPage));

            var import = new ThmsButton { Text = "Import Fidelity" };
            import.Click += (_, _) => Import();
            var close = new ThmsButton { Text = "Close" };
            close.Click += (_, _) => Close();
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(16, 8, 16, 16),
                MinimumSize = new Size(0, 56)
            };
            buttons.Controls.Add(close);
            buttons.Controls.Add(import);
            CancelButton = close;

            Controls.Add(tabs);
            Controls.Add(summary);
            Controls.Add(buttons);
            Load += (_, _) => Reload();
        }

        private void Import()
        {
            using var dialog = new InvestmentImportDialog(_orchestrator, _accountId);
            dialog.ShowDialog(this);
            Reload();
        }

        private void Reload()
        {
            var snapshot = _orchestrator.Snapshot(_accountId);
            _summary.Text =
                $"Cash {snapshot.Cash:c}    Market value {snapshot.PositionValue:c}    Account value {snapshot.AccountValue:c}    Estimated annual income {snapshot.EstimatedAnnualIncome:c}";
            Bind(_positions,
            [
                Column("Symbol", "Symbol", 90),
                Column("Name", "Name", 220),
                Number("Shares", "Shares", "n4"),
                Money("AverageCost", "Average cost"),
                Money("TotalBasis", "Total basis"),
                Money("Price", "Price"),
                Money("MarketValue", "Market value"),
                Money("EstimatedAnnualIncome", "Est. annual income"),
                DateColumn("ExDate", "Ex-date"),
                DateColumn("PayDate", "Pay date")
            ], snapshot.Positions.ToList());
            Bind(_income,
            [
                DateColumn("Date", "Date"),
                Money("EstimatedAnnualIncome", "Estimated annual income")
            ], snapshot.Income.ToList());

            var review = _orchestrator.ReviewRows(_accountId);
            Bind(_review,
            [
                Column(nameof(ReviewLine.Date), "Date", 90),
                Column(nameof(ReviewLine.Action), "Action", 280),
                Column(nameof(ReviewLine.Symbol), "Symbol", 80),
                Column(nameof(ReviewLine.Kind), "Kind", 110),
                Column(nameof(ReviewLine.Cash), "Cash change", 110),
                Column(nameof(ReviewLine.Shares), "Share change", 110),
                Column(nameof(ReviewLine.Note), "Note", 240)
            ], review.Select(action => new ReviewLine(action)).ToList());
        }

        private void ClassifySelected()
        {
            if (_review.CurrentRow?.DataBoundItem is not ReviewLine line)
            {
                MessageBox.Show(this, "Select a row that is waiting for review.", "Positions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_kind.SelectedItem is not KindChoice choice)
                return;

            try
            {
                _orchestrator.Classify(_accountId, line.Id, choice.Kind);
                Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Positions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static TabPage Page(string title, Control content)
        {
            var page = new TabPage(title) { Padding = new Padding(8) };
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            return page;
        }

        private static DataGridView Grid()
        {
            var grid = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            return grid;
        }

        private static void Bind(DataGridView grid, DataGridViewColumn[] columns, object rows)
        {
            grid.Columns.Clear();
            grid.Columns.AddRange(columns);
            grid.DataSource = rows;
        }

        private static DataGridViewTextBoxColumn Column(string property, string header, int width) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                MinimumWidth = width
            };

        private static DataGridViewTextBoxColumn Number(string property, string header, string format)
        {
            var column = Column(property, header, 90);
            column.DefaultCellStyle.Format = format;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn Money(string property, string header) =>
            Number(property, header, "c2");

        private static DataGridViewTextBoxColumn DateColumn(string property, string header)
        {
            var column = Column(property, header, 90);
            column.DefaultCellStyle.Format = "d";
            return column;
        }

        private sealed record KindChoice(InvestmentActionKind Kind, string Label)
        {
            public override string ToString() => Label;
        }

        private sealed class ReviewLine
        {
            public ReviewLine(InvestmentAction action)
            {
                Id = action.Id;
                Date = action.RunDate.ToShortDateString();
                Action = action.ActionText;
                Symbol = action.Symbol;
                Kind = action.Kind.ToString();
                Cash = action.CashChange?.ToString("c2") ?? "";
                Shares = action.ShareChange?.ToString("n4") ?? "";
                Note = action.Note ?? "";
            }

            public Guid Id { get; }
            public string Date { get; }
            public string Action { get; }
            public string Symbol { get; }
            public string Kind { get; }
            public string Cash { get; }
            public string Shares { get; }
            public string Note { get; }
        }
    }

    internal sealed class InvestmentImportDialog : Form
    {
        private readonly InvestmentImportOrchestrator _orchestrator;
        private readonly Guid _accountId;
        private readonly TextBox _positions = new() { Width = 560 };
        private readonly TextBox _history = new() { Width = 560 };
        private readonly TextBox _messages = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Height = 88, Dock = DockStyle.Top };
        private readonly DataGridView _changes = new()
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            Dock = DockStyle.Fill
        };
        private readonly ThmsButton _apply = new() { Text = "Import", Enabled = false };
        private bool _ready;

        public InvestmentImportDialog(InvestmentImportOrchestrator orchestrator, Guid accountId)
        {
            _orchestrator = orchestrator;
            _accountId = accountId;
            Text = "Import Fidelity";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(860, 560);

            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(16, 16, 16, 8)
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            AddFileRow(fields, 0, "Positions view", _positions);
            AddFileRow(fields, 1, "History", _history);

            _changes.Columns.AddRange(
                TextColumn("Date", "Date"),
                TextColumn("Action", "Action"),
                TextColumn("Symbol", "Symbol"),
                TextColumn("Kind", "Kind"),
                TextColumn("Status", "Status"),
                TextColumn("Cash", "Cash change"),
                TextColumn("Shares", "Share change"),
                TextColumn("Note", "Note"));
            DataGridViewUtil.EnableDoubleBuffering(_changes);

            var preview = new ThmsButton { Text = "Preview" };
            preview.Click += (_, _) => Preview();
            _apply.Click += (_, _) => Commit();
            var cancel = new ThmsButton { Text = "Cancel" };
            cancel.Click += (_, _) => Close();
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(16, 8, 16, 16),
                MinimumSize = new Size(0, 56)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(_apply);
            buttons.Controls.Add(preview);
            CancelButton = cancel;
            AcceptButton = _apply;

            Controls.Add(_changes);
            Controls.Add(_messages);
            Controls.Add(fields);
            Controls.Add(buttons);
        }

        private void AddFileRow(TableLayoutPanel fields, int row, string label, TextBox path)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            fields.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 0, 0) }, 0, row);
            path.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            path.Margin = new Padding(0, 4, 8, 4);
            fields.Controls.Add(path, 1, row);
            var browse = new ThmsButton { Text = "Browse" };
            browse.Click += (_, _) => Browse(path);
            fields.Controls.Add(browse, 2, row);
        }

        private void Browse(TextBox target)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Fidelity download"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                target.Text = dialog.FileName;
                _ready = false;
                _apply.Enabled = false;
            }
        }

        private void Preview()
        {
            try
            {
                var preview = _orchestrator.Preview(
                    _accountId,
                    Blank(_positions.Text),
                    Blank(_history.Text));
                _messages.Text = preview.Messages.Count == 0
                    ? $"{preview.Applied} applied, {preview.Skipped} already imported, {preview.Updated} updated, {preview.Review} waiting for review."
                    : string.Join(Environment.NewLine, preview.Messages)
                        + Environment.NewLine
                        + $"{preview.Applied} applied, {preview.Skipped} already imported, {preview.Updated} updated, {preview.Review} waiting for review.";
                _changes.DataSource = preview.Changes
                    .Select(action => new ChangeLine(action))
                    .ToList();
                _ready = true;
                _apply.Enabled = preview.Changes.Count > 0;
            }
            catch (Exception ex)
            {
                _ready = false;
                _apply.Enabled = false;
                MessageBox.Show(this, ex.Message, "Import Fidelity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Commit()
        {
            if (!_ready)
                return;
            try
            {
                _orchestrator.Commit(_accountId);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Import Fidelity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string? Blank(string text) =>
            string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new() { DataPropertyName = property, HeaderText = header };

        private sealed class ChangeLine
        {
            public ChangeLine(InvestmentAction action)
            {
                Date = action.RunDate.ToShortDateString();
                Action = action.ActionText;
                Symbol = action.Symbol;
                Kind = action.Kind.ToString();
                Status = action.Status.ToString();
                Cash = action.CashChange?.ToString("c2") ?? "";
                Shares = action.ShareChange?.ToString("n4") ?? "";
                Note = action.Note ?? "";
            }

            public string Date { get; }
            public string Action { get; }
            public string Symbol { get; }
            public string Kind { get; }
            public string Status { get; }
            public string Cash { get; }
            public string Shares { get; }
            public string Note { get; }
        }
    }
}
