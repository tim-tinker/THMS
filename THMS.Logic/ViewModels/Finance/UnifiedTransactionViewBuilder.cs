using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.ViewModels.Finance
{
    public static class UnifiedTransactionViewBuilder
    {
        public static List<UnifiedTransactionView> Build(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction> postedTransfers,
            IEnumerable<FutureSingleTransaction>? userFutureSingles = null,
            IEnumerable<FutureTransferTransaction>? userFutureTransfers = null)
        {
            var list = new List<UnifiedTransactionView>();

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
                    CategoryId = tx.CategoryId,
                    Type = UnifiedTransactionView.PostedType,
                    ForecastBalance = null
                });
            }

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
                    CategoryId = tx.CategoryId,
                    Type = UnifiedTransactionView.PostedTransferType,
                    ForecastBalance = null
                });
            }

            foreach (var tx in userFutureSingles ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.AccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    CategoryId = tx.CategoryId,
                    Type = UnifiedTransactionView.FutureType,
                    ForecastBalance = null
                });
            }

            foreach (var tx in userFutureTransfers ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.FromAccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    CategoryId = tx.CategoryId,
                    Type = UnifiedTransactionView.FutureTransferType,
                    ForecastBalance = null
                });
            }

            return UnifiedTransactionView.OrderForRunningBalance(list).ToList();
        }

        public static List<UnifiedTransactionView> BuildRecurringRules(
            IEnumerable<RecurringSingleTransactionRule> singles,
            IEnumerable<RecurringTransferRule> transfers)
        {
            var list = new List<UnifiedTransactionView>();

            foreach (var rule in singles)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = rule.Id,
                    AccountId = rule.AccountId,
                    Date = rule.NextOccurrence,
                    Description = rule.Description ?? "",
                    Amount = rule.Amount,
                    Category = rule.Category,
                    CategoryId = rule.CategoryId,
                    Type = UnifiedTransactionView.RecurringRuleType,
                    ForecastBalance = null
                });
            }

            foreach (var rule in transfers)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = rule.Id,
                    AccountId = rule.FromAccountId,
                    Date = rule.NextOccurrence,
                    Description = rule.Description ?? "",
                    Amount = rule.Amount,
                    Category = rule.Category,
                    CategoryId = rule.CategoryId,
                    Type = UnifiedTransactionView.RecurringTransferRuleType,
                    ForecastBalance = null
                });
            }

            return UnifiedTransactionView.OrderForDisplay(list).ToList();
        }
    }
}
