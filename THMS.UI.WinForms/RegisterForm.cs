using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;
using THMS.UI.WinForms.Controls;

namespace THMS.UI.WinForms
{
    public partial class RegisterForm : BaseEmbeddedForm
    {
        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly TransactionImportOrchestrator _importOrchestrator = new();
        private readonly RecurringRuleImportOrchestrator _ruleImportOrchestrator = new();
        private readonly RecurringTransferImportOrchestrator _transferImportOrchestrator = new();
        private readonly PlanningOrchestrator _planningOrchestrator = new();
        private readonly TransactionOrchestrator _transactionOrchestrator = new();
        private readonly CategoryOrchestrator _categoryOrchestrator = new();
        private readonly BindingSource _statementsSource = new();
        private readonly BindingSource _statementDetailsSource = new();
        private readonly BindingSource _statementTransactionsSource = new();
        private readonly ContextMenuStrip _statementMenu = new();
        private bool _suppressStatementMenu;
        private static readonly Guid EstimateStatementId = new("f5e5714a-7e00-4e57-a7e0-000000000001");
        private List<EstimatePeriod> _estimatePeriods = [];

        private List<AccountStatement> _accountStatements = [];
        private List<UnifiedTransactionView> _accountTransactionViews = [];
        private string _descriptionSearch = "";
        private SplitContainer? _statementColumns;
        private SplitContainer? _statementRows;
        private bool _statementColumnsPending = true;
        private bool _lineItemsSplitPending;
        private bool _suspendStatementSelection;
        private int _loadedRevision = int.MinValue;

        private bool _accountSplitReady;

        public RegisterForm()
        {
            InitializeComponent();
            SplitContainerUtil.MakeSplitterVisible(split);
            ledger.HostProvidesAccounts = true;
            categoryManager.Bind(new CategoryOrchestrator(), reload: false);
            categoryManager.CatalogChanged += (_, _) =>
            {
                ledger.RefreshCurrentAccount();
                RefreshStatementTransactions();
            };
            ConfigureStatementGrid();
            tabsTop.RecalculateItemSize();
            tabs.RecalculateItemSize();
            lblSelectedAccount.Height = Math.Max(
                lblSelectedAccount.PreferredHeight,
                lblSelectedAccount.Font.Height + lblSelectedAccount.Padding.Vertical);
            split.SizeChanged += (_, _) => LayoutAccountSplit();
            tabsTop.SelectedIndexChanged += (_, _) =>
            {
                if (tabsTop.SelectedTab == tabCategories)
                {
                    categoryManager.RefreshLayout();
                    categoryManager.ReloadIfClean();
                }
                else
                    LayoutAccountSplit();
            };
            tabs.SelectedIndexChanged += (_, _) =>
            {
                if (tabs.SelectedTab == tabBills)
                    billsControl.Reload();
            };
            billsControl.DataChanged += (_, _) =>
            {
                accountUpdater.RefreshAccounts();
                ledger.SelectAccount(accountUpdater.SelectedAccount?.Id);
            };
            billsControl.StatementChanged += (_, _) => LoadStatementsForSelectedAccount();
            ledger.DataChanged += (_, _) =>
            {
                accountUpdater.RefreshAccounts();
                RefreshStatementTransactions();
            };
            accountUpdater.SelectedAccountChanged += (_, _) => LoadSelectedAccount();
            accountUpdater.StatementsChanged += (_, _) => LoadStatementsForSelectedAccount();
            historyBar.SelectedPeriodChanged += (_, _) => ApplyHistoryPeriod();
        }

        public void ShowHome()
        {
            tabsTop.SelectedTab = tabAccounts;
            tabs.SelectedTab = tabStatements;
            accountUpdater.SelectFirstAccount();
            if (accountUpdater.SelectedAccount is not null)
                LoadStatementsForSelectedAccount();
        }

        public void ShowAccount(Guid accountId)
        {
            tabsTop.SelectedTab = tabAccounts;
            tabs.SelectedTab = tabStatements;
            accountUpdater.SelectAccount(accountId);
        }

