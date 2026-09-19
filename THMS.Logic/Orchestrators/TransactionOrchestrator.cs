using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Transactions;
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
                RecurringTransfers = _store.GetRecurringTransferRules(accountId),
                IncomingTransferSplitPosted = IncomingPostedSplitSources(accountId, DateTime.MinValue, DateTime.MaxValue),
                IncomingTransferSplitFutures = IncomingFutureSplitSources(accountId)
            };
        }

        public AccountTransactions GetTransactionsForAccount(Guid accountId, DateTime start, DateTime end)
        {
            if (start <= DateTime.MinValue)
                return GetTransactionsForAccount(accountId);

            return new AccountTransactions
            {
                Posted = _store.GetPostedTransactions(accountId, start, end),
                PostedTransfers = _store.GetPostedTransferTransactions(accountId, start, end),
                FutureSingles = _store.GetFutureSingleTransactions(accountId)
                    .Where(f => f.IsUserCreated && InRange(f.Date, start, end)),
                FutureTransfers = _store.GetFutureTransferTransactions(accountId)
                    .Where(f => f.IsUserCreated && InRange(f.Date, start, end)),
                RecurringSingles = _store.GetRecurringSingleRules(accountId),
                RecurringTransfers = _store.GetRecurringTransferRules(accountId),
                IncomingTransferSplitPosted = IncomingPostedSplitSources(accountId, start, end),
                IncomingTransferSplitFutures = IncomingFutureSplitSources(accountId)
                    .Where(f => InRange(f.Date, start, end))
            };
        }

        public decimal SumPostedAmountsBefore(Guid accountId, DateTime before)
        {
            if (before <= DateTime.MinValue)
                return 0;

            return _store.SumPostedAmountsBefore(accountId, before)
                + _store.SumPostedTransferAmountsBefore(accountId, before)
                + SumIncomingTransferSplits(accountId, before);
        }

        public decimal SumPostedAmountsAfter(Guid accountId, DateTime after) =>
            _store.SumPostedAmountsAfter(accountId, after)
            + _store.SumPostedTransferAmountsAfter(accountId, after)
            + SumIncomingTransferSplitsAfter(accountId, after);

        public DateTime? GetLatestPostedActivityDate(Guid accountId)
        {
            var posted = _store.GetLatestPostedTransactionDate(accountId);
            var transfer = _store.GetLatestPostedTransferTransactionDate(accountId);
            if (posted is null)
                return transfer;
            if (transfer is null)
                return posted;
            return posted > transfer ? posted : transfer;
        }

        private static bool InRange(DateTime date, DateTime start, DateTime end) =>
            date >= start && date <= end;

        public List<UnifiedTransactionView> GenerateForecast(Guid accountId, DateTime from, DateTime to)
        {
            return _forecastGenerator.GenerateForecast(
                accountId,
                from,
                to,
                _store.GetAllRecurringSingleRules(),
                _store.GetAllRecurringTransferRules());
        }

        public decimal ComputePostedBalance(Guid accountId, decimal startingBalance)
        {
            return PostedBalanceCalculator.Compute(
                startingBalance,
                _store.GetPostedTransactions(accountId),
                _store.GetPostedTransferTransactions(accountId))
                + SumIncomingTransferSplits(accountId, before: null);
        }

        public BaseTransaction? GetParent(Guid transactionId) => FindParent(transactionId);

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

        public List<SplitTransactionRow> GetSplits(Guid transactionId) =>
            _store.GetSplits(transactionId);

        public void ApplySplits(Guid transactionId, List<SplitTransactionRow> splits)
        {
            ArgumentNullException.ThrowIfNull(splits);
            var parent = FindParent(transactionId)
                ?? throw new InvalidOperationException($"Transaction {transactionId} was not found.");

            var prepared = PrepareSplits(transactionId, splits);
            SplitTransactionValidator.Validate(parent.Amount, prepared);
            _store.SaveSplits(transactionId, prepared);
            parent.Splits = prepared;
            RefreshBudgets(parent);
        }

        public void ClearSplits(Guid transactionId)
        {
            var parent = FindParent(transactionId);
            _store.DeleteSplits(transactionId);
            if (parent is not null)
            {
                parent.Splits = [];
                RefreshBudgets(parent);
            }
        }

        private IEnumerable<PostedTransaction> IncomingPostedSplitSources(Guid accountId, DateTime start, DateTime end) =>
            _store.GetPostedTransactions(start, end)
                .Concat(_store.GetPostedTransferTransactions(start, end))
                .Where(t => t.AccountId != accountId && HasTransferTo(t, accountId));

        private IEnumerable<FutureSingleTransaction> IncomingFutureSplitSources(Guid accountId) =>
            _store.GetAllFutureSingleTransactions()
                .Where(t => t.AccountId != accountId && HasTransferTo(t, accountId));

        private decimal SumIncomingTransferSplits(Guid accountId, DateTime? before)
        {
            var posted = _store.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue)
                .Concat(_store.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue));
            decimal sum = 0;
            foreach (var transaction in posted)
            {
                if (transaction.AccountId == accountId)
                    continue;
                if (before is DateTime cutoff && transaction.Date >= cutoff)
                    continue;
                sum += IncomingTransferAmount(transaction, accountId);
            }

            return sum;
        }

        private decimal SumIncomingTransferSplitsAfter(Guid accountId, DateTime after)
        {
            var posted = _store.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue)
                .Concat(_store.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue));
            decimal sum = 0;
            foreach (var transaction in posted)
            {
                if (transaction.AccountId == accountId || transaction.Date <= after)
                    continue;
                sum += IncomingTransferAmount(transaction, accountId);
            }

            return sum;
        }

        private static bool HasTransferTo(BaseTransaction transaction, Guid accountId) =>
            transaction.Splits.Any(s => SplitTransactionMath.IsTransferTo(s, accountId));

        private static decimal IncomingTransferAmount(BaseTransaction transaction, Guid accountId) =>
            transaction.Splits
                .Where(s => SplitTransactionMath.IsTransferTo(s, accountId))
                .Sum(SplitTransactionMath.CounterpartAmount);

        private BaseTransaction? FindParent(Guid transactionId) =>
            (BaseTransaction?)_store.GetPostedTransaction(transactionId) ??
            _store.GetPostedTransferTransaction(transactionId) ??
            _store.GetFutureSingleTransaction(transactionId) ??
            _store.GetFutureTransferTransaction(transactionId) ??
            (BaseTransaction?)_store.GetRecurringSingleRule(transactionId) ??
            _store.GetRecurringTransferRule(transactionId);

        private static List<SplitTransactionRow> PrepareSplits(Guid parentId, IEnumerable<SplitTransactionRow> splits)
        {
            var prepared = new List<SplitTransactionRow>();
            foreach (var split in splits)
            {
                var copy = split.Clone();
                if (copy.Id == Guid.Empty)
                    copy.Id = Guid.NewGuid();
                copy.ParentTransactionId = parentId;
                if (copy.Type != SplitType.Transfer)
                    copy.TransferAccountId = null;
                prepared.Add(copy);
            }

            return prepared;
        }

        private void RefreshBudgets(BaseTransaction parent)
        {
            var budgets = new BudgetOrchestrator(_store);
            switch (parent)
            {
                case BaseSingleAccountTransaction single:
                    budgets.RefreshAccount(single.AccountId);
                    foreach (var dest in Destinations(single))
                        budgets.RefreshAccount(dest);
                    break;
                case TransferTransaction transfer:
                    budgets.RefreshAccount(transfer.FromAccountId);
                    budgets.RefreshAccount(transfer.ToAccountId);
                    break;
            }
        }

        private static IEnumerable<Guid> Destinations(BaseTransaction parent) =>
            parent.Splits
                .Where(s => s.Type == SplitType.Transfer && s.TransferAccountId is Guid id && id != Guid.Empty)
                .Select(s => s.TransferAccountId!.Value)
                .Distinct();
    }

    public class AccountTransactions
    {
        public IEnumerable<PostedTransaction> Posted { get; init; } = [];
        public IEnumerable<PostedTransferTransaction> PostedTransfers { get; init; } = [];
        public IEnumerable<FutureSingleTransaction> FutureSingles { get; init; } = [];
        public IEnumerable<FutureTransferTransaction> FutureTransfers { get; init; } = [];
        public IEnumerable<RecurringSingleTransactionRule> RecurringSingles { get; init; } = [];
        public IEnumerable<RecurringTransferRule> RecurringTransfers { get; init; } = [];
        public IEnumerable<PostedTransaction> IncomingTransferSplitPosted { get; init; } = [];
        public IEnumerable<FutureSingleTransaction> IncomingTransferSplitFutures { get; init; } = [];
    }
}
