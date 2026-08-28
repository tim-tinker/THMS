using System.ComponentModel;

using THMS.Domain.Finance.Accounts;
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

        public TransactionManagerControl()
        {
            InitializeComponent();
            InitializeGrids();
            LoadAccounts();
        }

        // ------------------------------------------------------------
        // UI Initialization
        // ------------------------------------------------------------
        private void InitializeGrids()
        {
            masterGrid.AutoGenerateColumns = false;
            detailGrid.AutoGenerateColumns = false;

            detailGrid.DataSource = _transactionsSource;
            masterGrid.DataSource = _accountsSource;

            _accountsSource.CurrentChanged += OnCurrentAccountChanged;
            _transactionsSource.ListChanged += OnTransactionsListChanged;
        }

        // ------------------------------------------------------------
        // Load Accounts (Master Grid)
        // ------------------------------------------------------------
        private void LoadAccounts()
        {
            var accounts = _accountOrchestrator.GetAllAccounts().ToList();
            var unified = UnifiedAccountViewBuilder.Build(accounts);
            _accountsSource.DataSource = unified;
        }

        // ------------------------------------------------------------
        // When an account is selected → load transactions
        // ------------------------------------------------------------
        private void OnCurrentAccountChanged(object? sender, EventArgs e)
        {
            RefreshCurrentAccount();
        }

        // ------------------------------------------------------------
        // Load all transaction types for the selected account
        // ------------------------------------------------------------
        private void LoadTransactionsForAccount(Guid accountId)
        {
            var txs = _txOrchestrator.GetTransactionsForAccount(accountId);

            var unified = UnifiedTransactionViewBuilder.Build(
                txs.Posted,
                txs.PostedTransfers,
                txs.FutureSingles,
                txs.FutureTransfers,
                txs.RecurringSingles,
                txs.RecurringTransfers);

            _transactionsSource.DataSource = unified;

            // Default sort by date
            SortByDateAscending();
        }

        // ------------------------------------------------------------
        // Sorting
        // ------------------------------------------------------------
        private void SortByDateAscending()
        {
            _transactionsSource.Sort = "Date ASC";
        }

        private void ClearForecastBalances(IEnumerable<UnifiedTransactionView> list)
        {
            foreach (var item in list)
                item.ForecastBalance = null;
        }

        private void OnTransactionsListChanged(object? sender, ListChangedEventArgs e)
        {
            var account = _accountsSource.Current as UnifiedAccountView;
            if (account == null) return;

            // Recompute forecast using the current unified list
            var unified = _transactionsSource.List.Cast<UnifiedTransactionView>().ToList();
            ComputeForecastBalances(unified, account.Id);
        }

        // ------------------------------------------------------------
        // Forecast Balance Computation
        // ------------------------------------------------------------
        private void ComputeForecastBalances(List<UnifiedTransactionView> unified, Guid accountId)
        {
            var account = _accountOrchestrator.GetAccount(accountId);

            // Must be sorted by date and a valid account type
            if (CanComputeForecast(account))
            {
                decimal balance = GetPostedBalance(account);

                foreach (var tx in unified)
                {
                    balance += tx.Amount;
                    tx.ForecastBalance = balance;
                }
            }
            else
            {
                ClearForecastBalances(unified);
            }

            detailGrid.Refresh();
        }

        private bool CanComputeForecast(Account? account)
        {
            return _transactionsSource.Sort == "Date ASC" &&
                   string.IsNullOrEmpty(_transactionsSource.Filter) &&
                   (account is BankAccount || account is CreditAccount);
        }


        private decimal GetPostedBalance(Account? account)
        {
            decimal balance = 0;
            if (account is BankAccount bankAccount)
            {
                balance = bankAccount.PostedBalance;
            }
            else if (account is CreditAccount creditAccount)
            {
                balance = creditAccount.PostedBalance;
            }

            return balance;
        }

        // ------------------------------------------------------------
        // Filtering (example)
        // ------------------------------------------------------------
        public void FilterByType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                _transactionsSource.RemoveFilter();
            }
            else
            {
                _transactionsSource.Filter = $"Type = '{type}'";
            }

            // Filtering invalidates forecast balance
            var unified = _transactionsSource.List.Cast<UnifiedTransactionView>().ToList();
            ClearForecastBalances(unified);
            detailGrid.Refresh();
        }

        // ------------------------------------------------------------
        // Refresh button (optional)
        // ------------------------------------------------------------
        public void RefreshCurrentAccount()
        {
            var account = _accountsSource.Current as UnifiedAccountView;
            if (account == null) return;

            LoadTransactionsForAccount(account.Id);
        }
    }
}