        private void LayoutAccountSplit()
        {
            if (_accountSplitReady)
                return;

            const int panel1Min = 140;
            const int panel2Min = 180;
            var available = split.Height - split.SplitterWidth;
            if (available < panel1Min + panel2Min)
                return;

            split.Panel1MinSize = panel1Min;
            split.Panel2MinSize = panel2Min;
            split.SplitterDistance = Math.Clamp(260, panel1Min, available - panel2Min);
            _accountSplitReady = true;
        }

        private void ApplyHistoryPeriod()
        {
            categoryManager.SetHistoryPeriod(historyBar.SelectedPeriod);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || Disposing)
                return;

            categoryManager.ReloadIfClean();

            if (FinanceDataRevision.Current == _loadedRevision)
            {
                billsControl.Reload();
                LoadStatementsForSelectedAccount();
                return;
            }

            accountUpdater.RefreshAccounts();
        }

        private void ConfigureStatementGrid()
        {
            DataGridViewUtil.EnableDoubleBuffering(gridStatements);
            gridStatements.Columns.Clear();
            gridStatements.Columns.AddRange(
                TextColumn(nameof(AccountStatementListRow.Type), "Type"),
                TextColumn(nameof(AccountStatementListRow.StatementDate), "Statement Date"),
                TextColumn(nameof(AccountStatementListRow.DueDate), "Due Date"),
                MoneyColumn(nameof(AccountStatementListRow.AmountDue), "Amount Due"),
                MoneyColumn(nameof(AccountStatementListRow.StatementBalance), "Statement Balance"),
                MoneyColumn(nameof(AccountStatementListRow.EscrowBalance), "Escrow Balance"),
                FillColumn(nameof(AccountStatementListRow.Notes), "Notes"));
            gridStatements.DataSource = _statementsSource;
            gridStatements.SelectionChanged += (_, _) =>
            {
                if (!_suspendStatementSelection)
                    LoadStatementDetails();
            };
            gridStatements.CellDoubleClick += OnStatementCellDoubleClick;
            gridStatements.MouseDown += OnStatementGridMouseDown;
            gridStatements.ContextMenuStrip = _statementMenu;
            _statementMenu.Opening += OnStatementMenuOpening;
            HostStatementDetails();
        }

        private void HostStatementDetails()
        {
            var splitStatements = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel2Collapsed = true,
                Name = "splitStatements"
            };
            pnlStatements.Controls.Remove(gridStatements);
            gridStatements.Dock = DockStyle.Fill;
            splitStatements.Panel1.Controls.Add(gridStatements);

            var gridDetails = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                Name = "gridStatementDetails",
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            DataGridViewUtil.EnableDoubleBuffering(gridDetails);
            var amountColumn = TextColumn(nameof(StatementChildRow.Amount), "Amount");
            amountColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            var detailsColumn = TextColumn(nameof(StatementChildRow.Details), "Details");
            detailsColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            gridDetails.Columns.AddRange(
                TextColumn(nameof(StatementChildRow.Kind), "Kind"),
                TextColumn(nameof(StatementChildRow.Description), "Description"),
                amountColumn,
                detailsColumn);
            gridDetails.DataSource = _statementDetailsSource;
            gridDetails.CellDoubleClick += OnStatementCellDoubleClick;
            splitStatements.Panel2.Controls.Add(SectionHost(gridDetails, "Line items"));
            splitStatements.SizeChanged += (_, _) => OnStatementRowsSized();
            _statementRows = splitStatements;

