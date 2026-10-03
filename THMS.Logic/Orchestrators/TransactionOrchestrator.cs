using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Orchestrators
{
    public class TransactionOrchestrator
    {
        private readonly ITransactionDataStore _store;
        private readonly ForecastGenerator _forecastGenerator = new();

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
                FutureSingles = _store.GetFutureSingleTransactions(accountId).Where(f => !f.IsRealized),
                FutureTransfers = _store.GetFutureTransferTransactions(accountId).Where(f => !f.IsRealized),
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
                    .Where(f => !f.IsRealized && InRange(f.Date, start, end)),
                FutureTransfers = _store.GetFutureTransferTransactions(accountId)
                    .Where(f => !f.IsRealized && InRange(f.Date, start, end)),
                RecurringSingles = _store.GetRecurringSingleRules(accountId),
                RecurringTransfers = _store.GetRecurringTransferRules(accountId),
                IncomingTransferSplitPosted = IncomingPostedSplitSources(accountId, start, end),
                IncomingTransferSplitFutures = IncomingFutureSplitSources(accountId)
                    .Where(f => InRange(f.Date, start, end))
            };
        }

        public (List<PostedTransaction> Posted, List<PostedTransferTransaction> Transfers) GetPostedActivity(
            DateTime start,
            DateTime end)
        {
            if (start <= DateTime.MinValue)
            {
                return (
                    _store.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).ToList(),
                    _store.GetPostedTransferTransactions(DateTime.MinValue, DateTime.MaxValue).ToList());
            }

            return (
                _store.GetPostedTransactions(start, end).ToList(),
                _store.GetPostedTransferTransactions(start, end).ToList());
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
            var forecast = _forecastGenerator.GenerateForecast(
                accountId,
                from,
                to,
                _store.GetAllRecurringSingleRules(),
                _store.GetAllRecurringTransferRules());
            var scheduled = UnmatchedExpected();
            if (scheduled.Count == 0)
                return forecast;

            return forecast.Where(row => !CoveredByExpected(row, scheduled)).ToList();
        }

        private List<BaseTransaction> UnmatchedExpected()
        {
            var list = new List<BaseTransaction>();
            list.AddRange(_store.GetAllFutureSingleTransactions().Where(e => !e.IsRealized));
            list.AddRange(_store.GetAllFutureTransferTransactions().Where(e => !e.IsRealized));
            return list;
        }

        private static bool CoveredByExpected(
            UnifiedTransactionView row,
            IReadOnlyList<BaseTransaction> expected) =>
            expected.Any(item =>
            {
                var accountsMatch = item switch
                {
                    FutureSingleTransaction s => s.AccountId == row.AccountId,
                    FutureTransferTransaction t => t.FromAccountId == row.AccountId || t.ToAccountId == row.AccountId,
                    _ => false
                };
                return accountsMatch
                    && Math.Abs(Math.Abs(item.Amount) - Math.Abs(row.Amount)) <= RecurringRulePattern.AmountTolerance
                    && Math.Abs((item.Date.Date - row.Date.Date).TotalDays) <= BillsOrchestrator.MatchDayTolerance;
            });

        public decimal ComputePostedBalance(Guid accountId, decimal startingBalance)
        {
            return PostedBalanceCalculator.Compute(
                startingBalance,
                _store.GetPostedTransactions(accountId),
                _store.GetPostedTransferTransactions(accountId))
                + SumIncomingTransferSplits(accountId, before: null);
        }

        public BaseTransaction? GetParent(Guid transactionId) => FindParent(transactionId);

        public Guid? FindTransferCounterpartAccount(
            BaseTransaction parent,
            Guid thisAccountId,
            IReadOnlyList<Account> accounts)
        {
            if (parent is PostedTransferTransaction linked
                && linked.RelatedPostedTransactionId != Guid.Empty
                && FindParent(linked.RelatedPostedTransactionId) is BaseSingleAccountTransaction related
                && related.AccountId != thisAccountId)
            {
                return related.AccountId;
            }

            if (parent is FutureTransferTransaction future)
            {
                if (thisAccountId == future.FromAccountId && future.ToAccountId != Guid.Empty)
                    return future.ToAccountId;
                if (thisAccountId == future.ToAccountId && future.FromAccountId != Guid.Empty)
                    return future.FromAccountId;
            }

            if (parent is RecurringTransferRule rule)
            {
                if (thisAccountId == rule.FromAccountId && rule.ToAccountId != Guid.Empty)
                    return rule.ToAccountId;
                if (thisAccountId == rule.ToAccountId && rule.FromAccountId != Guid.Empty)
                    return rule.FromAccountId;
            }

            var start = parent.Date.Date.AddDays(-SplitTransactionSeed.MatchDayWindow);
            var end = parent.Date.Date.AddDays(SplitTransactionSeed.MatchDayWindow);
            var activity = GetPostedActivity(start, end);
            return SplitTransactionSeed.MatchCounterpartAccount(
                parent,
                thisAccountId,
                accounts,
                activity.Posted.Concat<PostedTransaction>(activity.Transfers));
        }

        public void ReconcileRules(Guid accountId) =>
            new ReconciliationOrchestrator(_store).RecommendMatches();

        public List<SplitTransactionRow> GetSplits(Guid transactionId) =>
            _store.GetSplits(transactionId);

        public bool TryApplyAsWholeTransfer(Guid transactionId, IReadOnlyList<SplitTransactionRow> splits)
        {
            if (splits.Count != 1)
                return false;

            var row = splits[0];
            if (row.Type != SplitType.Transfer)
                return false;
            if (row.FromAccountId is not Guid fromId || fromId == Guid.Empty
                || row.ToAccountId is not Guid toId || toId == Guid.Empty
                || fromId == toId)
                return false;

            if (FindParent(transactionId) is not PostedTransaction posted || row.Amount != posted.Amount)
                return false;
            if (SplitTransactionMath.IsUncategorized(row.CategoryId, row.Category))
                throw new InvalidOperationException("Transfer splits require a category.");

            if (posted is PostedTransferTransaction)
            {
                ClearSplits(transactionId);
                UpdateTransferPair(transactionId, fromId, toId, row.CategoryId, row.Category);
                LinkUnlinkedCounterpart(transactionId, fromId, toId);
                return true;
            }

            var transfer = CreateTransfer(posted, fromId, toId, row.CategoryId, row.Category, Guid.Empty);
            _store.DeletePostedTransaction(posted.Id);
            _store.AddPostedTransferTransaction(transfer);
            LinkUnlinkedCounterpart(transfer.Id, fromId, toId);
            RefreshBudgets(transfer);
            return true;
        }

        public void UpdateTransferPair(Guid transactionId, Guid fromAccountId, Guid toAccountId, Guid? categoryId, string? category)
        {
            if (FindParent(transactionId) is not PostedTransferTransaction side)
                throw new InvalidOperationException($"Transfer {transactionId} was not found.");

            side.FromAccountId = fromAccountId;
            side.ToAccountId = toAccountId;
            side.CategoryId = categoryId;
            side.Category = category;
            _store.UpdatePostedTransferTransaction(side);

            if (side.RelatedPostedTransactionId != Guid.Empty
                && FindParent(side.RelatedPostedTransactionId) is PostedTransferTransaction other)
            {
                other.FromAccountId = fromAccountId;
                other.ToAccountId = toAccountId;
                other.RelatedPostedTransactionId = side.Id;
                _store.UpdatePostedTransferTransaction(other);
            }
        }

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
                .Where(t => !t.IsRealized && t.AccountId != accountId && HasTransferTo(t, accountId));

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

        private void LinkUnlinkedCounterpart(Guid transactionId, Guid fromAccountId, Guid toAccountId)
        {
            if (FindParent(transactionId) is not PostedTransferTransaction side
                || side.RelatedPostedTransactionId != Guid.Empty)
                return;

            var otherAccountId = side.AccountId == fromAccountId ? toAccountId
                : side.AccountId == toAccountId ? fromAccountId
                : Guid.Empty;
            if (otherAccountId == Guid.Empty)
                return;

            var start = side.Date.Date.AddDays(-SplitTransactionSeed.MatchDayWindow);
            var end = side.Date.Date.AddDays(SplitTransactionSeed.MatchDayWindow);
            var candidates = _store.GetPostedTransactions(otherAccountId, start, end)
                .Where(t => t.Amount == -side.Amount && CanAbsorbAsTransfer(t))
                .Concat<PostedTransaction>(_store.GetPostedTransferTransactions(otherAccountId, start, end)
                    .Where(t => t.Id != side.Id && t.Amount == -side.Amount && CanAbsorbAsTransfer(t)))
                .ToList();
            if (candidates.Count != 1)
                return;

            var match = candidates[0];
            PostedTransferTransaction other;
            if (match is PostedTransferTransaction existing)
            {
                existing.FromAccountId = fromAccountId;
                existing.ToAccountId = toAccountId;
                existing.RelatedPostedTransactionId = side.Id;
                existing.Direction = existing.AccountId == toAccountId
                    ? TransferDirection.Incoming
                    : TransferDirection.Outgoing;
                _store.DeleteSplits(existing.Id);
                existing.Splits = [];
                _store.UpdatePostedTransferTransaction(existing);
                other = existing;
            }
            else
            {
                other = CreateTransfer(match, fromAccountId, toAccountId, match.CategoryId, match.Category, side.Id);
                _store.DeletePostedTransaction(match.Id);
                _store.AddPostedTransferTransaction(other);
            }

            side.RelatedPostedTransactionId = other.Id;
            _store.UpdatePostedTransferTransaction(side);
        }

        private static bool CanAbsorbAsTransfer(PostedTransaction transaction) =>
            transaction.Splits.Count == 0
            || (transaction.Splits.Count == 1
                && transaction.Splits[0].Type == SplitType.Transfer
                && transaction.Splits[0].Amount == transaction.Amount);

        private static PostedTransferTransaction CreateTransfer(
            PostedTransaction source,
            Guid fromAccountId,
            Guid toAccountId,
            Guid? categoryId,
            string? category,
            Guid relatedId)
        {
            return new PostedTransferTransaction(
                source,
                relatedId,
                source.AccountId == toAccountId ? TransferDirection.Incoming : TransferDirection.Outgoing)
            {
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                CategoryId = categoryId,
                Category = category
            };
        }

        private static List<SplitTransactionRow> PrepareSplits(Guid parentId, IEnumerable<SplitTransactionRow> splits)
        {
            var prepared = new List<SplitTransactionRow>();
            foreach (var split in splits)
            {
                var copy = split.Clone();
                if (copy.Id == Guid.Empty)
                    copy.Id = Guid.NewGuid();
                copy.ParentTransactionId = parentId;
                if (copy.Type == SplitType.Transfer)
                {
                    if (copy.ToAccountId is null && copy.TransferAccountId is Guid destination && destination != Guid.Empty)
                        copy.ToAccountId = destination;
                    copy.TransferAccountId = copy.ToAccountId;
                }
                else
                {
                    copy.TransferAccountId = null;
                    copy.FromAccountId = null;
                    copy.ToAccountId = null;
                }
                prepared.Add(copy);
            }

            return prepared;
        }

        private void RefreshBudgets(BaseTransaction parent) =>
            new BudgetOrchestrator(_store).RefreshAllActive();
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
