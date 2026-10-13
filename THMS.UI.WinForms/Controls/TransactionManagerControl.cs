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
        private const string ShowAll = "All";
        private const string ShowPosted = "Posted";
        private const string ShowForecast = "Forecast";
        private const string ShowRecurringRules = "Recurring Rules";
        private const string HistoryMonth = "Month";
        private const string HistoryYear = "Year";
        private const string HistoryLifetime = "Lifetime";

        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly TransactionOrchestrator _txOrchestrator = new();
        private readonly RecurringRuleOrchestrator _ruleOrchestrator = new();
        private readonly ReconciliationOrchestrator _reconciliationOrchestrator = new();
        private readonly BindingSource _importedSource = new();
        private DataGridView importedGrid = null!;
        private SplitContainer? _ledgerSplit;
        private bool _importedSplitPending;
        private Guid? _focusAfterReconcileAccountId;
        private Label? lblImported;
        private readonly CategoryOrchestrator _categoryOrchestrator = new();
        private readonly AccountActivityOrchestrator _activityOrchestrator = new();
        private readonly IAccountStatementDataStore _statements = new DataStoreFactory().GetAccountStatementStore();

        private BindingSource _accountsSource = new BindingSource();
        private BindingSource _transactionsSource = new BindingSource();
        private Label? lblLoadStatus;
        private ProgressBar? progressLoad;
        private CancellationTokenSource? _txLoadCts;
        private bool _filterApplied;
        private bool _ready;
        private bool _suspendAccountChange;
        private bool _suspendHistoryChange;
        private decimal _postedBalanceBeforeEdit;
        private bool _hostProvidesHistory;
        private bool _hostProvidesAccounts;
        private Font? _unreconciledAccountFont;
        private List<UnifiedTransactionView> _allTransactions = [];
        private HashSet<string>? _categoryFilter;
        private string _descriptionSearch = "";
        private bool _suppressBalanceRecompute;
        private TextBox txtDescriptionSearch = null!;

        public event EventHandler? DataChanged;

        public TransactionManagerControl()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(splitContainer);
            InitializeHistory();
            InitializeForecastPeriod();
            InitializeShowFilter();
            LayoutForecastToolbar();
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
            cmbForecastPeriod.Width = 140;
            cmbForecastPeriod.DropDownWidth = 140;
            cmbForecastPeriod.IntegralHeight = false;
            cmbShow.Width = 200;
            cmbShow.DropDownWidth = 220;
            cmbShow.IntegralHeight = false;
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

        private void InitializeForecastPeriod()
        {
            cmbForecastPeriod.Items.Clear();
            cmbForecastPeriod.Items.AddRange(["None", "30 days", "60 days", "90 days", "6 months", "1 year"]);
            cmbForecastPeriod.SelectedIndex = 0;
            cmbForecastPeriod.SelectedIndexChanged += OnForecastPeriodChanged;
        }

        private void InitializeShowFilter()
        {
            cmbShow.Items.Clear();
            cmbShow.Items.AddRange([ShowAll, ShowPosted, ShowForecast, ShowRecurringRules]);
            cmbShow.SelectedIndex = 0;
            cmbShow.SelectedIndexChanged += OnShowFilterChanged;
            btnAddRule.Click += OnAddRuleClicked;
            btnDeleteRule.Click += OnDeleteRuleClicked;
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

            detailGrid.CellDoubleClick += OnTransactionCellDoubleClick;
            detailGrid.KeyDown += OnTransactionGridKeyDown;
            detailGrid.CellMouseClick += OnCategoryCellMouseClick;
            detailGrid.ColumnHeaderMouseClick += OnLedgerHeaderMouseClick;
            detailGrid.CellFormatting += OnLedgerCellFormatting;
            detailGrid.SelectionChanged += (_, _) => UpdateRuleActionButtons();
            detailGrid.MouseDown += OnLedgerMouseDown;
            detailGrid.AllowDrop = true;
            detailGrid.DragEnter += OnLedgerDrag;
            detailGrid.DragOver += OnLedgerDrag;
            detailGrid.DragDrop += OnLedgerDragDrop;
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
                Size = new Size(180, cmbShow.Height),
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
            importedGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Dock = DockStyle.Fill,
                MultiSelect = true,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ScrollBars = ScrollBars.Both
            };
            DataGridViewUtil.EnableDoubleBuffering(importedGrid);
            var importedDate = TextColumn(nameof(ImportedTransactionView.Date), "Date");
            importedDate.DefaultCellStyle.Format = "d";
            importedDate.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            importedDate.Width = 100;
            var importedAmount = CurrencyColumn(nameof(ImportedTransactionView.Amount), "Amount");
            importedAmount.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            importedAmount.Width = 110;
            var importedDescription = TextColumn(nameof(ImportedTransactionView.Description), "Description");
            importedDescription.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            importedDescription.Width = 180;
            var importedMatch = TextColumn(nameof(ImportedTransactionView.RecommendedMatch), "Recommended Match");
            importedMatch.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            importedMatch.Width = 220;
            var importedStatus = TextColumn(nameof(ImportedTransactionView.Status), "Status");
            importedStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            importedStatus.Width = 120;
            importedGrid.Columns.AddRange(
                importedDate,
                importedAmount,
                importedDescription,
                importedMatch,
                importedStatus);
            importedGrid.DataError += (_, e) => e.ThrowException = false;
            importedGrid.Resize += (_, _) => SizeImportedColumns();
            importedGrid.DataSource = _importedSource;
            importedGrid.MouseDown += OnImportedMouseDown;

            lblImported = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(8, 0, 8, 0),
                Text = "Imported (unreconciled)",
                TextAlign = ContentAlignment.MiddleLeft
            };
            var importedHeader = new Panel { Dock = DockStyle.Top };
            importedHeader.Controls.Add(lblImported);
            var headerHeight = ledgerHeader.GetPreferredSize(Size.Empty).Height;
            if (headerHeight > 0)
                importedHeader.Height = headerHeight;
            ledgerHeader.SizeChanged += (_, _) =>
            {
                if (ledgerHeader.Height > 0 && importedHeader.Height != ledgerHeader.Height)
                    importedHeader.Height = ledgerHeader.Height;
            };

            var ledgerPanel = new Panel { Dock = DockStyle.Fill };
            ledgerPanel.Controls.Add(detailGrid);
            ledgerPanel.Controls.Add(ledgerHeader);

            var importedPanel = new Panel { Dock = DockStyle.Fill };
            importedPanel.Controls.Add(importedGrid);
            importedPanel.Controls.Add(importedHeader);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel2Collapsed = true
            };
            SplitContainerUtil.MakeSplitterVisible(split);
            split.Panel1.Controls.Add(ledgerPanel);
            split.Panel2.Controls.Add(importedPanel);
            split.SizeChanged += (_, _) => TryApplyImportedSplitDistance();
            _ledgerSplit = split;
            _importedSource.ListChanged += (_, _) => UpdateImportedSplit();
            return split;
        }

        private void UpdateImportedSplit()
        {
            if (_ledgerSplit is null || _ledgerSplit.IsDisposed)
                return;

            var show = _importedSource.Count > 0;
            if (_ledgerSplit.Panel2Collapsed == !show)
                return;

            if (!show)
            {
                _importedSplitPending = false;
                _ledgerSplit.Panel2Collapsed = true;
                return;
            }

            _importedSplitPending = true;
            _ledgerSplit.Panel2Collapsed = false;
            TryApplyImportedSplitDistance();
        }

        private void TryApplyImportedSplitDistance()
        {
            if (!_importedSplitPending || _ledgerSplit is null || _ledgerSplit.Panel2Collapsed || !_ledgerSplit.IsHandleCreated)
                return;

            const int min = 180;
            var available = _ledgerSplit.Width - _ledgerSplit.SplitterWidth;
            if (available < min * 2)
                return;

            _ledgerSplit.Panel1MinSize = min;
            _ledgerSplit.Panel2MinSize = min;
            _ledgerSplit.SplitterDistance = available / 2;
            _importedSplitPending = false;
            SizeImportedColumns();
            importedGrid?.Invalidate();
        }

        private void SizeImportedColumns()
        {
            if (importedGrid is null || importedGrid.IsDisposed || importedGrid.Columns.Count < 5)
                return;

            const int dateWidth = 100;
            const int amountWidth = 110;
            const int statusWidth = 120;
            var scrollbar = importedGrid.Controls.OfType<VScrollBar>().Any(bar => bar.Visible)
                ? SystemInformation.VerticalScrollBarWidth
                : 0;
            var remaining = importedGrid.ClientSize.Width - dateWidth - amountWidth - statusWidth - scrollbar;
            if (remaining < 200)
                return;

            var description = Math.Max(120, remaining * 5 / 11);
            var match = Math.Max(140, remaining - description);
            importedGrid.Columns[nameof(ImportedTransactionView.Date)].Width = dateWidth;
            importedGrid.Columns[nameof(ImportedTransactionView.Amount)].Width = amountWidth;
            importedGrid.Columns[nameof(ImportedTransactionView.Description)].Width = description;
            importedGrid.Columns[nameof(ImportedTransactionView.RecommendedMatch)].Width = match;
            importedGrid.Columns[nameof(ImportedTransactionView.Status)].Width = statusWidth;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new() { DataPropertyName = property, HeaderText = header, Name = property };

        private static DataGridViewTextBoxColumn CurrencyColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Format = "c2";
            return column;
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

        private void ClearImportedWaitCursor()
        {
            if (importedGrid is null || importedGrid.Cursor != Cursors.WaitCursor)
                return;

            importedGrid.Cursor = Cursors.Default;
            Cursor.Current = Cursors.Default;
        }

        private void LoadImported(Guid accountId)
        {
            if (importedGrid is null)
                return;

            var previousIds = ImportedRowIds();
            var topId = TopImportedRowId(previousIds);
            var rows = _reconciliationOrchestrator.GetUnreconciled(accountId);
            _importedSource.DataSource = rows;
            if (lblImported is not null)
                lblImported.Text = rows.Count == 0
                    ? "Imported (none unreconciled)"
                    : $"Imported (unreconciled) — {rows.Count}";
            RestoreImportedScroll(topId, previousIds, rows);
        }

        private List<Guid> ImportedRowIds()
        {
            var ids = new List<Guid>();
            if (importedGrid is null)
                return ids;

            foreach (DataGridViewRow row in importedGrid.Rows)
            {
                if (row.DataBoundItem is ImportedTransactionView view)
                    ids.Add(view.Id);
            }

            return ids;
        }

        private Guid? TopImportedRowId(IReadOnlyList<Guid> ids)
        {
            if (importedGrid is null || ids.Count == 0)
                return null;

            var index = importedGrid.FirstDisplayedScrollingRowIndex;
            return index >= 0 && index < ids.Count ? ids[index] : null;
        }

        private void RestoreImportedScroll(
            Guid? topId,
            IReadOnlyList<Guid> previousIds,
            IReadOnlyList<ImportedTransactionView> rows)
        {
            if (importedGrid is null || topId is not Guid anchor || rows.Count == 0)
                return;

            var index = ImportedAnchorIndex(anchor, previousIds, rows);
            if (index < 0)
                return;

            ApplyImportedScroll(index);
            if (importedGrid.IsHandleCreated)
                importedGrid.BeginInvoke(() => ApplyImportedScroll(index));
        }

        private static int ImportedAnchorIndex(
            Guid topId,
            IReadOnlyList<Guid> previousIds,
            IReadOnlyList<ImportedTransactionView> rows)
        {
            var indexById = new Dictionary<Guid, int>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
                indexById[rows[i].Id] = i;

            if (indexById.TryGetValue(topId, out var index))
                return index;

            var previousIndex = -1;
            for (var i = 0; i < previousIds.Count; i++)
            {
                if (previousIds[i] == topId)
                {
                    previousIndex = i;
                    break;
                }
            }

            if (previousIndex < 0)
                return -1;

            for (var i = previousIndex + 1; i < previousIds.Count; i++)
            {
                if (indexById.TryGetValue(previousIds[i], out index))
                    return index;
            }

            return -1;
        }

        private void ApplyImportedScroll(int firstDisplayed)
        {
            if (importedGrid is null || importedGrid.IsDisposed || importedGrid.RowCount == 0)
                return;
            if (!importedGrid.IsHandleCreated || importedGrid.DisplayedRowCount(includePartialRow: false) == 0)
                return;

            var index = Math.Clamp(firstDisplayed, 0, importedGrid.RowCount - 1);
            if (importedGrid.FirstDisplayedScrollingRowIndex != index)
                importedGrid.FirstDisplayedScrollingRowIndex = index;
        }

        private void OnImportedMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || importedGrid is null)
                return;
            var hit = importedGrid.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0 && !importedGrid.Rows[hit.RowIndex].Selected)
            {
                importedGrid.ClearSelection();
                importedGrid.Rows[hit.RowIndex].Selected = true;
                if (hit.ColumnIndex >= 0)
                    importedGrid.CurrentCell = importedGrid[hit.ColumnIndex, hit.RowIndex];
            }

            var location = e.Location;
            importedGrid.BeginInvoke(() => ShowImportedMenu(location));
        }

        private void ShowImportedMenu(Point location)
        {
            if (importedGrid is null || importedGrid.IsDisposed)
                return;

            var selected = SelectedImported().ToList();
            var menu = new ContextMenuStrip();
            var accept = new ToolStripMenuItem("Accept Match");
            accept.Enabled = selected.Count > 0 && selected.All(r => r.RecommendedExpectedId is Guid);
            accept.Click += (_, _) => AcceptImportedMatches(selected);
            var change = new ToolStripMenuItem("Change Match");
            change.Enabled = selected.Count == 1;
            change.Click += (_, _) => ChangeImportedMatch(selected[0]);
            var asNew = new ToolStripMenuItem("Accept as New");
            asNew.Enabled = selected.Count > 0;
            asNew.Click += (_, _) => AcceptImportedAsNew(selected);
            var before = new ToolStripMenuItem("Accept all unreconciled up to last statement");
            before.Enabled = CurrentAccountId is Guid;
            before.Click += (_, _) => AcceptImportedUpToLastStatement();
            menu.Items.Add(accept);
            menu.Items.Add(change);
            menu.Items.Add(asNew);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(before);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(importedGrid, location);
        }

        private void OnLedgerMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            var hit = detailGrid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;
            if (!detailGrid.Rows[hit.RowIndex].Selected)
            {
                detailGrid.ClearSelection();
                detailGrid.Rows[hit.RowIndex].Selected = true;
                if (hit.ColumnIndex >= 0)
                    detailGrid.CurrentCell = detailGrid[hit.ColumnIndex, hit.RowIndex];
            }

            var rowIndex = hit.RowIndex;
            var location = e.Location;
            detailGrid.BeginInvoke(() => ShowLedgerMenu(rowIndex, location));
        }

        private void ShowLedgerMenu(int rowIndex, Point location)
        {
            if (detailGrid.IsDisposed || rowIndex < 0 || rowIndex >= detailGrid.RowCount)
                return;

            var selected = detailGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem)
                .OfType<UnifiedTransactionView>()
                .ToList();
            var current = detailGrid.Rows[rowIndex].DataBoundItem as UnifiedTransactionView;

            var menu = new ContextMenuStrip();
            if (current is not null && TryGetRule(current, out _, out _))
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
            menu.Show(detailGrid, location);
        }

        private void OnLedgerDrag(object? sender, DragEventArgs e)
        {
            e.Effect = DroppedSpreadsheet(e) is not null && CurrentAccountId is Guid
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void OnLedgerDragDrop(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
            var path = DroppedSpreadsheet(e);
            if (path is null)
                return;

            var accountId = CurrentAccountId;
            // The browser stays inside its drag until this handler returns. Open the
            // modal import only after the drop has completed.
            BeginInvoke(() => ImportDroppedFile(path, accountId));
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

        private IEnumerable<ImportedTransactionView> SelectedImported()
        {
            if (importedGrid is null)
                yield break;
            foreach (DataGridViewRow row in importedGrid.SelectedRows)
            {
                if (row.DataBoundItem is ImportedTransactionView view)
                    yield return view;
            }
        }

        private void AcceptImportedMatches(IReadOnlyList<ImportedTransactionView> rows) =>
            ReconcileMany(
                rows,
                row => _reconciliationOrchestrator.AcceptMatch(row.Id, refreshBudgets: false),
                "Accept Match");

        private void AcceptImportedAsNew(IReadOnlyList<ImportedTransactionView> rows) =>
            ReconcileMany(
                rows,
                row => _reconciliationOrchestrator.AcceptAsNew(row.Id, refreshBudgets: false),
                "Accept as New");

        private void ChangeImportedMatch(ImportedTransactionView imported)
        {
            if (CurrentAccountId is not Guid accountId)
                return;
            var choices = _reconciliationOrchestrator.GetUnmatchedExpected(accountId).ToList();
            using var dialog = new ChangeMatchDialog(imported, choices);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;
            try
            {
                if (dialog.TreatAsNew)
                    _reconciliationOrchestrator.AcceptAsNew(imported.Id);
                else if (dialog.SelectedExpectedId is Guid expectedId)
                    _reconciliationOrchestrator.AcceptMatch(imported.Id, expectedId);
                RefreshAll();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Change Match", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FocusAfterImportedReconcile()
        {
            if (IsDisposed)
                return;

            var importedOpen = _ledgerSplit is { IsDisposed: false, Panel2Collapsed: false }
                && importedGrid is { IsDisposed: false, RowCount: > 0 };
            if (importedOpen)
            {
                FocusGrid(importedGrid, selectFirstRow: false);
                return;
            }

            FocusGrid(detailGrid, selectFirstRow: true);
        }

        private static void FocusGrid(DataGridView grid, bool selectFirstRow)
        {
            if (grid.IsDisposed || !grid.IsHandleCreated || !grid.Visible)
                return;

            grid.Focus();
            if (grid.RowCount == 0)
                return;

            var column = FirstVisibleColumnIndex(grid);
            if (column < 0)
                return;

            if (selectFirstRow)
            {
                grid.ClearSelection();
                grid.CurrentCell = grid[column, 0];
                grid.Rows[0].Selected = true;
                return;
            }

            if (grid.CurrentCell is null || !grid.CurrentCell.Visible)
                grid.CurrentCell = grid[column, 0];
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

        private void AcceptImportedUpToLastStatement()
        {
            if (CurrentAccountId is not Guid accountId)
                return;
            var lastStatement = _statements.GetForAccount(accountId)
                .OrderByDescending(s => s.StatementDate)
                .Select(s => s.StatementDate.Date)
                .FirstOrDefault();
            if (lastStatement == default)
            {
                MessageBox.Show(FindForm(),
                    "This account has no statements. Add a statement before accepting unreconciled transactions up to the last statement date.",
                    "Accept Unreconciled",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new AcceptBeforeDateDialog(lastStatement);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Reconciling transactions...", busy: true);
                var result = _reconciliationOrchestrator.AcceptAsNewOnOrBefore(
                    accountId, dialog.OnOrBeforeDate, AppStatus.ForImport());
                _focusAfterReconcileAccountId = accountId;
                RefreshAll();
                AppStatus.Set(ImportStatusText.Reconciled(result));
            }
            catch (Exception ex)
            {
                _focusAfterReconcileAccountId = null;
                AppStatus.Set("Reconcile failed.");
                MessageBox.Show(FindForm(), ex.Message, "Accept Unreconciled",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ReconcileMany(
            IReadOnlyList<ImportedTransactionView> rows,
            Action<ImportedTransactionView> accept,
            string title)
        {
            if (rows.Count == 0)
                return;

            var bulk = rows.Count > 1;
            try
            {
                if (bulk)
                    AppStatus.Set("Reconciling transactions...", busy: true);
                var progress = bulk ? AppStatus.ForImport() : null;
                ImportProgressReporter.Report(progress, 0, rows.Count, activity: "Reconciling", stride: 1);
                for (var i = 0; i < rows.Count; i++)
                {
                    accept(rows[i]);
                    ImportProgressReporter.Report(progress, i + 1, rows.Count, activity: "Reconciling", stride: 1);
                }

                _reconciliationOrchestrator.RefreshBudgetsFor(rows.Select(row => row.Id));
                RefreshAll();
                if (bulk)
                    AppStatus.Set(ImportStatusText.Reconciled(ImportResult.FromDates(rows.Count, rows.Select(r => r.Date))));
            }
            catch (InvalidOperationException ex)
            {
                if (bulk)
                    AppStatus.Set("Reconcile failed.");
                MessageBox.Show(FindForm(), ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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
                MessageBox.Show(FindForm(), ex.Message, "Undo Match", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnForecastPeriodChanged(object? sender, EventArgs e)
        {
            if (!_ready)
                return;

            RefreshCurrentAccount();
        }

        private void OnShowFilterChanged(object? sender, EventArgs e)
        {
            if (!_ready)
                return;

            _filterApplied = SelectedShowMode() == ShowPosted;
            cmbForecastPeriod.Enabled = SelectedShowMode() is ShowAll or ShowForecast;
            UpdateRuleActionButtons();
            RefreshCurrentAccount();
        }

        private string SelectedShowMode() =>
            cmbShow.SelectedItem?.ToString() ?? ShowAll;

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
            var show = SelectedShowMode();
            var history = SelectedHistory();
            var forecastEnd = GetForecastEnd();
            ShowLoadProgress($"Loading {history.ToLowerInvariant()} history...");

            try
            {
                var views = await Task.Run(
                    () => BuildTransactionViews(accountId, show, history, forecastEnd, token));
                if (views is null || token.IsCancellationRequested || CurrentAccountId != accountId)
                    return;

                ShowLoadProgress("Updating grid...");
                _allTransactions = views;
                ApplyLedgerRowFilter();
                LoadImported(accountId);
                UpdateRuleActionButtons();
                if (_focusAfterReconcileAccountId is Guid focusAccount && focusAccount != accountId)
                    _focusAfterReconcileAccountId = null;
                else if (_focusAfterReconcileAccountId == accountId)
                {
                    _focusAfterReconcileAccountId = null;
                    if (IsHandleCreated)
                        BeginInvoke(FocusAfterImportedReconcile);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private List<UnifiedTransactionView>? BuildTransactionViews(
            Guid accountId,
            string show,
            string history,
            DateTime forecastEnd,
            CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return null;
            if (show == ShowRecurringRules)
            {
                var rules = UnifiedTransactionViewBuilder.BuildRecurringRules(
                    _ruleOrchestrator.GetAllSingleRules(),
                    _ruleOrchestrator.GetTransferRules(accountId),
                    accountId);
                ApplyCategoryDisplayNames(rules);
                return rules;
            }

            var chronological = BuildUnifiedTransactions(accountId, show, history, forecastEnd, token);
            if (chronological is null || token.IsCancellationRequested)
                return null;
            ApplyCategoryDisplayNames(chronological);

            if (show != ShowAll)
            {
                ClearForecastBalances(chronological);
                return UnifiedTransactionView.OrderForDisplay(chronological).ToList();
            }

            var historyStart = BaseOrchestrator.GetStartDate(DateTime.Today, history);
            var opening = GetStartingBalance(accountId)
                + _txOrchestrator.SumPostedAmountsBefore(accountId, historyStart);
            ApplyRunningBalances(chronological, opening, accountId);
            return UnifiedTransactionView.OrderForDisplay(chronological).ToList();
        }

        private List<UnifiedTransactionView>? BuildUnifiedTransactions(
            Guid accountId,
            string show,
            string history,
            DateTime forecastEnd,
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

            if (show == ShowPosted)
                return posted;
            if (token.IsCancellationRequested)
                return null;

            var forecast = _txOrchestrator.GenerateForecast(
                accountId,
                DateTime.Today,
                forecastEnd);
            if (token.IsCancellationRequested)
                return null;

            if (show == ShowForecast)
                return forecast;

            return UnifiedTransactionView.OrderForRunningBalance(posted.Concat(forecast)).ToList();
        }

        private DateTime GetForecastEnd()
        {
            return cmbForecastPeriod.SelectedItem?.ToString() switch
            {
                "None" => DateTime.Today,
                "30 days" => DateTime.Today.AddDays(30),
                "60 days" => DateTime.Today.AddDays(60),
                "90 days" => DateTime.Today.AddDays(90),
                "6 months" => DateTime.Today.AddMonths(6),
                "1 year" => DateTime.Today.AddYears(1),
                _ => DateTime.Today.AddDays(90)
            };
        }

        private void ApplyRunningBalances(
            IEnumerable<UnifiedTransactionView> chronological,
            decimal startingBalance,
            Guid accountId)
        {
            var credit = _accountOrchestrator.GetAccount(accountId) is CreditAccount;
            decimal balance = startingBalance;
            foreach (var tx in chronological)
            {
                balance += tx.Amount;
                tx.ForecastBalance = credit ? -balance : balance;
            }
        }

        private decimal GetStartingBalance(Guid accountId)
        {
            return PostedBalanceCalculator.GetStartingBalance(_accountOrchestrator.GetAccount(accountId));
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
            if (_accountsSource.Current is not UnifiedAccountView account)
                return;

            var unified = _transactionsSource.List.Cast<UnifiedTransactionView>().ToList();
            if (SelectedShowMode() != ShowAll || _filterApplied || !string.IsNullOrEmpty(_transactionsSource.Filter))
            {
                ClearForecastBalances(unified);
                detailGrid.Refresh();
                return;
            }

            var chronological = UnifiedTransactionView.OrderForRunningBalance(unified).ToList();
            ApplyRunningBalances(chronological, GetStartingBalance(account.Id), account.Id);
            detailGrid.Refresh();
        }

        private void OnTransactionCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (IsCategoryColumn(detailGrid, CategoryColumn, e.ColumnIndex))
            {
                ShowCategoryMenu(detailGrid, CategoryColumn, e.RowIndex);
                return;
            }

            if (detailGrid.Rows[e.RowIndex].DataBoundItem is not UnifiedTransactionView view)
                return;

            if (TryGetRule(view, out _, out _))
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
            ExcelColumnFilterDropDown.Show(
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
                rows = rows.Where(t => _categoryFilter.Contains(CategoryKey(t)));
            if (!string.IsNullOrWhiteSpace(_descriptionSearch))
            {
                rows = rows.Where(t =>
                    (t.Description ?? "").Contains(_descriptionSearch, StringComparison.CurrentCultureIgnoreCase));
            }

            var list = SelectedShowMode() == ShowAll && !LedgerRowFilterActive
                ? rows.ToList()
                : UnifiedTransactionView.OrderForDisplay(rows).ToList();

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

        private void OnCategoryCellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || !IsCategoryColumn(detailGrid, CategoryColumn, e.ColumnIndex))
                return;
            if (e.Button != MouseButtons.Left)
                return;
            if (!CanEditCategory(detailGrid, e.RowIndex))
                return;

            detailGrid.CurrentCell = detailGrid[e.ColumnIndex, e.RowIndex];
            ShowCategoryMenu(detailGrid, CategoryColumn, e.RowIndex);
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
                category => AssignCategory(view, category.Id),
                () => CreateAndAssignCategory(view),
                OpenCategoryManager,
                () => OpenSplitEditor(view),
                splitSelected);

            var cell = grid.GetCellDisplayRectangle(categoryColumn.Index, rowIndex, cutOverflow: false);
            menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
            menu.Show(grid, new Point(cell.Left, cell.Bottom));
        }

        private void AssignCategory(UnifiedTransactionView view, Guid categoryId)
        {
            _categoryOrchestrator.AssignToPosted(view.LookupId, categoryId, splitRowId: view.SplitRowId);
            RefreshAll();
        }

        private void CreateAndAssignCategory(UnifiedTransactionView view)
        {
            using var editor = new CategoryEditor(_categoryOrchestrator);
            if (editor.ShowDialog(FindForm()) != DialogResult.OK || editor.CreatedCategory is null)
                return;

            AssignCategory(view, editor.CreatedCategory.Id);
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

        private void OnTransactionGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (detailGrid.CurrentCell is { RowIndex: >= 0 } cell
                && IsCategoryColumn(detailGrid, CategoryColumn, cell.ColumnIndex)
                && e.KeyCode is Keys.F2 or Keys.Enter or Keys.Space
                && CanEditCategory(detailGrid, cell.RowIndex))
            {
                ShowCategoryMenu(detailGrid, CategoryColumn, cell.RowIndex);
                e.Handled = true;
                return;
            }

            if (e.KeyCode != Keys.Delete)
                return;

            if (GetSelectedRuleView() is not null)
            {
                DeleteSelectedRule();
                e.Handled = true;
                return;
            }

            if (detailGrid.CurrentRow?.DataBoundItem is UnifiedTransactionView view && CanEditTransaction(view))
            {
                var targets = detailGrid.SelectedRows
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

            var hint = parent switch
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
                initialAmount: parent.Amount,
                initialDate: parent.Date,
                initialCategoryId: parent.CategoryId,
                lockAmount: parent.HasSplits,
                lockCategory: parent.HasSplits,
                categoryIsSplit: parent.HasSplits);
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            try
            {
                _activityOrchestrator.UpdateLedgerEntry(
                    parent.Id,
                    dialog.Date,
                    dialog.Amount,
                    dialog.Description,
                    dialog.CategoryId);
                RefreshAll();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Edit Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show(FindForm(), ex.Message, "Delete Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show(FindForm(), ex.Message, "Split Transaction",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            cmbShow.SelectedItem = ShowPosted;
            _filterApplied = !string.IsNullOrWhiteSpace(type);
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
            ClearImportedWaitCursor();
        }

        private Guid? CurrentAccountId =>
            (_accountsSource.Current as UnifiedAccountView)?.Id;
    }
}
