using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class InvestmentColumnMapDialog : Form
    {
        private const string None = "(none)";
        private const string MatchAccounts = "Match account numbers in the file";

        private readonly InvestmentImportFile _file;
        private readonly IReadOnlyList<string> _headers;
        private readonly bool _choosePortfolio;
        private readonly ComboBox _portfolio = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
        private readonly SplitContainer _split = new() { Orientation = Orientation.Horizontal, Dock = DockStyle.Fill };
        private readonly List<MappedField> _fields;
        private bool _splitReady;

        public InvestmentPositionColumnMap? PositionMap { get; private set; }
        public InvestmentHistoryColumnMap? HistoryMap { get; private set; }
        public Guid? TargetAccountId { get; private set; }

        public InvestmentColumnMapDialog(
            InvestmentImportFile file,
            InvestmentFileSheet sheet,
            InvestmentPositionColumnMap? positionMap,
            InvestmentHistoryColumnMap? historyMap,
            IReadOnlyList<InvestmentAccount> portfolios,
            Guid preferredAccountId,
            bool choosePortfolio)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            _file = file;
            _headers = sheet.Headers;
            _choosePortfolio = choosePortfolio && file == InvestmentImportFile.Positions;
            _fields = file == InvestmentImportFile.Positions
                ? PositionFields(positionMap ?? new InvestmentPositionColumnMap())
                : HistoryFields(historyMap ?? new InvestmentHistoryColumnMap());

            Text = file == InvestmentImportFile.Positions ? "Import positions — map columns" : "Import history — map columns";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(760, 560);
            Size = new Size(980, 720);

            if (_choosePortfolio)
                FillPortfolios(portfolios, preferredAccountId);

            SplitContainerUtil.MakeSplitterVisible(_split);
            _split.Panel1.Controls.Add(BuildMapping());
            _split.Panel2.Controls.Add(BuildFileGrid(sheet));
            var buttons = BuildButtons();
            Controls.Add(_split);
            Controls.Add(buttons);
            CancelButton = buttons.Controls.OfType<Button>().First(button => button.DialogResult == DialogResult.Cancel);
            Shown += (_, _) => LayoutSplit();
            SizeChanged += (_, _) => LayoutSplit();
        }

        private Control BuildMapping()
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(16, 12, 16, 8)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var intro = _file == InvestmentImportFile.Positions
                ? "Map the file's column headings to the position. A date range uses its end date. The unit price is the price column, or the market value divided by the shares."
                : "Map the file's column headings to each activity row. The same mapping is remembered for this portfolio.";
            var introLabel = Wrap(intro);
            layout.Controls.Add(introLabel, 0, 0);
            layout.SetColumnSpan(introLabel, 2);

            var row = 1;
            if (_choosePortfolio)
            {
                layout.Controls.Add(FieldLabel("Portfolio"), 0, row);
                layout.Controls.Add(_portfolio, 1, row);
                row++;
            }

            foreach (var field in _fields)
            {
                Fill(field.Box, field.Current(), field.Optional);
                layout.Controls.Add(FieldLabel(field.Label), 0, row);
                layout.Controls.Add(field.Box, 1, row);
                row++;
            }

            scroll.Controls.Add(layout);
            return scroll;
        }

        private Control BuildFileGrid(InvestmentFileSheet sheet)
        {
            var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8) };
            var captionFont = new Font(Font, FontStyle.Bold);
            Disposed += (_, _) => captionFont.Dispose();
            var caption = new Label
            {
                Text = "File contents",
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(0, 4, 0, 10),
                Font = captionFont,
                TextAlign = ContentAlignment.BottomLeft
            };
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            foreach (var header in sheet.Headers)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = header,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }

            foreach (var row in sheet.Rows)
            {
                var values = new object[sheet.Headers.Count];
                for (var i = 0; i < values.Length; i++)
                    values[i] = i < row.Cells.Count ? row.Cells[i] : "";
                grid.Rows.Add(values);
            }

            host.Controls.Add(grid);
            host.Controls.Add(caption);
            return host;
        }

        private void LayoutSplit()
        {
            if (_splitReady || _split.IsDisposed)
                return;

            const int min = 100;
            var available = _split.Height - _split.SplitterWidth;
            if (available < min * 2)
                return;

            _split.Panel1MinSize = min;
            _split.Panel2MinSize = min;
            _splitReady = SplitContainerUtil.TrySetLeadingRatio(_split, leadingParts: 3, trailingParts: 1);
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
            var continueButton = new ThmsButton { Text = "Continue" };
            continueButton.Click += OnContinue;
            panel.Controls.Add(cancel);
            panel.Controls.Add(continueButton);
            return panel;
        }

        private void OnContinue(object? sender, EventArgs e)
        {
            if (_file == InvestmentImportFile.Positions)
            {
                var map = ReadPositionMap();
                var error = InvestmentPositionColumnMapping.Validate(_headers, map);
                if (error is null && _choosePortfolio && TargetFromPortfolio() is null && string.IsNullOrWhiteSpace(map.AccountNumberColumn))
                    error = "Choose a portfolio, or map an account number column.";
                if (error is not null)
                {
                    MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                PositionMap = map;
                TargetAccountId = _choosePortfolio ? TargetFromPortfolio() : map.AccountId == Guid.Empty ? null : map.AccountId;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            var history = ReadHistoryMap();
            var historyError = InvestmentHistoryColumnMapping.Validate(_headers, history);
            if (historyError is not null)
            {
                MessageBox.Show(this, historyError, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            HistoryMap = history;
            DialogResult = DialogResult.OK;
            Close();
        }

        private InvestmentPositionColumnMap ReadPositionMap()
        {
            var map = new InvestmentPositionColumnMap();
            foreach (var field in _fields)
                field.Assign(Selected(field.Box));
            AssignPosition(map);
            if (!_choosePortfolio)
                return map;
            map.AccountId = TargetFromPortfolio() ?? Guid.Empty;
            return map;
        }

        private InvestmentHistoryColumnMap ReadHistoryMap()
        {
            var map = new InvestmentHistoryColumnMap();
            foreach (var field in _fields)
                field.Assign(Selected(field.Box));
            AssignHistory(map);
            return map;
        }

        private void AssignPosition(InvestmentPositionColumnMap map)
        {
            map.SymbolColumn = Value("Symbol");
            map.DescriptionColumn = Value("Description");
            map.QuantityColumn = Value("Quantity");
            map.PriceColumn = Value("Price");
            map.ValueColumn = Value("Value");
            map.CostBasisColumn = Value("Cost");
            map.AverageCostColumn = Value("Average");
            map.AccountNumberColumn = Value("AccountNumber");
            map.AccountNameColumn = Value("AccountName");
            map.AsOfColumn = Value("AsOf");
            map.AmountPerShareColumn = Value("AmountPerShare");
            map.EstimatedIncomeColumn = Value("Income");
            map.DistributionRateColumn = Value("Rate");
            map.RateAsOfColumn = Value("RateAsOf");
            map.ExDateColumn = Value("ExDate");
            map.PayDateColumn = Value("PayDate");
        }

        private void AssignHistory(InvestmentHistoryColumnMap map)
        {
            map.DateColumn = Value("Date");
            map.ActionColumn = Value("Action");
            map.SymbolColumn = Value("Symbol");
            map.DescriptionColumn = Value("Description");
            map.QuantityColumn = Value("Quantity");
            map.PriceColumn = Value("Price");
            map.AmountColumn = Value("Amount");
            map.CashBalanceColumn = Value("Cash");
        }

        private string Value(string key) =>
            _fields.First(field => field.Key == key).Assigned;

        private Guid? TargetFromPortfolio() =>
            _portfolio.SelectedItem is PortfolioChoice choice ? choice.Id : null;

        private void FillPortfolios(IReadOnlyList<InvestmentAccount> portfolios, Guid preferredAccountId)
        {
            _portfolio.Items.Add(new PortfolioChoice(null, MatchAccounts));
            foreach (var account in portfolios)
                _portfolio.Items.Add(new PortfolioChoice(account.Id, account.Name ?? "Portfolio"));

            var accountNumberMapped = !string.IsNullOrWhiteSpace(Current("AccountNumber"));
            var preferred = preferredAccountId == Guid.Empty ? -1 : IndexOfPortfolio(preferredAccountId);
            if (accountNumberMapped)
                _portfolio.SelectedIndex = 0;
            else if (preferred >= 0)
                _portfolio.SelectedIndex = preferred;
            else if (portfolios.Count == 1)
                _portfolio.SelectedIndex = 1;
            else
                _portfolio.SelectedIndex = 0;
        }

        private int IndexOfPortfolio(Guid accountId)
        {
            for (var i = 0; i < _portfolio.Items.Count; i++)
            {
                if (_portfolio.Items[i] is PortfolioChoice choice && choice.Id == accountId)
                    return i;
            }

            return -1;
        }

        private string Current(string key) =>
            _fields.First(field => field.Key == key).Current();

        private void Fill(ComboBox box, string selected, bool optional)
        {
            box.Items.Clear();
            if (optional)
                box.Items.Add(None);
            foreach (var header in _headers)
                box.Items.Add(header);
            if (string.IsNullOrWhiteSpace(selected))
            {
                box.SelectedIndex = optional ? 0 : -1;
                return;
            }

            var index = InvestmentPositionColumnMapping.ColumnIndex(_headers, selected);
            if (index < 0)
                box.SelectedIndex = optional ? 0 : -1;
            else
                box.SelectedIndex = index + (optional ? 1 : 0);
        }

        private static string Selected(ComboBox box) =>
            box.SelectedItem as string is { } value && value != None ? value : "";

        private static Label FieldLabel(string text) =>
            new() { Text = text, AutoSize = true, Margin = new Padding(0, 8, 8, 0) };

        private static Label Wrap(string text) =>
            new()
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(900, 0),
                Margin = new Padding(0, 0, 0, 8)
            };

        private static List<MappedField> PositionFields(InvestmentPositionColumnMap map) =>
        [
            Field("Symbol", "Security", false, () => map.SymbolColumn),
            Field("Description", "Description", true, () => map.DescriptionColumn),
            Field("Quantity", "Shares", false, () => map.QuantityColumn),
            Field("Price", "Unit price", true, () => map.PriceColumn),
            Field("Value", "Market value", true, () => map.ValueColumn),
            Field("Cost", "Total cost", true, () => map.CostBasisColumn),
            Field("Average", "Average cost", true, () => map.AverageCostColumn),
            Field("AccountNumber", "Account number", true, () => map.AccountNumberColumn),
            Field("AccountName", "Account name", true, () => map.AccountNameColumn),
            Field("AsOf", "As-of date", true, () => map.AsOfColumn),
            Field("AmountPerShare", "Amount per share", true, () => map.AmountPerShareColumn),
            Field("Income", "Est. annual income", true, () => map.EstimatedIncomeColumn),
            Field("Rate", "Distribution rate", true, () => map.DistributionRateColumn),
            Field("RateAsOf", "Rate as of", true, () => map.RateAsOfColumn),
            Field("ExDate", "Ex-date", true, () => map.ExDateColumn),
            Field("PayDate", "Pay date", true, () => map.PayDateColumn)
        ];

        private static List<MappedField> HistoryFields(InvestmentHistoryColumnMap map) =>
        [
            Field("Date", "Date", false, () => map.DateColumn),
            Field("Action", "Action", false, () => map.ActionColumn),
            Field("Symbol", "Security", true, () => map.SymbolColumn),
            Field("Description", "Description", true, () => map.DescriptionColumn),
            Field("Quantity", "Shares", true, () => map.QuantityColumn),
            Field("Price", "Price", true, () => map.PriceColumn),
            Field("Amount", "Amount", true, () => map.AmountColumn),
            Field("Cash", "Cash balance", true, () => map.CashBalanceColumn)
        ];

        private static MappedField Field(string key, string label, bool optional, Func<string> current) =>
            new()
            {
                Key = key,
                Label = label,
                Optional = optional,
                Current = current
            };

        private sealed class MappedField
        {
            public string Key { get; init; } = "";
            public string Label { get; init; } = "";
            public bool Optional { get; init; }
            public Func<string> Current { get; init; } = () => "";
            public ComboBox Box { get; } = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
            public string Assigned { get; private set; } = "";

            public void Assign(string value) => Assigned = value;
        }

        private sealed record PortfolioChoice(Guid? Id, string Label)
        {
            public override string ToString() => Label;
        }
    }
}
