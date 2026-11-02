using System.Diagnostics;
using THMS.Data.Stores;
using THMS.Domain.Finance.Investments;
using THMS.Logic.Finance.Investments;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public class PortfoliosForm : BaseEmbeddedForm
    {
        private readonly InvestmentPortfolioCatalog _catalog = new();
        private readonly InvestmentImportOrchestrator _import = new();
        private readonly BindingSource _portfolioSource = new();
        private readonly BindingSource _securitySource = new();
        private readonly BindingSource _actionSource = new();
        private readonly DataGridView _portfolios = Grid();
        private readonly DataGridView _securities = Grid();
        private readonly DataGridView _actions = Grid();
        private readonly SplitContainer _outer = new() { Orientation = Orientation.Horizontal, Dock = DockStyle.Fill };
        private readonly SplitContainer _inner = new() { Orientation = Orientation.Horizontal, Dock = DockStyle.Fill };
        private bool _binding;
        private bool _splitsReady;
        private bool _preferFirst;
        private int _loadedRevision = int.MinValue;

        public PortfoliosForm()
        {
            Text = "Portfolios";
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;

            _portfolios.DataSource = _portfolioSource;
            _securities.DataSource = _securitySource;
            _actions.DataSource = _actionSource;
            _portfolios.Columns.AddRange(
                LinkFillColumn(nameof(InvestmentPortfolioRow.Name), "Name"),
                LinkFillColumn(nameof(InvestmentPortfolioRow.Institution), "Institution"),
                TextColumn(nameof(InvestmentPortfolioRow.AccountNumber), "Account"),
                MoneyColumn(nameof(InvestmentPortfolioRow.Cash), "Cash"),
                MoneyColumn(nameof(InvestmentPortfolioRow.EstimatedAnnualIncome), "Est. annual income"));
            _securities.Columns.AddRange(
                TextColumn(nameof(InvestmentPositionRow.Symbol), "Symbol"),
                FillColumn(nameof(InvestmentPositionRow.Name), "Name"),
                NumberColumn(nameof(InvestmentPositionRow.Shares), "Shares", "n4"),
                MoneyColumn(nameof(InvestmentPositionRow.AverageCost), "Average cost"),
                MoneyColumn(nameof(InvestmentPositionRow.TotalBasis), "Total basis"),
                MoneyColumn(nameof(InvestmentPositionRow.EstimatedAnnualIncome), "Est. annual income"),
                NumberColumn(nameof(InvestmentPositionRow.AmountPerShare), "Amount / share", "n4"),
                DateColumn(nameof(InvestmentPositionRow.ExDate), "Ex-date"),
                DateColumn(nameof(InvestmentPositionRow.PayDate), "Pay date"));
            _actions.Columns.AddRange(
                DateColumn(nameof(InvestmentAction.RunDate), "Date"),
                FillColumn(nameof(InvestmentAction.ActionText), "Action"),
                TextColumn(nameof(InvestmentAction.Kind), "Kind"),
                TextColumn(nameof(InvestmentAction.Status), "Status"),
                MoneyColumn(nameof(InvestmentAction.Amount), "Amount"),
                NumberColumn(nameof(InvestmentAction.Quantity), "Quantity", "n4"),
                MoneyColumn(nameof(InvestmentAction.Price), "Price"),
                MoneyColumn(nameof(InvestmentAction.CashChange), "Cash change"),
                NumberColumn(nameof(InvestmentAction.ShareChange), "Share change", "n4"),
                TextColumn(nameof(InvestmentAction.Note), "Note"));

            _portfolios.SelectionChanged += (_, _) => OnPortfolioSelected();
            _portfolios.CellContentClick += OnPortfolioLinkClicked;
            _portfolios.CellFormatting += OnPortfolioCellFormatting;
            _securities.SelectionChanged += (_, _) => OnSecuritySelected();

            SplitContainerUtil.MakeSplitterVisible(_outer);
            SplitContainerUtil.MakeSplitterVisible(_inner);
            var portfolios = Section("Portfolios", _portfolios);
            AttachPositionsDrop(portfolios);
            _outer.Panel1.Controls.Add(portfolios);
            _inner.Panel1.Controls.Add(Section("Securities", _securities));
            _inner.Panel2.Controls.Add(Section("Actions", _actions));
            _outer.Panel2.Controls.Add(_inner);
            _outer.SizeChanged += (_, _) => LayoutSplits();
            Controls.Add(_outer);
        }

        public void ShowHome()
        {
            _preferFirst = true;
            _loadedRevision = int.MinValue;
            RefreshView();
        }

        public void RefreshView()
        {
            if (!Visible || Disposing)
                return;
            if (FinanceDataRevision.Current == _loadedRevision)
                return;
            Reload();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            RefreshView();
        }

        private void Reload()
        {
            var accountId = _preferFirst ? (Guid?)null : SelectedPortfolio()?.AccountId;
            var symbol = _preferFirst ? null : SelectedSecurity()?.Symbol;
            _preferFirst = false;
            _binding = true;
            try
            {
                var portfolios = _catalog.Portfolios();
                _portfolioSource.DataSource = portfolios;
                var account = portfolios.FirstOrDefault(row => row.AccountId == accountId) ?? portfolios.FirstOrDefault();
                SelectRow(_portfolios, item => item is InvestmentPortfolioRow row && account is not null && row.AccountId == account.AccountId);

                var securities = account is null ? [] : _catalog.Securities(account.AccountId);
                _securitySource.DataSource = securities;
                var security = securities.FirstOrDefault(row =>
                        symbol is not null && string.Equals(row.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
                    ?? securities.FirstOrDefault();
                SelectRow(_securities, item => item is InvestmentPositionRow row && security is not null && string.Equals(row.Symbol, security.Symbol, StringComparison.OrdinalIgnoreCase));

                _actionSource.DataSource = account is null || security is null
                    ? []
                    : _catalog.Actions(account.AccountId, security.Symbol);
            }
            finally
            {
                _binding = false;
            }

            _loadedRevision = FinanceDataRevision.Current;
        }

        private void OnPortfolioSelected()
        {
            if (_binding)
                return;

            var symbol = SelectedSecurity()?.Symbol;
            _binding = true;
            try
            {
                var account = SelectedPortfolio();
                var securities = account is null ? [] : _catalog.Securities(account.AccountId);
                _securitySource.DataSource = securities;
                var security = securities.FirstOrDefault(row =>
                        symbol is not null && string.Equals(row.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
                    ?? securities.FirstOrDefault();
                SelectRow(_securities, item => item is InvestmentPositionRow row && security is not null && string.Equals(row.Symbol, security.Symbol, StringComparison.OrdinalIgnoreCase));
                _actionSource.DataSource = account is null || security is null
                    ? []
                    : _catalog.Actions(account.AccountId, security.Symbol);
            }
            finally
            {
                _binding = false;
            }
        }

        private void OnSecuritySelected()
        {
            if (_binding)
                return;

            var account = SelectedPortfolio();
            var security = SelectedSecurity();
            _binding = true;
            try
            {
                _actionSource.DataSource = account is null || security is null
                    ? []
                    : _catalog.Actions(account.AccountId, security.Symbol);
            }
            finally
            {
                _binding = false;
            }
        }

        private void OnPortfolioLinkClicked(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var column = _portfolios.Columns[e.ColumnIndex].Name;
            if (column is not (nameof(InvestmentPortfolioRow.Name) or nameof(InvestmentPortfolioRow.Institution)))
                return;
            if (_portfolios.Rows[e.RowIndex].DataBoundItem is not InvestmentPortfolioRow row)
                return;

            OpenWebsite(row.WebsiteUrl);
        }

        private void OnPortfolioCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var column = _portfolios.Columns[e.ColumnIndex].Name;
            if (column is not (nameof(InvestmentPortfolioRow.Name) or nameof(InvestmentPortfolioRow.Institution)))
                return;
            if (_portfolios.Rows[e.RowIndex].DataBoundItem is not InvestmentPortfolioRow row)
                return;
            if (_portfolios.Rows[e.RowIndex].Cells[e.ColumnIndex] is not DataGridViewLinkCell cell)
                return;

            DataGridViewUtil.ApplyLinkAppearance(
                cell,
                e.CellStyle,
                !string.IsNullOrWhiteSpace(row.WebsiteUrl),
                _portfolios.DefaultCellStyle.ForeColor);
        }

        private static void OpenWebsite(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (!Uri.TryCreate("https://" + url.Trim(), UriKind.Absolute, out uri))
                    return;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }

        private InvestmentPortfolioRow? SelectedPortfolio() =>
            _portfolios.CurrentRow?.DataBoundItem as InvestmentPortfolioRow;

        private InvestmentPositionRow? SelectedSecurity() =>
            _securities.CurrentRow?.DataBoundItem as InvestmentPositionRow;

        private void AttachPositionsDrop(Control section)
        {
            EnablePositionsDrop(section);
            foreach (Control child in section.Controls)
                EnablePositionsDrop(child);
        }

        private void EnablePositionsDrop(Control target)
        {
            target.AllowDrop = true;
            target.DragEnter += OnPositionsDrag;
            target.DragOver += OnPositionsDrag;
            target.DragDrop += OnPositionsDrop;
        }

        private static void OnPositionsDrag(object? sender, DragEventArgs e)
        {
            e.Effect = DroppedCsv(e) is not null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnPositionsDrop(object? sender, DragEventArgs e)
        {
            var path = DroppedCsv(e);
            if (path is null)
                return;

            e.Effect = DragDropEffects.Copy;
            BeginInvoke(() => ImportPositions(path));
        }

        private void ImportPositions(string path)
        {
            using var dialog = new InvestmentImportDialog(_import, path);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _loadedRevision = int.MinValue;
            RefreshView();
        }

        private static string? DroppedCsv(DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
                return null;

            var csv = files.Where(file =>
                Path.GetExtension(file).Equals(".csv", StringComparison.OrdinalIgnoreCase)).ToList();
            return csv.Count == 1 ? csv[0] : null;
        }

        private void LayoutSplits()
        {
            if (_splitsReady)
                return;

            if (!ApplySplit(_outer, 1, 2))
                return;
            if (!ApplySplit(_inner, 1, 1))
                return;
            _splitsReady = true;
        }

        private static bool ApplySplit(SplitContainer split, int leading, int trailing)
        {
            const int min = 100;
            var available = split.Height - split.SplitterWidth;
            if (available < min * 2)
                return false;

            split.Panel1MinSize = min;
            split.Panel2MinSize = min;
            return SplitContainerUtil.TrySetLeadingRatio(split, leading, trailing);
        }

        private static void SelectRow(DataGridView grid, Func<object, bool> match)
        {
            for (var i = 0; i < grid.Rows.Count; i++)
            {
                if (grid.Rows[i].DataBoundItem is { } item && match(item))
                {
                    DataGridViewUtil.SelectOnlyCell(grid, i, 0);
                    return;
                }
            }

            if (grid.Rows.Count > 0)
                DataGridViewUtil.SelectOnlyCell(grid, 0, 0);
        }

        private static Control Section(string title, DataGridView grid)
        {
            var label = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSize = false,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(8, 8, 8, 4),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            label.Height = label.PreferredHeight + label.Padding.Vertical;
            var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            grid.Dock = DockStyle.Fill;
            host.Controls.Add(grid);
            host.Controls.Add(label);
            return host;
        }

        private static DataGridView Grid()
        {
            var grid = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            DataGridViewUtil.EnableDoubleBuffering(grid);
            return grid;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                MinimumWidth = 70
            };

        private static DataGridViewLinkColumn LinkFillColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                MinimumWidth = 140,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                LinkBehavior = LinkBehavior.HoverUnderline,
                TrackVisitedState = false
            };

        private static DataGridViewTextBoxColumn FillColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.MinimumWidth = 140;
            return column;
        }

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "c2";
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn NumberColumn(string property, string header, string format)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = format;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private void InitializeComponent()
        {

        }

        private static DataGridViewTextBoxColumn DateColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "d";
            return column;
        }
    }
}
