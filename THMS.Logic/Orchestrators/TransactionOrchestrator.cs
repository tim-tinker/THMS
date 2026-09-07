using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class TransactionOrchestrator
    {
        private readonly ITransactionDataStore _store;
        private readonly ForecastGenerator _forecastGenerator = new();
        private readonly FutureReconciler _reconciler = new();

        public TransactionOrchestrator()
            : this(new DataStoreFactory().GetTransactionStore())
        {
        }

        public TransactionOrchestrator(ITransactionDataStore store)
        {
            _store = store;
        }

        public AccountTransactions GetTransactionsForAccount(Guid accountId)
        {
            return new AccountTransactions
            {
                Posted = _store.GetPostedTransactions(accountId),
                PostedTransfers = _store.GetPostedTransferTransactions(accountId),
                FutureSingles = _store.GetFutureSingleTransactions(accountId).Where(f => f.IsUserCreated),
                FutureTransfers = _store.GetFutureTransferTransactions(accountId).Where(f => f.IsUserCreated),
                RecurringSingles = _store.GetRecurringSingleRules(accountId),
                RecurringTransfers = _store.GetRecurringTransferRules(accountId)
            };
        }

        public List<UnifiedTransactionView> GenerateForecast(Guid accountId, DateTime from, DateTime to)
        {
            return _forecastGenerator.GenerateForecast(
                accountId,
                from,
                to,
                _store.GetRecurringSingleRules(accountId),
                _store.GetRecurringTransferRules(accountId),
                _store.GetExpenseBudgetRules(accountId));
        }

        public decimal ComputePostedBalance(Guid accountId, decimal startingBalance)
        {
            return PostedBalanceCalculator.Compute(
                startingBalance,
                _store.GetPostedTransactions(accountId),
                _store.GetPostedTransferTransactions(accountId));
        }

        public void ReconcileRules(Guid accountId)
        {
            var posted = _store.GetPostedTransactions(accountId).ToList();
            var postedTransfers = _store.GetPostedTransferTransactions(accountId).ToList();
            var singleRules = _store.GetRecurringSingleRules(accountId).ToList();
            var transferRules = _store.GetRecurringTransferRules(accountId).ToList();

            _reconciler.ReconcileSingles(posted, singleRules);
            _reconciler.ReconcileTransfers(postedTransfers, transferRules);

            foreach (var rule in _reconciler.MatchedSingleRules)
                _store.UpdateRecurringSingleRule(rule);

            foreach (var rule in _reconciler.MatchedTransferRules)
                _store.UpdateRecurringTransferRule(rule);
        }
    }

    public class AccountTransactions
    {
        public IEnumerable<PostedTransaction> Posted { get; init; } = [];
        public IEnumerable<PostedTransferTransaction> PostedTransfers { get; init; } = [];
        public IEnumerable<FutureSingleTransaction> FutureSingles { get; init; } = [];
        public IEnumerable<FutureTransferTransaction> FutureTransfers { get; init; } = [];
        public IEnumerable<RecurringSingleTransactionRule> RecurringSingles { get; init; } = [];
        public IEnumerable<RecurringTransferRule> RecurringTransfers { get; init; } = [];
    }
}
