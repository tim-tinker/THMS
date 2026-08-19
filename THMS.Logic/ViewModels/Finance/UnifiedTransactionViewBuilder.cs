using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.ViewModels.Finance
{
    public static class UnifiedTransactionViewBuilder
    {
        public static List<UnifiedTransactionView> Build(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction> postedTransfers,
            IEnumerable<FutureSingleTransaction> futureSingles,
            IEnumerable<FutureTransferTransaction> futureTransfers,
            IEnumerable<RecurringSingleTransactionRule> recurringSingles,
            IEnumerable<RecurringTransferRule> recurringTransfers)
        {
            var list = new List<UnifiedTransactionView>();

            // ------------------------------------------------------------
            // Posted (single-account)
            // ------------------------------------------------------------
            foreach (var tx in posted)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.AccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = "Posted",
                    ForecastBalance = null
                });
            }

            // ------------------------------------------------------------
            // Posted Transfers (ledger-level)
            // ------------------------------------------------------------
            foreach (var tx in postedTransfers)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.AccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = "PostedTransfer",
                    ForecastBalance = null
                });
            }

            // ------------------------------------------------------------
            // Future Singles
            // ------------------------------------------------------------
            foreach (var tx in futureSingles)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.AccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = tx.IsRealized ? "Future (Realized)" : "Future",
                    ForecastBalance = null
                });
            }

            // ------------------------------------------------------------
            // Future Transfers
            // ------------------------------------------------------------
            foreach (var tx in futureTransfers)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.FromAccountId, // UI shows per-account
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = tx.IsRealized ? "FutureTransfer (Realized)" : "FutureTransfer",
                    ForecastBalance = null
                });
            }

            // ------------------------------------------------------------
            // Recurring Rules (optional)
            // ------------------------------------------------------------
            foreach (var rule in recurringSingles)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = rule.Id,
                    AccountId = rule.AccountId,
                    Date = rule.NextOccurrence,
                    Description = rule.Description ?? "",
                    Amount = rule.Amount,
                    Category = rule.Category,
                    Type = "RecurringRule",
                    ForecastBalance = null
                });
            }

            foreach (var rule in recurringTransfers)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = rule.Id,
                    AccountId = rule.FromAccountId,
                    Date = rule.NextOccurrence,
                    Description = rule.Description ?? "",
                    Amount = rule.Amount,
                    Category = rule.Category,
                    Type = "RecurringTransferRule",
                    ForecastBalance = null
                });
            }

            // ------------------------------------------------------------
            // Sort by date ascending (initial view)
            // ------------------------------------------------------------
            return list.OrderBy(t => t.Date).ToList();
        }
    }
}
