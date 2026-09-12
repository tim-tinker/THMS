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
                list.AddRange(Expand(tx, tx.AccountId, UnifiedTransactionView.PostedType));

            foreach (var tx in postedTransfers)
                list.AddRange(Expand(tx, tx.AccountId, UnifiedTransactionView.PostedTransferType));

            foreach (var tx in userFutureSingles ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                list.AddRange(Expand(tx, tx.AccountId, UnifiedTransactionView.FutureType));
            }

            foreach (var tx in userFutureTransfers ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                list.AddRange(Expand(tx, tx.FromAccountId, UnifiedTransactionView.FutureTransferType));
            }

            return UnifiedTransactionView.OrderForRunningBalance(list).ToList();
        }

        public static List<UnifiedTransactionView> BuildRecurringRules(
            IEnumerable<RecurringSingleTransactionRule> singles,
            IEnumerable<RecurringTransferRule> transfers)
        {
            var list = new List<UnifiedTransactionView>();

            foreach (var rule in singles)
                list.AddRange(Expand(rule, rule.AccountId, UnifiedTransactionView.RecurringRuleType));

            foreach (var rule in transfers)
                list.AddRange(Expand(rule, rule.FromAccountId, UnifiedTransactionView.RecurringTransferRuleType));

            return UnifiedTransactionView.OrderForDisplay(list).ToList();
        }

        public static IEnumerable<UnifiedTransactionView> Expand(
            BaseTransaction transaction,
            Guid accountId,
            string type)
        {
            if (!transaction.HasSplits)
            {
                yield return Create(transaction, accountId, type, transaction.Id, transaction.Amount,
                    transaction.CategoryId, transaction.Category, splitRowId: null, splitKind: null);
                yield break;
            }

            foreach (var split in transaction.Splits.OrderBy(s => s.Id))
            {
                yield return Create(
                    transaction,
                    accountId,
                    type,
                    split.Id,
                    split.Amount,
                    split.CategoryId,
                    split.Category,
                    split.Id,
                    split.Type.ToString());
            }
        }

        private static UnifiedTransactionView Create(
            BaseTransaction transaction,
            Guid accountId,
            string type,
            Guid id,
            decimal amount,
            Guid? categoryId,
            string? category,
            Guid? splitRowId,
            string? splitKind) =>
            new()
            {
                Id = id,
                ParentTransactionId = transaction.Id,
                AccountId = accountId,
                Date = transaction.Date,
                Description = transaction.Description ?? "",
                Amount = amount,
                Category = category,
                CategoryId = categoryId,
                Type = type,
                SplitRowId = splitRowId,
                SplitKind = splitKind,
                ForecastBalance = null
            };
    }
}
