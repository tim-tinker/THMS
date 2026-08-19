using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class TransactionOrchestrator
    {
        private readonly ITransactionDataStore _store;

        public TransactionOrchestrator(ITransactionDataStore store)
        {
            _store = store;
        }

        // ------------------------------------------------------------
        // 1. Import posted transactions
        // ------------------------------------------------------------
        public void ImportPostedTransactions(Guid accountId, IEnumerable<PostedTransaction> imported)
        {
            foreach (var tx in imported)
            {
                tx.AccountId = accountId;
                _store.AddPostedTransaction(tx);
            }

            // After import, try to detect transfers for this account
            DetectTransfers(accountId);
        }

        // ------------------------------------------------------------
        // 2. Detect transfers (ledger-level)
        // ------------------------------------------------------------
        public void DetectTransfers(Guid accountId)
        {
            var unmatched = _store.GetUnmatchedPostedTransactions(accountId).ToList();

            // naive example: match by abs(amount), date, and description pattern
            foreach (var tx in unmatched)
            {
                var candidates = _store.GetUnmatchedPostedTransactions(GetOtherAccounts(accountId).FirstOrDefault())
                                       .Where(c => IsPotentialTransferMatch(tx, c))
                                       .ToList();

                var match = candidates.FirstOrDefault();
                if (match == null) continue;

                var postedFrom = CreatePostedTransfer(tx, TransferDirection.Outgoing, match.Id);
                var postedTo = CreatePostedTransfer(match, TransferDirection.Incoming, tx.Id);

                _store.AddPostedTransferTransaction(postedFrom);
                _store.AddPostedTransferTransaction(postedTo);

                // Optionally delete the original PostedTransaction entries
                _store.DeletePostedTransaction(tx.Id);
                _store.DeletePostedTransaction(match.Id);
            }
        }

        private bool IsPotentialTransferMatch(PostedTransaction a, PostedTransaction b)
        {
            return Math.Abs(a.Amount) == Math.Abs(b.Amount)
                   && a.Date.Date == b.Date.Date;
        }

        private IEnumerable<Guid> GetOtherAccounts(Guid accountId)
        {
            // placeholder: you’ll wire this to your account system
            return Enumerable.Empty<Guid>();
        }

        private PostedTransferTransaction CreatePostedTransfer(
            PostedTransaction source,
            TransferDirection direction,
            Guid relatedId)
        {
            return new PostedTransferTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = source.AccountId,
                Date = source.Date,
                Amount = source.Amount,
                Category = source.Category,
                Description = source.Description,
                Direction = direction,
                RelatedPostedTransactionId = relatedId
            };
        }

        // ------------------------------------------------------------
        // 3. Generate future transactions from recurring rules
        // ------------------------------------------------------------
        public void GenerateFutureTransactions(Guid accountId, DateTime forecastEnd)
        {
            var singleRules = _store.GetRecurringSingleRules(accountId);
            var transferRules = _store.GetRecurringTransferRules(accountId);

            foreach (var rule in singleRules.Where(r => r.IsActive))
                GenerateFutureSinglesFromRule(rule, forecastEnd);

            foreach (var rule in transferRules.Where(r => r.IsActive))
                GenerateFutureTransfersFromRule(rule, forecastEnd);
        }

        private void GenerateFutureSinglesFromRule(RecurringSingleTransactionRule rule, DateTime forecastEnd)
        {
            var next = rule.NextOccurrence;

            while (next <= forecastEnd && (rule.EndDate == null || next <= rule.EndDate.Value))
            {
                var amount = rule.IsFinalPaymentDifferent && rule.EndDate.HasValue && next == rule.EndDate.Value
                    ? rule.FinalPaymentAmount ?? rule.Amount
                    : rule.Amount;

                var future = new FutureSingleTransaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = rule.AccountId,
                    Date = next,
                    Amount = amount,
                    Category = rule.Category,
                    Description = rule.Description,
                    IsRealized = false
                };

                _store.AddFutureSingleTransaction(future);

                next = next.AddFrequency(rule.Frequency);
            }

            rule.NextOccurrence = next;
            _store.UpdateRecurringSingleRule(rule);
        }

        private void GenerateFutureTransfersFromRule(RecurringTransferRule rule, DateTime forecastEnd)
        {
            var next = rule.NextOccurrence;

            while (next <= forecastEnd && (rule.EndDate == null || next <= rule.EndDate.Value))
            {
                var amount = rule.IsFinalPaymentDifferent && rule.EndDate.HasValue && next == rule.EndDate.Value
                    ? rule.FinalPaymentAmount ?? rule.Amount
                    : rule.Amount;

                var future = new FutureTransferTransaction
                {
                    Id = Guid.NewGuid(),
                    FromAccountId = rule.FromAccountId,
                    ToAccountId = rule.ToAccountId,
                    Date = next,
                    Amount = amount,
                    Category = rule.Category,
                    Description = rule.Description,
                    IsRealized = false
                };

                _store.AddFutureTransferTransaction(future);

                next = next.AddFrequency(rule.Frequency);
            }

            rule.NextOccurrence = next;
            _store.UpdateRecurringTransferRule(rule);
        }

        // ------------------------------------------------------------
        // 4. Reconcile future singles
        // ------------------------------------------------------------
        public void ReconcileFutureSingles(Guid accountId)
        {
            var posted = _store.GetPostedTransactions(accountId).ToList();
            var futures = _store.GetFutureSingleTransactions(accountId).Where(f => !f.IsRealized).ToList();

            foreach (var future in futures)
            {
                var match = posted.FirstOrDefault(p =>
                    Math.Abs(p.Amount) == Math.Abs(future.Amount) &&
                    p.Date.Date == future.Date.Date);

                if (match == null) continue;

                future.IsRealized = true;
                future.PostedTransactionId = match.Id;
                _store.UpdateFutureSingleTransaction(future);
            }
        }

        // ------------------------------------------------------------
        // 5. Reconcile future transfers
        // ------------------------------------------------------------
        public void ReconcileFutureTransfers(Guid accountId)
        {
            var postedTransfers = _store.GetPostedTransferTransactions(accountId).ToList();
            var futures = _store.GetFutureTransferTransactions(accountId).Where(f => !f.IsRealized).ToList();

            foreach (var future in futures)
            {
                var fromPosted = postedTransfers.FirstOrDefault(p =>
                    p.AccountId == future.FromAccountId &&
                    Math.Abs(p.Amount) == Math.Abs(future.Amount) &&
                    p.Date.Date == future.Date.Date);

                var toPosted = postedTransfers.FirstOrDefault(p =>
                    p.AccountId == future.ToAccountId &&
                    Math.Abs(p.Amount) == Math.Abs(future.Amount) &&
                    p.Date.Date == future.Date.Date);

                if (fromPosted == null || toPosted == null) continue;

                future.IsRealized = true;
                future.PostedFromTransactionId = fromPosted.Id;
                future.PostedToTransactionId = toPosted.Id;
                _store.UpdateFutureTransferTransaction(future);
            }
        }

        // ------------------------------------------------------------
        // 6. Roll-off realized future transactions
        // ------------------------------------------------------------
        public void RollOffRealizedFutureTransactions(DateTime cutoff)
        {
            var realizedSingles = _store.GetRealizedFutureSingleTransactions(cutoff);
            var realizedTransfers = _store.GetRealizedFutureTransferTransactions(cutoff);

            foreach (var f in realizedSingles)
                _store.DeleteFutureSingleTransaction(f.Id);

            foreach (var f in realizedTransfers)
                _store.DeleteFutureTransferTransaction(f.Id);
        }
    }
}
