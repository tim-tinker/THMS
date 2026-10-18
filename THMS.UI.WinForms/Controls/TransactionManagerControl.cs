using System.ComponentModel;
using System.Diagnostics;

using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Transactions;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionManagerControl : UserControl, IDataManagerControl
    {
        private const string HistoryMonth = "Month";
        private const string HistoryYear = "Year";
        private const string HistoryLifetime = "Lifetime";

        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly TransactionOrchestrator _txOrchestrator = new();
        private readonly RecurringRuleOrchestrator _ruleOrchestrator = new();
        private readonly ReconciliationOrchestrator _reconciliationOrchestrator = new();
        private readonly CategoryOrchestrator _categoryOrchestrator = new();
        private readonly AccountActivityOrchestrator _activityOrchestrator = new();
        private readonly IAccountStatementDataStore _statements = new DataStoreFactory().GetAccountStatementStore();

        private BindingSource _accountsSource = new BindingSource();
        private BindingSource _transactionsSource = new BindingSource();
        private Label? lblLoadStatus;
        private ProgressBar? progressLoad;
        private CancellationTokenSource? _txLoadCts;
        private bool _ready;
        private bool _suspendAccountChange;
        private bool _suspendHistoryChange;
        private decimal _postedBalanceBeforeEdit;
        private bool _hostProvidesHistory;
        private bool _hostProvidesAccounts;
        private Font? _unreconciledAccountFont;
        private List<UnifiedTransactionView> _allTransactions = [];
        private string? _categoryFilter;
        private string _descriptionSearch = "";
        private bool _suppressBalanceRecompute;
        private TextBox txtDescriptionSearch = null!;

        public event EventHandler? DataChanged;

        public TransactionManagerControl()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(splitContainer);
            InitializeHistory();
            LayoutForecastToolbar();
            btnAddRule.Click += OnAddRuleClicked;
            btnDeleteRule.Click += OnDeleteRuleClicked;
            InitializeGrids();
            HostLedgerDetail();
            _ready = true;
        }

        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HostProvidesHistory
        {
            get => _hostProvidesHistory;
            set
            {
                _hostProvidesHistory = value;
                lblHistory.Visible = !value;
                cmbHistory.Visible = !value;
            }
        }

        [DefaultValue(false)]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HostProvidesAccounts
        {
            get => _hostProvidesAccounts;
            set
            {
                _hostProvidesAccounts = value;
                splitContainer.Panel1Collapsed = value;
            }
        }

        public void SelectAccount(Guid? accountId)
        {
            LoadAccounts();
            _suspendAccountChange = true;
            try
            {
                if (accountId is Guid id)
                {
                    for (var i = 0; i < _accountsSource.Count; i++)
                    {
                        if (_accountsSource[i] is UnifiedAccountView view && view.Id == id)
                        {
                            _accountsSource.Position = i;
                            break;
                        }
                    }
                }
            }
            finally
            {
                _suspendAccountChange = false;
            }

            RefreshCurrentAccount();
        }

        public Control GetControl() => this;

        public void SetGridDataSource(string period)
        {
            SelectHistory(period);
            RefreshAll();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_hostProvidesHistory)
                LoadAccounts();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (_hostProvidesHistory)
                return;
            if (_ready && Visible && IsHandleCreated && Parent != null && !Disposing)
                RefreshAll();
        }

        private void LayoutForecastToolbar()
        {
            cmbHistory.Width = 120;
            cmbHistory.DropDownWidth = 120;
            cmbHistory.IntegralHeight = false;
            btnAddRule.AutoSize = true;
            btnDeleteRule.AutoSize = true;
        }

        private void InitializeHistory()
        {
            cmbHistory.Items.Clear();
            cmbHistory.Items.AddRange([HistoryMonth, HistoryYear, HistoryLifetime]);
            cmbHistory.SelectedItem = HistoryMonth;
            cmbHistory.SelectedIndexChanged += OnHistoryChanged;
        }

        private void InitializeGrids()
        {
            DataGridViewUtil.EnableDoubleBuffering(masterGrid);
            DataGridViewUtil.EnableDoubleBuffering(detailGrid);
            masterGrid.AutoGenerateColumns = false;
            detailGrid.AutoGenerateColumns = false;
            detailGrid.ReadOnly = true;
            detailGrid.EditMode = DataGridViewEditMode.EditProgrammatically;
            detailGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            detailGrid.MultiSelect = true;
            CategoryColumn.ReadOnly = true;
            CategoryColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            ForecastColumn.Visible = false;
            TypeColumn.Visible = false;
            detailGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StatusColumn",
                DataPropertyName = nameof(UnifiedTransactionView.Status),
                HeaderText = "Status",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            detailGrid.DataSource = _transactionsSource;
            masterGrid.DataSource = _accountsSource;
            BalanceColumn.DefaultCellStyle.NullValue = "N/A";
            AvailableColumn.DefaultCellStyle.NullValue = "N/A";

            _accountsSource.CurrentChanged += OnCurrentAccountChanged;
            _transactionsSource.ListChanged += OnTransactionsListChanged;

            masterGrid.CellBeginEdit += OnAccountCellBeginEdit;
            masterGrid.CellValidating += OnAccountCellValidating;
            masterGrid.CellParsing += OnAccountCellParsing;
            masterGrid.CellEndEdit += OnAccountPostedBalanceEdited;
            masterGrid.DataError += OnAccountGridDataError;
            masterGrid.CellDoubleClick += OnAccountCellDoubleClick;
            masterGrid.CellContentClick += OnAccountWebsiteClicked;
            masterGrid.CellFormatting += OnAccountGridCellFormatting;

            detailGrid.CellDoubleClick += (_, e) => OnTransactionDoubleClick(detailGrid, CategoryColumn, e, includeRules: true);
            detailGrid.KeyDown += (_, e) => OnTransactionKeyDown(detailGrid, CategoryColumn, e, includeRules: true);
            detailGrid.CellMouseClick += (_, e) => OnCategoryCellMouseClick(detailGrid, CategoryColumn, e);
            CategoryColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            detailGrid.ColumnHeaderMouseClick += OnLedgerHeaderMouseClick;
            detailGrid.CellFormatting += OnLedgerCellFormatting;
            detailGrid.SelectionChanged += (_, _) => UpdateRuleActionButtons();
            detailGrid.MouseDown += (_, e) => OnTransactionMouseDown(detailGrid, e, includeRules: true);
            AttachTransactionDrop(detailGrid, () => CurrentAccountId);
        }

        public void AttachTransactionActions(DataGridView grid, Func<Guid?> accountId)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(accountId);
            grid.MultiSelect = true;
            grid.ReadOnly = true;
            grid.EditMode = DataGridViewEditMode.EditProgrammatically;
            var category = grid.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(column => column.DataPropertyName == nameof(UnifiedTransactionView.Category));
            grid.MouseDown += (_, e) => OnTransactionMouseDown(grid, e, includeRules: false);
            grid.CellDoubleClick += (_, e) => OnTransactionDoubleClick(grid, category, e, includeRules: false);
            grid.CellMouseClick += (_, e) => OnCategoryCellMouseClick(grid, category, e);
            grid.KeyDown += (_, e) => OnTransactionKeyDown(grid, category, e, includeRules: false);
            grid.MultiSelect = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            if (grid.DataSource is BindingSource source)
                TransactionCategoryFilter.Attach(grid, source);
            AttachTransactionDrop(grid, accountId);
        }

        private void AttachTransactionDrop(DataGridView grid, Func<Guid?> accountId)
        {
            grid.AllowDrop = true;
            grid.DragEnter += (_, e) => OnTransactionDrag(e, accountId);
            grid.DragOver += (_, e) => OnTransactionDrag(e, accountId);
            grid.DragDrop += (_, e) => OnTransactionDragDrop(e, accountId);
        }

        private void HostLedgerDetail()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            splitContainer.Panel2.Controls.Remove(detailGrid);
            splitContainer.Panel2.Controls.Remove(forecastPanel);

            var forecastBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(8, 6, 8, 6),
                Name = "forecastBar"
            };
            var forecastControls = forecastPanel.Controls.Cast<Control>().ToArray();
            forecastPanel.Controls.Clear();
            foreach (var child in forecastControls)
            {
                if (child == btnAddRule || child == btnDeleteRule)
                    continue;
                child.Margin = new Padding(4, 4, 8, 4);
                forecastBar.Controls.Add(child);
            }

            lblLoadStatus = new Label
            {
                AutoSize = true,
                Margin = new Padding(12, 8, 8, 4),
                Visible = false
            };
            progressLoad = new ProgressBar
            {
                Width = 220,
                Height = 22,
                Margin = new Padding(4, 8, 8, 4),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false
            };
            forecastBar.Controls.Add(lblLoadStatus);
            forecastBar.Controls.Add(progressLoad);

            var lblDescription = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(4, 8, 8, 4),
                Text = "Description:",
                TextAlign = ContentAlignment.MiddleLeft
            };
            txtDescriptionSearch = new TextBox
            {
                AutoSize = false,
                Anchor = AnchorStyles.Left,
                Size = new Size(180, cmbHistory.Height),
                Margin = new Padding(4, 4, 8, 4),
                PlaceholderText = "Search"
            };
            txtDescriptionSearch.TextChanged += OnDescriptionSearchChanged;
            forecastBar.Controls.Add(lblDescription);
            forecastBar.Controls.Add(txtDescriptionSearch);
            forecastBar.WrapContents = false;

            splitContainer.Panel2.Controls.Add(BuildTransactionSplit(forecastBar));
        }

        private Control BuildTransactionSplit(Control ledgerHeader)
        {
            var ledgerPanel = new Panel { Dock = DockStyle.Fill };
            ledgerPanel.Controls.Add(detailGrid);
            ledgerPanel.Controls.Add(ledgerHeader);
            return ledgerPanel;
        }

        private void LoadAccounts()
        {
            _suspendAccountChange = true;
            try
            {
                var accounts = _accountOrchestrator.GetAllAccounts().ToList();
                if (_hostProvidesAccounts)
                {
                    _accountsSource.DataSource = UnifiedAccountViewBuilder.Build(accounts);
                    return;
                }

                var nextPayments = accounts
                    .Where(a => a is LoanAccount or MortgageAccount)
                    .ToDictionary(a => a.Id, a => _ruleOrchestrator.GetNextPaymentDate(a.Id));
                var liveBalances = new Dictionary<Guid, PostedBalanceDisplay>();
                foreach (var account in accounts)
                {
                    var statements = _statements.GetForAccount(account.Id).ToList();
                    if (!PostedBalanceCalculator.TryResolveLatestStatement(account, statements, out _, out var anchor))
                        continue;

                    var activity = _txOrchestrator.SumPostedAmountsAfter(account.Id, anchor.AsOf);
                    var latest = _txOrchestrator.GetLatestPostedActivityDate(account.Id);
                    if (PostedBalanceCalculator.TryCreateDisplay(account, statements, activity, latest, out var display))
                        liveBalances[account.Id] = display;
                }

                _accountsSource.DataSource = UnifiedAccountViewBuilder.Build(
                    accounts,
                    nextPayments,
                    livePostedBalances: liveBalances,
                    unreconciledAccountIds: _reconciliationOrchestrator.AccountIdsWithUnreconciled());
            }
            finally
            {
                _suspendAccountChange = false;
            }
        }

        private void OnCurrentAccountChanged(object? sender, EventArgs e)
        {
            if (_suspendAccountChange || !_ready)
                return;

            RefreshCurrentAccount();
        }

        private void OnHistoryChanged(object? sender, EventArgs e)
        {
            if (_suspendHistoryChange || !_ready)
                return;

            RefreshCurrentAccount();
        }

        private void OnTransactionMouseDown(DataGridView grid, MouseEventArgs e, bool includeRules)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var hit = grid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;
            if (!grid.Rows[hit.RowIndex].Selected)
            {
                grid.ClearSelection();
                grid.Rows[hit.RowIndex].Selected = true;
                if (hit.ColumnIndex >= 0)
                    grid.CurrentCell = grid[hit.ColumnIndex, hit.RowIndex];
            }

            var rowIndex = hit.RowIndex;
            var location = e.Location;
            grid.BeginInvoke(() => ShowTransactionMenu(grid, rowIndex, location, includeRules));
        }

        private void ShowTransactionMenu(DataGridView grid, int rowIndex, Point location, bool includeRules)
        {
            if (grid.IsDisposed || rowIndex < 0 || rowIndex >= grid.RowCount)
                return;

            var selected = grid.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem)
                .OfType<UnifiedTransactionView>()
                .ToList();
            var current = grid.Rows[rowIndex].DataBoundItem as UnifiedTransactionView;

            var menu = new ContextMenuStrip();
            if (includeRules && current is not null && TryGetRule(current, out _, out _))
            {
                var edit = new ToolStripMenuItem("Edit Rule…");
                edit.Click += (_, _) => OpenRuleEditor(current);
                menu.Items.Add(edit);
            }
            else if (current is not null)
            {
                if (CanEditTransaction(current))
                {
                    var edit = new ToolStripMenuItem("Edit Transaction…");
                    edit.Click += (_, _) => OpenTransactionEditor(current);
                    menu.Items.Add(edit);
                }

                if (CanSplit(current))
                {
                    var split = new ToolStripMenuItem("Split Transaction");
                    split.Click += (_, _) => OpenSplitEditor(current);
                    menu.Items.Add(split);
                }

                AddSetCategoryItem(menu, selected, current);

                if (CanEditTransaction(current))
                {
                    if (menu.Items.Count > 0)
                        menu.Items.Add(new ToolStripSeparator());
                    var targets = selected
                        .Append(current)
                        .Where(CanEditTransaction)
                        .Select(v => v.LookupId)
                        .Distinct()
                        .ToList();
                    var delete = new ToolStripMenuItem(targets.Count > 1
                        ? $"Delete {targets.Count} Transactions"
                        : "Delete Transaction");
                    delete.Click += (_, _) => ConfirmDeleteTransactions(targets, current);
                    menu.Items.Add(delete);
                }
            }

            var undoable = selected
                .Where(v => v.Status is TransactionStatuses.Reconciled or TransactionStatuses.New)
                .Where(v => _reconciliationOrchestrator.CanUndoMatch(v.LookupId))
                .ToList();
            if (undoable.Count > 0)
            {
                if (menu.Items.Count > 0)
                    menu.Items.Add(new ToolStripSeparator());
                var undo = new ToolStripMenuItem("Undo Match");
                undo.Click += (_, _) => UndoLedgerMatches(undoable);
                menu.Items.Add(undo);
            }

            if (menu.Items.Count == 0)
            {
                menu.Dispose();
                return;
            }

            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(grid, location);
        }

        private void AddSetCategoryItem(
            ContextMenuStrip menu,
            IReadOnlyList<UnifiedTransactionView> selected,
            UnifiedTransactionView? current)
        {
            var targets = selected.ToList();
            if (current is not null && targets.All(row => row.Id != current.Id))
                targets.Add(current);
            targets = targets
                .Where(CanAssignCategory)
                .DistinctBy(row => (row.LookupId, row.SplitRowId))
                .ToList();
            if (targets.Count == 0)
                return;

            if (menu.Items.Count > 0)
                menu.Items.Add(new ToolStripSeparator());

            Guid? shared = targets.Select(row => row.CategoryId).Distinct().Count() == 1
                ? targets[0].CategoryId
                : null;
            menu.Items.Add(CategoryTreeUi.CreateSetCategoryItem(
                _categoryOrchestrator.GetActiveCategories(),
                targets.Count,
                shared,
                category => AssignCategory(targets, category.Id),
                () => CreateAndAssignCategory(targets),
                OpenCategoryManager));
        }

        private void OnTransactionDrag(DragEventArgs e, Func<Guid?> accountId)
        {
            e.Effect = DroppedSpreadsheet(e) is not null && accountId() is Guid
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void OnTransactionDragDrop(DragEventArgs e, Func<Guid?> accountId)
        {
            e.Effect = DragDropEffects.Copy;
            var path = DroppedSpreadsheet(e);
            if (path is null)
                return;

            var id = accountId();
            // The browser stays inside its drag until this handler returns. Open the
            // modal import only after the drop has completed.
            BeginInvoke(() => ImportDroppedFile(path, id));
        }

        private void ImportDroppedFile(string path, Guid? accountId)
        {
            if (accountId is not Guid id)
            {
                MessageBox.Show(FindForm(), "Select an account before importing transactions.", "Import Transactions",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var account = _accountOrchestrator.GetAccount(id);
            if (account is null)
                return;

            TransactionFileImportLauncher.ImportFile(FindForm(), account, path, RefreshAll);
        }

        private static string? DroppedSpreadsheet(DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
                return null;

            var spreadsheets = files.Where(TransactionFileImportLauncher.IsSpreadsheet).ToList();
            return spreadsheets.Count == 1 ? spreadsheets[0] : null;
        }

        private static int FirstVisibleColumnIndex(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.Visible)
                    return column.Index;
            }

            return -1;
        }

        private void UndoLedgerMatches(IReadOnlyList<UnifiedTransactionView> rows)
        {
            try
            {
                foreach (var row in rows)
                    _reconciliationOrchestrator.UndoMatch(row.LookupId);
                RefreshAll();
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(FindForm(), "Undo Match", ex, icon: MessageBoxIcon.Information);
            }
        }

        private string SelectedHistory() =>
            cmbHistory.SelectedItem?.ToString() ?? HistoryMonth;

        private void SelectHistory(string period)
        {
            var item = period is HistoryYear or HistoryLifetime ? period : HistoryMonth;
            if (Equals(cmbHistory.SelectedItem, item))
                return;

            _suspendHistoryChange = true;
            cmbHistory.SelectedItem = item;
            _suspendHistoryChange = false;
        }

        private async Task LoadTransactionsForAccountAsync(Guid accountId, CancellationToken token)
        {
            var history = SelectedHistory();
            ShowLoadProgress($"Loading {history.ToLowerInvariant()} history...");

            try
            {
                var views = await Task.Run(
                    () => BuildTransactionViews(accountId, history, token));
                if (views is null || token.IsCancellationRequested || CurrentAccountId != accountId)
                    return;

                ShowLoadProgress("Updating grid...");
                _allTransactions = views;
                if (_categoryFilter is not null && views.All(row => !SameCategory(row, _categoryFilter)))
                {
                    _categoryFilter = null;
                    CategoryColumn.HeaderText = "Category";
                }

                ApplyLedgerRowFilter();
                UpdateRuleActionButtons();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private List<UnifiedTransactionView>? BuildTransactionViews(
            Guid accountId,
            string history,
            CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return null;

            var start = BaseOrchestrator.GetStartDate(DateTime.Today, history);
            var txs = history == HistoryLifetime
                ? _txOrchestrator.GetTransactionsForAccount(accountId)
                : _txOrchestrator.GetTransactionsForAccount(accountId, start, DateTime.MaxValue);
            if (token.IsCancellationRequested)
                return null;

            var posted = UnifiedTransactionViewBuilder.Build(
                txs.Posted,
                txs.PostedTransfers,
                userFutureSingles: [],
                userFutureTransfers: [],
                accountId,
                txs.IncomingTransferSplitPosted,
                incomingFutureSplitSources: []);
            ApplyCategoryDisplayNames(posted);
            ClearForecastBalances(posted);
            return UnifiedTransactionView.OrderForDisplay(posted).ToList();
        }

        private void OnAccountCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
            {
                e.Cancel = true;
                return;
            }

            _postedBalanceBeforeEdit = GetRowPostedBalance(e.RowIndex);
        }

        private void OnAccountCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (!IsPostedBalanceColumn(e.ColumnIndex) || e.RowIndex < 0)
                return;

            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
                return;

            if (!TryParsePostedBalance(e.FormattedValue, out _))
            {
                e.Cancel = true;
                masterGrid.CancelEdit();
            }
        }

        private void OnAccountCellParsing(object? sender, DataGridViewCellParsingEventArgs e)
        {
            if (!IsPostedBalanceColumn(e.ColumnIndex))
                return;

            if (TryParsePostedBalance(e.Value, out var parsed))
            {
                e.Value = parsed;
                e.ParsingApplied = true;
            }
        }

        private void OnAccountPostedBalanceEdited(object? sender, DataGridViewCellEventArgs e)
        {
            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
                return;

            if (GetAccountView(e.RowIndex) is not UnifiedAccountView view)
                return;

            var entered = view.Balance ?? 0;
            var displayDelta = entered - _postedBalanceBeforeEdit;
            if (displayDelta == 0)
                return;

            try
            {
                var account = _accountOrchestrator.GetAccount(view.Id);
                var ledgerDelta = PostedBalanceCalculator.ToLedgerPostedDelta(account, displayDelta);
                _accountOrchestrator.AdjustStartingBalanceForPostedDelta(view.Id, ledgerDelta);
                view.Balance = entered;
                RefreshCurrentAccount();
            }
            catch
            {
                view.Balance = _postedBalanceBeforeEdit;
                masterGrid.Refresh();
            }
        }

        private void OnAccountGridDataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = true;
        }

        private void OnAccountCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (!IsEditablePostedBalanceCell(e.RowIndex, e.ColumnIndex))
                return;

            masterGrid.CurrentCell = masterGrid[e.ColumnIndex, e.RowIndex];
            masterGrid.BeginEdit(true);
        }

        private void OnAccountWebsiteClicked(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (masterGrid.Columns[e.ColumnIndex] != WebsiteColumn)
                return;

            var url = GetAccountView(e.RowIndex)?.WebsiteUrl;
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

        private bool IsPostedBalanceColumn(int columnIndex) =>
            columnIndex >= 0 && masterGrid.Columns[columnIndex] == BalanceColumn;

        private void OnAccountGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var hasUnreconciled = masterGrid.Rows[e.RowIndex].DataBoundItem is UnifiedAccountView { HasUnreconciled: true };
            e.CellStyle.Font = hasUnreconciled
                ? _unreconciledAccountFont ??= new Font(masterGrid.Font, FontStyle.Bold)
                : masterGrid.Font;

            var column = masterGrid.Columns[e.ColumnIndex];
            if (column != BalanceColumn && column != AvailableColumn)
                return;

            if (e.Value is not null and not DBNull)
                return;

            e.Value = "N/A";
            e.FormattingApplied = true;
        }

        private bool IsEditablePostedBalanceCell(int rowIndex, int columnIndex)
        {
            if (!IsPostedBalanceColumn(columnIndex) || rowIndex < 0)
                return false;

            return GetAccountView(rowIndex)?.AccountType is "Bank" or "Credit"
                && GetAccountView(rowIndex)?.Balance is not null;
        }

        private UnifiedAccountView? GetAccountView(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= masterGrid.Rows.Count)
                return null;

            return masterGrid.Rows[rowIndex].DataBoundItem as UnifiedAccountView;
        }

        private decimal GetRowPostedBalance(int rowIndex) =>
            GetAccountView(rowIndex)?.Balance ?? 0;

        private static bool TryParsePostedBalance(object? value, out decimal parsed)
        {
            parsed = 0;
            if (value is decimal numeric)
            {
                parsed = numeric;
                return true;
            }

            var text = Convert.ToString(value)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Currency,
                System.Globalization.CultureInfo.CurrentCulture,
                out parsed);
        }

        private void ClearForecastBalances(IEnumerable<UnifiedTransactionView> list)
        {
            foreach (var item in list)
                item.ForecastBalance = null;
        }

        private void OnTransactionsListChanged(object? sender, ListChangedEventArgs e)
        {
            if (_suppressBalanceRecompute || LedgerRowFilterActive)
                return;
            if (_accountsSource.Current is not UnifiedAccountView)
                return;

            var unified = _transactionsSource.List.Cast<UnifiedTransactionView>().ToList();
            ClearForecastBalances(unified);
            detailGrid.Refresh();
        }

        private void OnTransactionDoubleClick(
            DataGridView grid,
            DataGridViewColumn? categoryColumn,
            DataGridViewCellEventArgs e,
            bool includeRules)
        {
            if (e.RowIndex < 0)
                return;

            if (categoryColumn is not null && IsCategoryColumn(grid, categoryColumn, e.ColumnIndex))
            {
                ShowCategoryMenu(grid, categoryColumn, e.RowIndex);
                return;
            }

            if (grid.Rows[e.RowIndex].DataBoundItem is not UnifiedTransactionView view)
                return;

            if (includeRules && TryGetRule(view, out _, out _))
            {
                OpenRuleEditor(view);
                return;
            }

            if (CanEditTransaction(view))
            {
                OpenTransactionEditor(view);
                return;
            }

            if (CanSplit(view))
                OpenSplitEditor(view);
        }

        private bool LedgerRowFilterActive =>
            _categoryFilter is not null || !string.IsNullOrWhiteSpace(_descriptionSearch);

        private bool ShowingCreditCard() =>
            _accountsSource.Current is UnifiedAccountView { AccountType: AccountKinds.Credit };

        private void OnDescriptionSearchChanged(object? sender, EventArgs e)
        {
            _descriptionSearch = txtDescriptionSearch.Text.Trim();
            ApplyLedgerRowFilter();
        }

        private void OnLedgerHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex != CategoryColumn.Index)
                return;
            if (_allTransactions.Count == 0)
                return;

            var header = detailGrid.GetCellDisplayRectangle(e.ColumnIndex, -1, cutOverflow: false);
            var screen = detailGrid.RectangleToScreen(header);
            ExcelColumnFilterDropDown.ShowSingle(
                this,
                screen,
                _allTransactions.Select(CategoryKey),
                _categoryFilter,
                chosen =>
                {
                    _categoryFilter = chosen;
                    CategoryColumn.HeaderText = _categoryFilter is null ? "Category" : "Category ▼";
                    ApplyLedgerRowFilter();
                });
        }

        private void OnLedgerCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (detailGrid.Columns[e.ColumnIndex] == AmountColumn
                && ShowingCreditCard()
                && e.Value is decimal amount)
            {
                e.Value = -amount;
                return;
            }

            if (detailGrid.Columns[e.ColumnIndex] != ForecastColumn)
                return;
            if (!LedgerRowFilterActive)
                return;

            e.Value = "TBD";
            e.FormattingApplied = true;
        }

        private void ApplyLedgerRowFilter()
        {
            var firstDisplayed = detailGrid.RowCount > 0
                ? detailGrid.FirstDisplayedScrollingRowIndex
                : -1;
            var selectedId = detailGrid.CurrentRow?.DataBoundItem is UnifiedTransactionView selected
                ? selected.Id
                : (Guid?)null;
            var selectedColumn = detailGrid.CurrentCell?.ColumnIndex ?? -1;

            IEnumerable<UnifiedTransactionView> rows = _allTransactions;
            if (_categoryFilter is not null)
                rows = rows.Where(t => SameCategory(t, _categoryFilter));
            if (!string.IsNullOrWhiteSpace(_descriptionSearch))
            {
                rows = rows.Where(t =>
                    (t.Description ?? "").Contains(_descriptionSearch, StringComparison.CurrentCultureIgnoreCase));
            }

            var list = UnifiedTransactionView.OrderForDisplay(rows).ToList();

            _suppressBalanceRecompute = true;
            try
            {
                _transactionsSource.DataSource = list;
            }
            finally
            {
                _suppressBalanceRecompute = false;
            }

            detailGrid.Refresh();
            RestoreLedgerView(firstDisplayed, selectedId, selectedColumn);
        }

        private void RestoreLedgerView(int firstDisplayed, Guid? selectedId, int columnIndex)
        {
            if (selectedId is Guid id)
            {
                for (var i = 0; i < detailGrid.Rows.Count; i++)
                {
                    if (detailGrid.Rows[i].DataBoundItem is not UnifiedTransactionView view || view.Id != id)
                        continue;

                    var column = columnIndex >= 0
                        && columnIndex < detailGrid.Columns.Count
                        && detailGrid.Columns[columnIndex].Visible
                        ? columnIndex
                        : FirstVisibleColumnIndex(detailGrid);
                    if (column >= 0
                        && (detailGrid.CurrentCell is null
                            || detailGrid.CurrentCell.RowIndex != i
                            || detailGrid.CurrentCell.ColumnIndex != column))
                    {
                        detailGrid.CurrentCell = detailGrid[column, i];
                    }

                    detailGrid.Rows[i].Selected = true;
                    break;
                }
            }

            if (firstDisplayed < 0)
                return;

            ApplyLedgerScroll(firstDisplayed);
            if (detailGrid.IsHandleCreated)
                detailGrid.BeginInvoke(() => ApplyLedgerScroll(firstDisplayed));
        }

        private void ApplyLedgerScroll(int firstDisplayed)
        {
            if (detailGrid.IsDisposed || detailGrid.RowCount == 0 || !detailGrid.IsHandleCreated)
                return;
            if (detailGrid.DisplayedRowCount(includePartialRow: false) == 0)
                return;

            var visible = Math.Max(1, detailGrid.DisplayedRowCount(includePartialRow: false));
            var maxFirst = Math.Max(0, detailGrid.RowCount - visible);
            var index = Math.Clamp(firstDisplayed, 0, maxFirst);
            if (detailGrid.FirstDisplayedScrollingRowIndex != index)
                detailGrid.FirstDisplayedScrollingRowIndex = index;
        }

        private static string CategoryKey(UnifiedTransactionView view) =>
            ExcelColumnFilterDropDown.Normalize(view.Category);

        private static bool SameCategory(UnifiedTransactionView view, string category) =>
            string.Equals(CategoryKey(view), category, StringComparison.CurrentCultureIgnoreCase);

        private void OnCategoryCellMouseClick(DataGridView grid, DataGridViewColumn? categoryColumn, DataGridViewCellMouseEventArgs e)
        {
            if (categoryColumn is null || e.RowIndex < 0 || !IsCategoryColumn(grid, categoryColumn, e.ColumnIndex))
                return;
            if (e.Button != MouseButtons.Left)
                return;
            if ((Control.ModifierKeys & (Keys.Control | Keys.Shift)) != 0)
                return;
            if (!CanEditCategory(grid, e.RowIndex))
                return;

            grid.CurrentCell = grid[e.ColumnIndex, e.RowIndex];
            ShowCategoryMenu(grid, categoryColumn, e.RowIndex);
        }

        private static bool IsCategoryColumn(DataGridView grid, DataGridViewColumn categoryColumn, int columnIndex) =>
            columnIndex >= 0 && grid.Columns[columnIndex] == categoryColumn;

        private static bool CanEditCategory(DataGridView grid, int rowIndex)
        {
            if (grid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view)
                return false;

            return view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;
        }

        private void ShowCategoryMenu(DataGridView grid, DataGridViewColumn categoryColumn, int rowIndex)
        {
            if (grid.Rows[rowIndex].DataBoundItem is not UnifiedTransactionView view || !CanEditCategory(grid, rowIndex))
                return;

            var posted = view.Type == UnifiedTransactionView.PostedType
                ? _txOrchestrator.GetTransactionsForAccount(view.AccountId).Posted.FirstOrDefault(t => t.Id == view.LookupId)
                : null;
            var suggestion = posted is null ? null : _categoryOrchestrator.Suggest(posted);
            var splitSelected = !view.IsSplitRow && view.Category == UnifiedTransactionView.SplitCategory;
            var menu = CategoryTreeUi.CreateAssignMenu(
                _categoryOrchestrator.GetActiveCategories(),
                splitSelected ? null : view.CategoryId ?? suggestion?.CategoryId,
                splitSelected ? null : suggestion?.CategoryId,
                category => AssignCategory([view], category.Id),
                () => CreateAndAssignCategory([view]),
                OpenCategoryManager,
                () => OpenSplitEditor(view),
                splitSelected);

            var cell = grid.GetCellDisplayRectangle(categoryColumn.Index, rowIndex, cutOverflow: false);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(grid, new Point(cell.Left, cell.Bottom));
        }

        private static bool CanAssignCategory(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType or UnifiedTransactionView.PostedTransferType;

        private void AssignCategory(IReadOnlyList<UnifiedTransactionView> views, Guid categoryId)
        {
            foreach (var view in views)
                _categoryOrchestrator.AssignToPosted(view.LookupId, categoryId, splitRowId: view.SplitRowId);
            RefreshAll();
        }

        private void CreateAndAssignCategory(IReadOnlyList<UnifiedTransactionView> views)
        {
            using var editor = new CategoryEditor(_categoryOrchestrator);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK || editor.CreatedCategory is null)
                return;

            AssignCategory(views, editor.CreatedCategory.Id);
        }

        private void OpenCategoryManager()
        {
            using var manager = new CategoryManager(_categoryOrchestrator);
            if (manager.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private void ApplyCategoryDisplayNames(IEnumerable<UnifiedTransactionView> views)
        {
            var names = _categoryOrchestrator.GetAllCategories(includeInactive: true)
                .ToDictionary(c => c.Id, c => c.Name);
            foreach (var view in views)
            {
                if (view.CategoryId is Guid id && names.TryGetValue(id, out var name))
                    view.Category = name;
            }
        }

        private void OnTransactionKeyDown(
            DataGridView grid,
            DataGridViewColumn? categoryColumn,
            KeyEventArgs e,
            bool includeRules)
        {
            if (categoryColumn is not null
                && grid.CurrentCell is { RowIndex: >= 0 } cell
                && IsCategoryColumn(grid, categoryColumn, cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditCategory(grid, cell.RowIndex))
            {
                ShowCategoryMenu(grid, categoryColumn, cell.RowIndex);
                e.Handled = true;
                return;
            }

            if (e.KeyCode != Keys.Delete)
                return;

            if (includeRules && GetSelectedRuleView() is not null)
            {
                DeleteSelectedRule();
                e.Handled = true;
                return;
            }

            if (grid.CurrentRow?.DataBoundItem is UnifiedTransactionView view && CanEditTransaction(view))
            {
                var targets = grid.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(r => r.DataBoundItem)
                    .OfType<UnifiedTransactionView>()
                    .Append(view)
                    .Where(CanEditTransaction)
                    .Select(v => v.LookupId)
                    .Distinct()
                    .ToList();
                ConfirmDeleteTransactions(targets, view);
                e.Handled = true;
            }
        }

        private void OnAddRuleClicked(object? sender, EventArgs e)
        {
            var accountId = (_accountsSource.Current as UnifiedAccountView)?.Id;
            using var editor = new RecurringRuleEditor(accountId);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private bool CanEditTransaction(UnifiedTransactionView view)
        {
            var parent = _txOrchestrator.GetParent(view.LookupId);
            return parent is PostedTransaction or FutureSingleTransaction or FutureTransferTransaction;
        }

        private void OpenTransactionEditor(UnifiedTransactionView view)
        {
            var parent = _txOrchestrator.GetParent(view.LookupId);
            if (parent is not (PostedTransaction or FutureSingleTransaction or FutureTransferTransaction))
                return;

            var categories = _categoryOrchestrator.GetActiveCategories().ToList();
            if (parent.CategoryId is Guid categoryId && categories.All(c => c.Id != categoryId))
            {
                var current = _categoryOrchestrator.GetCategory(categoryId);
                if (current is not null)
                    categories.Add(current);
            }

            var showIssuerSign = parent is not FutureTransferTransaction
                && _accountOrchestrator.GetAllAccounts().FirstOrDefault(account => account.Id == view.AccountId) is CreditAccount;
            var hint = showIssuerSign
                ? "A charge is a positive amount. A payment is negative."
                : parent switch
                {
                    FutureTransferTransaction => "Amount is the size of the transfer.",
                    PostedTransferTransaction => "Negative amounts leave this account. Positive amounts come in.",
                    _ => "Negative amounts leave this account. Positive amounts are deposits."
                };
            if (parent.HasSplits)
                hint += " Amount and category are changed from Split Transaction.";

            using var dialog = new AccountEntryDialog(
                "Edit Transaction",
                hint,
                categories,
                defaultDescription: parent.Description,
                initialAmount: showIssuerSign ? -parent.Amount : parent.Amount,
                initialDate: parent.Date,
                initialCategoryId: parent.CategoryId,
                lockAmount: parent.HasSplits,
                lockCategory: parent.HasSplits,
                categoryIsSplit: parent.HasSplits,
                accounts: _accountOrchestrator.GetAllAccounts().ToList(),
                existingSplits: parent.Splits);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _activityOrchestrator.UpdateLedgerEntry(
                    parent.Id,
                    dialog.Date,
                    showIssuerSign ? -dialog.Amount : dialog.Amount,
                    dialog.Description,
                    dialog.CategoryId);
                if (dialog.HasPendingSplits)
                    ApplyChosenSplits(parent.Id, dialog.PendingSplits);
                RefreshAll();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(FindForm(), "Edit Transaction", ex, icon: MessageBoxIcon.Information);
            }
        }

        private void ConfirmDeleteTransactions(IReadOnlyList<Guid> ids, UnifiedTransactionView current)
        {
            if (ids.Count == 0)
                return;

            var parent = _txOrchestrator.GetParent(current.LookupId);
            var prompt = ids.Count > 1
                ? $"Delete {ids.Count} transactions?"
                : DeletePrompt(current, parent);
            if (MessageBox.Show(FindForm(), prompt, "Delete Transaction",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                foreach (var id in ids)
                    _activityOrchestrator.DeleteLedgerEntry(id);
                RefreshAll();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(FindForm(), "Delete Transaction", ex, icon: MessageBoxIcon.Information);
            }
        }

        private static string DeletePrompt(UnifiedTransactionView view, BaseTransaction? parent)
        {
            var name = string.IsNullOrWhiteSpace(parent?.Description) ? "this transaction" : parent!.Description.Trim();
            var when = parent is null ? "" : $" ({parent.Amount:c2} on {parent.Date:d})";
            var prompt = $"Delete {name}{when}?";
            if (parent is PostedTransferTransaction { RelatedPostedTransactionId: var related } && related != Guid.Empty)
                prompt += " This also removes the other side of the transfer.";
            else if (parent is FutureTransferTransaction)
                prompt += " This removes it from both accounts.";
            else if (view.IsSplitRow || parent?.HasSplits == true)
                prompt += " This removes the whole transaction, including its splits.";
            return prompt;
        }

        private void ApplyChosenSplits(Guid transactionId, IReadOnlyList<SplitTransactionRow> splits)
        {
            var rows = splits.Select(split => split.Clone()).ToList();
            if (!_txOrchestrator.TryApplyAsWholeTransfer(transactionId, rows))
                _txOrchestrator.ApplySplits(transactionId, rows);
        }

        private static bool CanSplit(UnifiedTransactionView view) =>
            view.Type is UnifiedTransactionView.PostedType
                or UnifiedTransactionView.PostedTransferType
                or UnifiedTransactionView.FutureType
                or UnifiedTransactionView.FutureTransferType;

        private List<SplitTransactionRow> SeedSplits(
            BaseTransaction parent,
            Guid accountId,
            IReadOnlyList<ExpenseCategory> categories,
            IReadOnlyList<Account> accounts)
        {
            var existing = parent.Splits.Select(s => s.Clone()).ToList();
            if (existing.Count > 0)
                return existing;

            var ownerId = parent is BaseSingleAccountTransaction single && single.AccountId != Guid.Empty
                ? single.AccountId
                : accountId;
            var counterpart = _txOrchestrator.FindTransferCounterpartAccount(parent, ownerId, accounts);
            return [SplitTransactionSeed.Create(parent, counterpart, categories)];
        }

        private void OpenSplitEditor(UnifiedTransactionView view)
        {
            if (!CanSplit(view))
                return;

            var parentId = view.LookupId;
            var parent = _txOrchestrator.GetParent(parentId);
            if (parent is null)
                return;

            var amount = parent.Amount;
            var description = parent.Description ?? view.Description;
            var categories = _categoryOrchestrator.GetActiveCategories();
            var accounts = _accountOrchestrator.GetAllAccounts().ToList();
            var existing = SeedSplits(parent, view.AccountId, categories, accounts);

            using var editor = new SplitTransactionEditor(
                description,
                amount,
                existing,
                categories,
                accounts);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                if (!_txOrchestrator.TryApplyAsWholeTransfer(parentId, editor.Result))
                    _txOrchestrator.ApplySplits(parentId, editor.Result);

                RefreshAll();
            }
            catch (InvalidOperationException ex)
            {
                ExceptionDialog.Show(FindForm(), "Split Transaction", ex, icon: MessageBoxIcon.Information);
            }
        }

        private void OnDeleteRuleClicked(object? sender, EventArgs e)
        {
            DeleteSelectedRule();
        }

        private void UpdateRuleActionButtons()
        {
            btnDeleteRule.Enabled = GetSelectedRuleView() is not null;
        }

        private UnifiedTransactionView? GetSelectedRuleView()
        {
            if (detailGrid.CurrentRow?.DataBoundItem is UnifiedTransactionView view
                && TryGetRule(view, out _, out _))
                return view;

            return null;
        }

        private bool TryGetRule(
            UnifiedTransactionView view,
            out RecurringSingleTransactionRule? single,
            out RecurringTransferRule? transfer)
        {
            single = _ruleOrchestrator.GetSingleRule(view.LookupId);
            transfer = single is null ? _ruleOrchestrator.GetTransferRule(view.LookupId) : null;
            return single is not null || transfer is not null;
        }

        private void OpenRuleEditor(UnifiedTransactionView view)
        {
            if (!TryGetRule(view, out var single, out var transfer))
                return;

            using var editor = single is not null
                ? new RecurringRuleEditor(view.AccountId, single)
                : new RecurringRuleEditor(view.AccountId, existingTransfer: transfer!);
            if (editor.ShowDialog(FindForm()) == DialogResult.OK)
                RefreshAll();
        }

        private void DeleteSelectedRule()
        {
            if (GetSelectedRuleView() is not UnifiedTransactionView view
                || !TryGetRule(view, out var single, out var transfer))
                return;

            if (MessageBox.Show(
                    FindForm(),
                    $"Delete recurring rule '{view.Description}'?",
                    "Delete Rule",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            if (single is not null)
                _ruleOrchestrator.DeleteSingleRule(single.Id);
            else
                _ruleOrchestrator.DeleteTransferRule(transfer!.Id);

            RefreshAll();
        }

        public void FilterByType(string type)
        {
            RefreshCurrentAccount();

            if (string.IsNullOrWhiteSpace(type))
            {
                _transactionsSource.RemoveFilter();
                return;
            }

            var filtered = _transactionsSource.List.Cast<UnifiedTransactionView>()
                .Where(t => !t.IsForecasted && t.Type == type)
                .ToList();
            ClearForecastBalances(filtered);
            _transactionsSource.DataSource = filtered;
            detailGrid.Refresh();
        }

        public void RefreshAll()
        {
            LoadAccounts();
            RefreshCurrentAccount();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshCurrentAccount()
        {
            _ = RefreshCurrentAccountAsync();
        }

        private async Task RefreshCurrentAccountAsync()
        {
            if (_accountsSource.Current is not UnifiedAccountView account)
                return;

            _txLoadCts?.Cancel();
            var cts = new CancellationTokenSource();
            _txLoadCts = cts;

            try
            {
                await LoadTransactionsForAccountAsync(account.Id, cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(_txLoadCts, cts) && !cts.IsCancellationRequested)
                    HideLoadProgress();
                else if (!ReferenceEquals(_txLoadCts, cts))
                    cts.Dispose();
            }
        }

        private void ShowLoadProgress(string status)
        {
            if (lblLoadStatus is null || progressLoad is null)
                return;

            lblLoadStatus.Text = status;
            lblLoadStatus.Visible = true;
            progressLoad.Visible = true;
            lblLoadStatus.Update();
            progressLoad.Update();
        }

        private void HideLoadProgress()
        {
            if (lblLoadStatus is null || progressLoad is null)
                return;

            progressLoad.Visible = false;
            lblLoadStatus.Visible = false;
            lblLoadStatus.Text = "";
        }

        private Guid? CurrentAccountId =>
            (_accountsSource.Current as UnifiedAccountView)?.Id;
    }
}
