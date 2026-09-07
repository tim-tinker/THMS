using System.ComponentModel;

using THMS.Domain.Finance.Accounts;
using THMS.Logic.Finance.Model;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionManagerControl : UserControl
    {
        private readonly AccountOrchestrator _accountOrchestrator = new();
        private readonly TransactionOrchestrator _txOrchestrator = new();

        private BindingSource _accountsSource = new BindingSource();
        private BindingSource _transactionsSource = new BindingSource();
        private bool _filterApplied;
        private decimal _postedBalanceBeforeEdit;

        public TransactionManagerControl()
        {
            InitializeComponent();
            InitializeForecastPeriod();
            InitializeGrids();
            LoadAccounts();
        }

        private void InitializeForecastPeriod()
        {
            cmbForecastPeriod.Items.Clear();
            cmbForecastPeriod.Items.AddRange(["30 days", "60 days", "90 days", "6 months", "1 year"]);
            cmbForecastPeriod.SelectedIndex = 2;
            cmbForecastPeriod.SelectedIndexChanged += OnForecastPeriodChanged;
        }

        private void InitializeGrids()
        {
            masterGrid.AutoGenerateColumns = false;
            detailGrid.AutoGenerateColumns = false;

            detailGrid.DataSource = _transactionsSource;
            masterGrid.DataSource = _accountsSource;

            _accountsSource.CurrentChanged += OnCurrentAccountChanged;
            _transactionsSource.ListChanged += OnTransactionsListChanged;

            masterGrid.CellBeginEdit += OnAccountCellBeginEdit;
            masterGrid.CellValidating += OnAccountCellValidating;
            masterGrid.CellParsing += OnAccountCellParsing;
            masterGrid.CellEndEdit += OnAccountPostedBalanceEdited;
            masterGrid.DataError += OnAccountGridDataError;
            masterGrid.CellDoubleClick += OnAccountCellDoubleClick;
        }

        private void LoadAccounts()
        {
            var accounts = _accountOrchestrator.GetAllAccounts().ToList();
            foreach (var account in accounts)
                ApplyPostedBalance(account);

            _accountsSource.DataSource = UnifiedAccountViewBuilder.Build(accounts);
        }

        private void ApplyPostedBalance(Account account)
        {
            if (account is not BankAccount and not CreditAccount)
                return;

            var postedBalance = _txOrchestrator.ComputePostedBalance(
                account.Id,
                PostedBalanceCalculator.GetStartingBalance(account));
            PostedBalanceCalculator.ApplyPostedBalance(account, postedBalance);
        }

        private void OnCurrentAccountChanged(object? sender, EventArgs e)
        {
            RefreshCurrentAccount();
        }

        private void OnForecastPeriodChanged(object? sender, EventArgs e)
        {
            RefreshCurrentAccount();
        }

        private void LoadTransactionsForAccount(Guid accountId)
        {
            var chronological = BuildUnifiedTransactions(accountId);

            if (_filterApplied)
            {
                chronological = chronological.Where(t => !t.IsForecasted).ToList();
                ClearForecastBalances(chronological);
            }
            else
            {
                ApplyRunningBalances(chronological, GetStartingBalance(accountId));
            }

            _transactionsSource.DataSource = chronological
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList();
        }

        private List<UnifiedTransactionView> BuildUnifiedTransactions(Guid accountId)
        {
            var txs = _txOrchestrator.GetTransactionsForAccount(accountId);
            var posted = UnifiedTransactionViewBuilder.Build(
                txs.Posted,
                txs.PostedTransfers,
                txs.FutureSingles,
                txs.FutureTransfers);

            if (_filterApplied)
                return posted;

            var forecast = _txOrchestrator.GenerateForecast(
                accountId,
                DateTime.Today,
                GetForecastEnd());

            return posted.Concat(forecast)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .ToList();
        }

        private DateTime GetForecastEnd()
        {
            return cmbForecastPeriod.SelectedItem?.ToString() switch
            {
                "30 days" => DateTime.Today.AddDays(30),
                "60 days" => DateTime.Today.AddDays(60),
                "90 days" => DateTime.Today.AddDays(90),
                "6 months" => DateTime.Today.AddMonths(6),
                "1 year" => DateTime.Today.AddYears(1),
                _ => DateTime.Today.AddDays(90)
            };
        }

        private void ApplyRunningBalances(IEnumerable<UnifiedTransactionView> chronological, decimal startingBalance)
        {
            decimal balance = startingBalance;
            foreach (var tx in chronological)
            {
                balance += tx.Amount;
                tx.ForecastBalance = balance;
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
            var delta = entered - _postedBalanceBeforeEdit;
            if (delta == 0)
                return;

            try
            {
                _accountOrchestrator.AdjustStartingBalanceForPostedDelta(view.Id, delta);
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

        private bool IsPostedBalanceColumn(int columnIndex) =>
            columnIndex >= 0 && masterGrid.Columns[columnIndex] == BalanceColumn;

        private bool IsEditablePostedBalanceCell(int rowIndex, int columnIndex)
        {
            if (!IsPostedBalanceColumn(columnIndex) || rowIndex < 0)
                return false;

            return GetAccountView(rowIndex)?.AccountType is "Bank" or "Credit";
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
            if (_accountsSource.Current is not UnifiedAccountView account)
                return;

            var unified = _transactionsSource.List.Cast<UnifiedTransactionView>().ToList();
            if (_filterApplied || !string.IsNullOrEmpty(_transactionsSource.Filter))
            {
                ClearForecastBalances(unified);
                detailGrid.Refresh();
                return;
            }

            var chronological = unified.OrderBy(t => t.Date).ThenBy(t => t.Id).ToList();
            ApplyRunningBalances(chronological, GetStartingBalance(account.Id));
            detailGrid.Refresh();
        }

        public void FilterByType(string type)
        {
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
        }

        public void RefreshCurrentAccount()
        {
            if (_accountsSource.Current is not UnifiedAccountView account)
                return;

            LoadTransactionsForAccount(account.Id);
        }
    }
}