            var gridTransactions = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                Name = "gridStatementTransactions",
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            DataGridViewUtil.EnableDoubleBuffering(gridTransactions);
            gridTransactions.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            var txDate = TextColumn(nameof(UnifiedTransactionView.Date), "Transaction Date");
            txDate.DefaultCellStyle.Format = "d";
            var txAmount = TextColumn(nameof(UnifiedTransactionView.Amount), "Amount");
            txAmount.DefaultCellStyle.Format = "c2";
            txAmount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            txAmount.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            var txDescription = TextColumn(nameof(UnifiedTransactionView.Description), "Description");
            txDescription.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            txDescription.MinimumWidth = 120;
            gridTransactions.Columns.AddRange(
                txDate,
                txAmount,
                TextColumn(nameof(UnifiedTransactionView.Category), "Category"),
                txDescription);
            gridTransactions.DataSource = _statementTransactionsSource;
            gridTransactions.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != txAmount.Index)
                    return;
                if (e.Value is decimal amount)
                    e.Value = PostedBalanceCalculator.ToDisplayBalance(accountUpdater.SelectedAccount, amount);
            };
            ledger.AttachTransactionActions(gridTransactions, () => accountUpdater.SelectedAccount?.Id);

            var columns = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Name = "splitStatementColumns"
            };
            columns.Panel1.Controls.Add(splitStatements);
            columns.Panel2.Controls.Add(gridTransactions);
            columns.SizeChanged += (_, _) => OnStatementColumnsSized();
            _statementColumns = columns;

            pnlStatements.Controls.Add(columns);
            // Dock the button bar first so the grids fill only the space above it.
            columns.BringToFront();
            SplitContainerUtil.MakeSplitterVisible(splitStatements);
            SplitContainerUtil.MakeSplitterVisible(columns);
        }

        private Panel SectionHost(Control body, string title)
        {
            var host = new Panel { Dock = DockStyle.Fill };
            var font = new Font(Font.FontFamily, Font.Size + 3f, FontStyle.Bold);
            var labelPad = LogicalToDeviceUnits(6);
            var labelHeight = TextRenderer.MeasureText(title, font).Height + (labelPad * 2);
            var label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Font = font,
                Height = Math.Max(LogicalToDeviceUnits(32), labelHeight),
                Padding = new Padding(LogicalToDeviceUnits(8), labelPad, LogicalToDeviceUnits(8), labelPad),
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            host.Controls.Add(body);
            host.Controls.Add(label);
            return host;
        }

        private void TryApplyStatementColumnSplit()
        {
            if (!_statementColumnsPending || _statementColumns is null || _statementColumns.Panel2Collapsed || !_statementColumns.IsHandleCreated)
                return;

            const int min = 220;
            var available = _statementColumns.Width - _statementColumns.SplitterWidth;
            if (available < min * 2)
                return;

            _statementColumns.Panel1MinSize = min;
            _statementColumns.Panel2MinSize = min;
            _statementColumns.SplitterDistance = available / 2;
            _statementColumnsPending = false;
        }

        private void OnStatementColumnsSized() => TryApplyStatementColumnSplit();

        private void OnStatementRowsSized()
        {
            if (_statementRows is null)
                return;

            var show = _statementDetailsSource.Count > 0;
            if (_statementRows.Panel2Collapsed == show)
                UpdateLineItemsSplit();
            else
                TryApplyLineItemsSplitDistance();
        }

        private void UpdateLineItemsSplit()
        {
            if (_statementRows is null || _statementRows.IsDisposed)
                return;

            var show = _statementDetailsSource.Count > 0;
            if (_statementRows.Panel2Collapsed == !show)
                return;

            try
            {
                if (!show)
                {
                    _lineItemsSplitPending = false;
                    _statementRows.Panel2Collapsed = true;
                    return;
                }

                _lineItemsSplitPending = true;
                _statementRows.Panel2Collapsed = false;
                TryApplyLineItemsSplitDistance();
            }
            catch (InvalidOperationException)
            {
                _lineItemsSplitPending = show;
            }
        }

        private void TryApplyLineItemsSplitDistance()
        {
            if (!_lineItemsSplitPending || _statementRows is null || _statementRows.Panel2Collapsed || !_statementRows.IsHandleCreated)
                return;

            const int min = 80;
            var available = _statementRows.Height - _statementRows.SplitterWidth;
            if (available < min * 2)
                return;

            _statementRows.Panel1MinSize = min;
            _statementRows.Panel2MinSize = min;
            _statementRows.SplitterDistance = Math.Clamp((available * 2) / 3, min, available - min);
            _lineItemsSplitPending = false;
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property,
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            return column;
        }

        private static DataGridViewTextBoxColumn FillColumn(string property, string header)
        {
            var column = TextColumn(property, header);
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.MinimumWidth = 120;
            return column;
        }

        private void LoadSelectedAccount()
        {
            var name = accountUpdater.SelectedAccount?.Name;
            lblSelectedAccount.Text = string.IsNullOrWhiteSpace(name)
                ? "Account:"
                : $"Account: {name}";
            billsControl.SelectAccount(accountUpdater.SelectedAccount?.Id);
            ledger.SelectAccount(accountUpdater.SelectedAccount?.Id);
            LoadStatementsForSelectedAccount();
            _loadedRevision = FinanceDataRevision.Current;
        }

        private void OnDescriptionSearchChanged(object? sender, EventArgs e)
        {
            _descriptionSearch = txtDescriptionSearch.Text.Trim();
            ApplyStatementList(SelectedStatementId());
        }

        private void LoadStatementsForSelectedAccount(bool keepSelection = true)
        {
            var account = accountUpdater.SelectedAccount;
            if (account is null)
            {
                _accountStatements = [];
                _accountTransactionViews = [];
                btnAddStatement.Enabled = false;
                ApplyStatementList(null);
                return;
            }

            btnAddStatement.Enabled = StatementAccountMatch.ForAccount(account) is not null;
            _accountStatements = _planningOrchestrator.GetStatementStore().GetForAccount(account.Id).ToList();
            ReloadStatementViews(keepSelection ? SelectedStatementId() : null);
        }

        private void RefreshStatementTransactions() => ReloadStatementViews(SelectedStatementId());

        private void ReloadStatementViews(Guid? preferStatementId)
        {
            var account = accountUpdater.SelectedAccount;
            if (account is null)
            {
                _accountTransactionViews = [];
                ApplyStatementList(null);
                return;
            }

            _accountTransactionViews = BuildAccountTransactionViews(account.Id);
            ApplyStatementList(preferStatementId);
        }

        private Guid? SelectedStatementId() =>
            gridStatements.CurrentRow?.DataBoundItem is AccountStatementListRow row ? row.Id : null;

        private void ApplyStatementList(Guid? preferStatementId)
        {
            var ordered = ListedStatements();
            var visible = string.IsNullOrWhiteSpace(_descriptionSearch)
                ? ordered
                : ordered.Where(StatementHasMatch).ToList();
            var selectId = ChooseStatementId(ordered, visible, preferStatementId);

            if (StatementGridShows(visible))
            {
                if (selectId is Guid id && SelectedStatementId() == id)
                    LoadStatementDetails();
                else if (selectId is Guid nextId)
                    SelectStatementRow(nextId);
                else
                    ClearStatementSelection();
                gridStatements.Refresh();
                return;
            }

            var rows = visible.Select(ToStatementRow).ToList();
            _suspendStatementSelection = true;
            try
            {
                _statementsSource.DataSource = rows;
                _statementsSource.ResetBindings(false);
                if (selectId is Guid id)
                {
                    var index = rows.FindIndex(row => row.Id == id);
                    if (index >= 0)
                        _statementsSource.Position = index;
                }
            }
            finally
            {
                _suspendStatementSelection = false;
            }

            if (selectId is Guid statementId)
                SelectStatementRow(statementId);
            else
                ClearStatementSelection();
            gridStatements.Refresh();
        }

        private List<ListedStatement> ListedStatements()
        {
            var ordered = _accountStatements
                .OrderByDescending(statement => statement.StatementDate)
                .ThenByDescending(statement => statement.DueDate)
                .Select(statement => new ListedStatement(statement.Id, statement))
                .ToList();
            RebuildEstimatePeriods();
            foreach (var period in _estimatePeriods)
                ordered.Insert(0, new ListedStatement(period.Id, null));
            return ordered;
        }

        private static Guid? ChooseStatementId(
            IReadOnlyList<ListedStatement> ordered,
            IReadOnlyList<ListedStatement> visible,
            Guid? preferStatementId)
        {
            if (visible.Count == 0)
                return null;
            if (preferStatementId is Guid prefer && visible.Any(statement => statement.Id == prefer))
                return prefer;

            if (preferStatementId is Guid current)
            {
                var index = -1;
                for (var i = 0; i < ordered.Count; i++)
                {
                    if (ordered[i].Id == current)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0)
                {
                    for (var i = index + 1; i < ordered.Count; i++)
                    {
                        var candidate = ordered[i];
                        if (visible.Any(statement => statement.Id == candidate.Id))
                            return candidate.Id;
                    }
                }
            }

            return visible.FirstOrDefault(statement => statement.Statement is not null)?.Id ?? visible[0].Id;
        }

        private bool StatementGridShows(IReadOnlyList<ListedStatement> visible)
        {
            if (gridStatements.Rows.Count != visible.Count)
                return false;

            for (var i = 0; i < visible.Count; i++)
            {
                if (gridStatements.Rows[i].DataBoundItem is not AccountStatementListRow row)
                    return false;

                var expected = ToStatementRow(visible[i]);
                if (!row.SameDisplayedColumns(expected) || !CellsShow(gridStatements.Rows[i], expected))
                    return false;
            }

            return true;
        }

        private static bool CellsShow(DataGridViewRow gridRow, AccountStatementListRow expected) =>
            CellShows(gridRow, nameof(AccountStatementListRow.StatementDate), expected.StatementDate)
            && CellShows(gridRow, nameof(AccountStatementListRow.DueDate), expected.DueDate)
            && CellShows(gridRow, nameof(AccountStatementListRow.AmountDue), expected.AmountDue)
            && CellShows(gridRow, nameof(AccountStatementListRow.StatementBalance), expected.StatementBalance)
            && CellShows(gridRow, nameof(AccountStatementListRow.EscrowBalance), expected.EscrowBalance)
            && CellShows(gridRow, nameof(AccountStatementListRow.Notes), expected.Notes);

        private static bool CellShows(DataGridViewRow gridRow, string column, string expected)
        {
            if (gridRow.DataGridView?.Columns[column] is not DataGridViewColumn found)
                return false;

            var cell = gridRow.Cells[found.Index];
            return SameCellText(cell.Value, expected) && SameCellText(cell.FormattedValue, expected);
        }

        private static bool SameCellText(object? value, string expected) =>
            string.Equals(value?.ToString() ?? "", expected ?? "", StringComparison.Ordinal);

        private AccountStatementListRow ToStatementRow(ListedStatement item)
        {
            if (item.Statement is AccountStatement statement)
                return AccountStatementListRow.From(statement);

            var period = _estimatePeriods.First(candidate => candidate.Id == item.Id);
            var latest = _accountStatements
                .OrderByDescending(statement => statement.StatementDate)
                .ThenByDescending(statement => statement.DueDate)
                .First();
            return new AccountStatementListRow
            {
                Id = period.Id,
                Type = AccountStatementListRow.DisplayType(latest.Type),
                StatementDate = period.End.ToString("d"),
                Notes = "Estimate"
            };
        }

        private void RebuildEstimatePeriods()
        {
            _estimatePeriods = [];
            if (_accountStatements.Count == 0 || _accountTransactionViews.Count == 0)
                return;

            var latest = _accountStatements.Max(statement => statement.StatementDate.Date);
            var latestTransaction = _accountTransactionViews.Max(view => view.Date.Date);
            if (latestTransaction <= latest)
                return;

            var previousEnd = latest;
            for (var months = 1; ; months++)
            {
                var end = latest.AddMonths(months);
                if (end <= previousEnd)
                    break;

                var start = previousEnd.AddDays(1);
                if (_accountTransactionViews.Any(view => view.Date.Date >= start && view.Date.Date <= end))
                    _estimatePeriods.Add(new EstimatePeriod(EstimateId(end), start, end));

                if (latestTransaction <= end)
                    break;

                previousEnd = end;
            }
        }

        private static Guid EstimateId(DateTime end)
        {
            var bytes = EstimateStatementId.ToByteArray();
            var day = end.Year * 372 + (end.Month - 1) * 31 + (end.Day - 1);
            BitConverter.GetBytes(day).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        private bool TryEstimatePeriod(Guid id, out EstimatePeriod period)
        {
            foreach (var candidate in _estimatePeriods)
            {
                if (candidate.Id != id)
                    continue;

                period = candidate;
                return true;
            }

            period = default;
            return false;
        }

        private void SelectStatementRow(Guid statementId)
        {
            for (var i = 0; i < gridStatements.Rows.Count; i++)
            {
                if (gridStatements.Rows[i].DataBoundItem is not AccountStatementListRow row || row.Id != statementId)
                    continue;

                var column = gridStatements.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
                if (column is not null)
                {
                    try
                    {
                        gridStatements.CurrentCell = gridStatements.Rows[i].Cells[column.Index];
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                LoadStatementDetails();
                return;
            }

            ClearStatementSelection();
        }

        private void ClearStatementSelection()
        {
            _statementDetailsSource.DataSource = new List<StatementChildRow>();
            _statementTransactionsSource.DataSource = new List<UnifiedTransactionView>();
            UpdateLineItemsSplit();
        }

        private void LoadStatementDetails()
        {
            if (gridStatements.CurrentRow?.DataBoundItem is not AccountStatementListRow row)
            {
                ClearStatementSelection();
                return;
            }

            if (TryEstimatePeriod(row.Id, out var period))
            {
                _statementDetailsSource.DataSource = new List<StatementChildRow>();
                _statementTransactionsSource.DataSource = TransactionsInWindow(period.Start, period.End);
                UpdateLineItemsSplit();
                return;
            }

            var statement = _planningOrchestrator.GetStatement(row.Id);
            _statementDetailsSource.DataSource = statement is null
                ? new List<StatementChildRow>()
                : StatementChildRow.From(statement);
            _statementTransactionsSource.DataSource = statement is null
                ? new List<UnifiedTransactionView>()
                : StatementTransactions(statement);
            UpdateLineItemsSplit();
        }

        private List<UnifiedTransactionView> BuildAccountTransactionViews(Guid accountId)
        {
            var txs = _transactionOrchestrator.GetTransactionsForAccount(accountId);
            var views = UnifiedTransactionViewBuilder.Build(
                txs.Posted,
                txs.PostedTransfers,
                userFutureSingles: [],
                userFutureTransfers: [],
                accountId,
                txs.IncomingTransferSplitPosted,
                incomingFutureSplitSources: []);
            var names = _categoryOrchestrator.GetAllCategories(includeInactive: true)
                .ToDictionary(category => category.Id, category => category.Name);
            foreach (var view in views)
            {
                if (view.CategoryId is Guid id && names.TryGetValue(id, out var name))
                    view.Category = name;
            }

            return views;
        }

        private bool StatementHasMatch(ListedStatement item)
        {
            if (item.Statement is AccountStatement statement)
            {
                var window = StatementWindow(statement);
                return HasTransactionInWindow(window.Start, window.End);
            }

            return TryEstimatePeriod(item.Id, out var period) && HasTransactionInWindow(period.Start, period.End);
        }

        private bool HasTransactionInWindow(DateTime start, DateTime end) =>
            _accountTransactionViews.Any(view =>
                view.Date.Date >= start
                && view.Date.Date <= end
                && DescriptionMatches(view));

        private List<UnifiedTransactionView> StatementTransactions(AccountStatement statement)
        {
            var (start, end) = StatementWindow(statement);
            return TransactionsInWindow(start, end);
        }

        private List<UnifiedTransactionView> TransactionsInWindow(DateTime start, DateTime end) =>
            UnifiedTransactionView.OrderForDisplay(
                _accountTransactionViews.Where(view =>
                    view.Date.Date >= start
                    && view.Date.Date <= end
                    && DescriptionMatches(view))).ToList();

        private (DateTime Start, DateTime End) StatementWindow(AccountStatement statement)
        {
            var end = statement.StatementDate.Date;
            var previous = _accountStatements
                .Where(item => item.Id != statement.Id && item.StatementDate.Date < end)
                .Select(item => item.StatementDate.Date)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();
            var start = previous == DateTime.MinValue ? DateTime.MinValue : previous.AddDays(1);
            return (start, end);
        }

        private bool DescriptionMatches(UnifiedTransactionView view) =>
            string.IsNullOrWhiteSpace(_descriptionSearch)
            || (view.Description ?? "").Contains(_descriptionSearch, StringComparison.CurrentCultureIgnoreCase);

        private sealed record ListedStatement(Guid Id, AccountStatement? Statement);

        private readonly record struct EstimatePeriod(Guid Id, DateTime Start, DateTime End);

        private void AfterImport()
        {
            accountUpdater.RefreshAccounts();
            LoadSelectedAccount();
        }

        public void RefreshAfterExternalData() => AfterImport();

        private void OnImportAccounts(object? sender, EventArgs e)
        {
            accountUpdater.ImportAccountsFromFile();
            AfterImport();
        }

        private void OnImportCategories(object? sender, EventArgs e)
        {
            categoryManager.ImportFromFile();
            AfterImport();
        }

        private void OnLinkPlaidAccounts(object? sender, EventArgs e)
        {
            using var dialog = new PlaidAccountSetupDialog();
            dialog.ShowDialog(this);
            AfterImport();
        }

        private void OnImportFromFile(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select transaction spreadsheet",
                Multiselect = true
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing transactions...", busy: true);
                var rows = _importOrchestrator.LoadTransactionsFromFiles(fileDialog.FileNames);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this, "The selected file(s) did not contain any transactions.", "Import Transactions",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _importOrchestrator.ImportTransactions(rows, AppStatus.ForImport());
                AfterImport();
                AppStatus.Set(ImportStatusText.Imported(result, "transaction", "transactions"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                ExceptionDialog.Show(this, "Import Transactions", ex, "Could not parse the file(s).");
            }
        }

        private async void OnImportFromPlaid(object? sender, EventArgs e)
        {
            plaidSyncDataItem.Enabled = false;
            try
            {
                AppStatus.Set("Syncing Plaid...", busy: true);
                var orchestrator = new PlaidTransactionOrchestrator();
                var progress = new Progress<PlaidSyncProgress>(AppStatus.Report);
                var result = await orchestrator.SyncIncrementalAsync(progress: progress);
                AfterImport();
                AppStatus.Set(result.Summary);
            }
            catch (Exception ex)
            {
                AppStatus.Set("Plaid sync failed.");
                ExceptionDialog.Show(this, "Plaid", ex, "Plaid sync failed.");
            }
            finally
            {
                plaidSyncDataItem.Enabled = true;
            }
        }

        private void OnImportTransactionRules(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select transaction rules spreadsheet"
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing transaction rules...", busy: true);
                var rows = _ruleImportOrchestrator.LoadRulesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this,
                        "The selected file did not contain any transaction rules for known accounts.",
                        "Import Transaction Rules",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _ruleImportOrchestrator.ImportRules(rows, AppStatus.ForImport());
                AfterImport();
                AppStatus.Set(ImportStatusText.Imported(result, "transaction rule", "transaction rules"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                ExceptionDialog.Show(this, "Import Transaction Rules", ex, "Could not parse the file.");
            }
        }

        private void OnImportTransferRules(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select transfer rules spreadsheet"
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing transfer rules...", busy: true);
                var rows = _transferImportOrchestrator.LoadRulesFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this,
                        "The selected file did not contain any transfer rules for known accounts.",
                        "Import Transfer Rules",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _transferImportOrchestrator.ImportRules(rows, AppStatus.ForImport());
                AfterImport();
                AppStatus.Set(ImportStatusText.Imported(result, "transfer rule", "transfer rules"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                ExceptionDialog.Show(this, "Import Transfer Rules", ex, "Could not parse the file.");
            }
        }

        private void OnAddStatement(object? sender, EventArgs e) =>
            OpenStatementEditor(existing: null, accountUpdater.SelectedAccount);

        private void OnStatementGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = gridStatements.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0)
                return;

            var alreadyCurrent = gridStatements.CurrentCell?.RowIndex == hit.RowIndex
                && (hit.ColumnIndex < 0 || gridStatements.CurrentCell?.ColumnIndex == hit.ColumnIndex)
                && DataGridViewUtil.CellIsSelected(gridStatements, hit.RowIndex, hit.ColumnIndex);
            if (!alreadyCurrent)
            {
                if (!DataGridViewUtil.SelectOnlyCell(gridStatements, hit.RowIndex, hit.ColumnIndex))
                    return;

                // A current-cell change made while the right button is down does not
                // raise SelectionChanged, so the line items and transactions stay put.
                LoadStatementDetails();
            }

            var location = e.Location;
            _suppressStatementMenu = true;
            gridStatements.BeginInvoke(() =>
            {
                _suppressStatementMenu = false;
                if (!gridStatements.IsDisposed)
                    _statementMenu.Show(gridStatements, location);
            });
        }

        private void OnStatementMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _statementMenu.Items.Clear();
            if (_suppressStatementMenu || !TryValidatableStatement(out var statement, out var previous))
            {
                e.Cancel = true;
                return;
            }

            var validate = new ToolStripMenuItem("Validate");
            validate.Click += (_, _) => ShowStatementValidation(statement, previous);
            _statementMenu.Items.Add(validate);
        }

        private bool TryValidatableStatement(out AccountStatement statement, out AccountStatement previous)
        {
            statement = null!;
            previous = null!;
            if (gridStatements.CurrentRow?.DataBoundItem is not AccountStatementListRow row
                || TryEstimatePeriod(row.Id, out _))
                return false;

            var current = _accountStatements.FirstOrDefault(item => item.Id == row.Id);
            if (current is null or UtilityStatement)
                return false;

            var older = OlderStatement(current);
            if (older is null)
                return false;

            statement = current;
            previous = older;
            return true;
        }

        private AccountStatement? OlderStatement(AccountStatement statement)
        {
            var end = statement.StatementDate.Date;
            return _accountStatements
                .Where(item => item.Id != statement.Id && item.StatementDate.Date < end)
                .OrderByDescending(item => item.StatementDate)
                .ThenByDescending(item => item.DueDate)
                .ThenByDescending(item => item.Id)
                .FirstOrDefault();
        }

        private void ShowStatementValidation(AccountStatement statement, AccountStatement previous)
        {
            var account = accountUpdater.SelectedAccount;
            if (account is null)
                return;

            var (start, end) = StatementWindow(statement);
            var transactions = _accountTransactionViews
                .Where(view => view.Date.Date >= start && view.Date.Date <= end)
                .ToList();
            var validation = StatementBalanceValidation.Build(account, statement, previous, transactions);
            using var dialog = new StatementValidationDialog(account.Name, validation);
            dialog.ShowDialog(this);
        }

        private void OnStatementCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (gridStatements.CurrentRow?.DataBoundItem is not AccountStatementListRow row
                || TryEstimatePeriod(row.Id, out _))
                return;

            var statement = _planningOrchestrator.GetStatement(row.Id);
            if (statement is null)
            {
                MessageBox.Show(this, "The selected statement was not found.", "Edit Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            OpenStatementEditor(statement, accountUpdater.SelectedAccount);
        }

        private void OpenStatementEditor(AccountStatement? existing, Account? account)
        {
            account ??= accountUpdater.SelectedAccount;
            if (account is null)
            {
                MessageBox.Show(this, "Select an account to add a statement.", "Add Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (StatementAccountMatch.ForAccount(account) is null)
            {
                MessageBox.Show(this, $"Statements are not supported for {account.Name}.", "Add Statement",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var dlg = new StatementEditorDialog(_planningOrchestrator, existing, account);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                LoadStatementsForSelectedAccount(keepSelection: existing is not null);
                billsControl.Reload();
                accountUpdater.RefreshAccounts();
            }
            catch (Exception ex)
            {
                ExceptionDialog.Show(this, existing is null ? "Add Statement" : "Edit Statement", ex);
            }
        }

        private void OnImportStatements(object? sender, EventArgs e)
        {
            using var fileDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select statement spreadsheet"
            };
            if (fileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                AppStatus.Set("Importing statements...", busy: true);
                var import = new StatementImportOrchestrator();
                var rows = import.LoadStatementsFromFile(fileDialog.FileName);
                if (rows.Count == 0)
                {
                    AppStatus.Set("Ready.");
                    MessageBox.Show(this,
                        "The selected file did not contain any statements for accounts that already exist.",
                        "Import Statements",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = import.ImportStatements(rows, AppStatus.ForImport());
                LoadStatementsForSelectedAccount(keepSelection: false);
                billsControl.Reload();
                accountUpdater.RefreshAccounts();
                AppStatus.Set(ImportStatusText.Imported(result, "statement", "statements"));
            }
            catch (Exception ex)
            {
                AppStatus.Set("Import failed.");
                ExceptionDialog.Show(this, "Import Statements", ex, "Could not parse the file.");
            }
        }
    }
}
