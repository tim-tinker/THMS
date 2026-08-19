using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.ViewModels.Finance;

namespace THMS.UI.WinForms.Controls
{
    public partial class TransactionManagerControl : UserControl
    {
        private readonly ITransactionDataStore _txStore;
        private readonly IAccountDataStore _accountStore;

        private BindingSource _accountsSource = new BindingSource();
        private BindingSource _transactionsSource = new BindingSource();

        public TransactionManagerControl(
            ITransactionDataStore txStore,
            IAccountDataStore accountStore)
        {
            _txStore = txStore;
            _accountStore = accountStore;

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

            masterGrid.SelectionChanged += MasterGrid_SelectionChanged;

            detailGrid.DataSource = _transactionsSource;
            masterGrid.DataSource = _accountsSource;
        }

        // ------------------------------------------------------------
        // Load Accounts (Master Grid)
        // ------------------------------------------------------------
        private void LoadAccounts()
        {
            var accounts = _accountStore.GetAllAccounts().ToList();
            var unified = UnifiedAccountViewBuilder.Build(accounts);
            _accountsSource.DataSource = unified;
        }

        // ------------------------------------------------------------
        // When an account is selected → load transactions
        // ------------------------------------------------------------
        private void MasterGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (masterGrid.SelectedRows.Count == 0)
                return;

            var account = masterGrid.SelectedRows[0].DataBoundItem as Account;
            if (account == null)
                return;

            LoadTransactionsForAccount(account.Id);
        }

        // ------------------------------------------------------------
        // Load all transaction types for the selected account
        // ------------------------------------------------------------
        private void LoadTransactionsForAccount(Guid accountId)
        {
            var posted = _txStore.GetPostedTransactions(accountId);
            var postedTransfers = _txStore.GetPostedTransferTransactions(accountId);
            var futureSingles = _txStore.GetFutureSingleTransactions(accountId);
            var futureTransfers = _txStore.GetFutureTransferTransactions(accountId);
            var recurringSingles = _txStore.GetRecurringSingleRules(accountId);
            var recurringTransfers = _txStore.GetRecurringTransferRules(accountId);

            var unified = UnifiedTransactionViewBuilder.Build(
                posted,
                postedTransfers,
                futureSingles,
                futureTransfers,
                recurringSingles,
                recurringTransfers);

            _transactionsSource.DataSource = unified;

            // Default sort by date
            SortByDateAscending();

            // Compute forecast balances
            ComputeForecastBalances(unified, accountId);
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

        // ------------------------------------------------------------
        // Forecast Balance Computation
        // ------------------------------------------------------------
        private void ComputeForecastBalances(List<UnifiedTransactionView> unified, Guid accountId)
        {
            var account = _accountStore.GetAccount(accountId);

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
            if (masterGrid.SelectedRows.Count == 0)
                return;

            var account = masterGrid.SelectedRows[0].DataBoundItem as Account;
            if (account == null)
                return;

            LoadTransactionsForAccount(account.Id);
        }
    }
}
