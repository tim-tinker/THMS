using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.Logic.ViewModels.Finance
{
    public static class UnifiedTransactionViewBuilder
    {
        public static List<UnifiedTransactionView> Build(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction> postedTransfers,
            IEnumerable<FutureSingleTransaction>? userFutureSingles = null,
            IEnumerable<FutureTransferTransaction>? userFutureTransfers = null,
            Guid? forAccountId = null,
            IEnumerable<PostedTransaction>? incomingPostedSplitSources = null,
            IEnumerable<FutureSingleTransaction>? incomingFutureSplitSources = null)
        {
            var list = new List<UnifiedTransactionView>();

            foreach (var tx in posted)
                AddLedgerRows(list, tx, tx.AccountId, UnifiedTransactionView.PostedType,
                    UnifiedTransactionView.PostedTransferType, forAccountId);

            foreach (var tx in postedTransfers)
                AddLedgerRows(list, tx, tx.AccountId, UnifiedTransactionView.PostedTransferType,
                    UnifiedTransactionView.PostedTransferType, forAccountId);

            foreach (var tx in userFutureSingles ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                AddLedgerRows(list, tx, tx.AccountId, UnifiedTransactionView.FutureType,
                    UnifiedTransactionView.FutureTransferType, forAccountId);
            }

            foreach (var tx in userFutureTransfers ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                AddLedgerRows(list, tx, tx.FromAccountId, UnifiedTransactionView.FutureTransferType,
                    UnifiedTransactionView.FutureTransferType, forAccountId);
            }

            foreach (var source in incomingPostedSplitSources ?? [])
                AddTransferSplitRows(list, source, UnifiedTransactionView.PostedTransferType, forAccountId);

            foreach (var source in incomingFutureSplitSources ?? [])
            {
                if (!source.IsUserCreated || source.IsRealized)
                    continue;

                AddTransferSplitRows(list, source, UnifiedTransactionView.FutureTransferType, forAccountId);
            }

            return UnifiedTransactionView.OrderForRunningBalance(list).ToList();
        }

        public static List<UnifiedTransactionView> BuildRecurringRules(
            IEnumerable<RecurringSingleTransactionRule> singles,
            IEnumerable<RecurringTransferRule> transfers,
            Guid? forAccountId = null)
        {
            var list = new List<UnifiedTransactionView>();

            foreach (var rule in singles)
            {
                if (forAccountId is Guid accountId)
                {
                    if (rule.AccountId == accountId)
                        list.Add(ForLedgerRow(rule, rule.AccountId, UnifiedTransactionView.RecurringRuleType));
                    AddTransferSplitRows(list, rule, UnifiedTransactionView.RecurringTransferRuleType, accountId);
                }
                else
                {
                    list.Add(ForLedgerRow(rule, rule.AccountId, UnifiedTransactionView.RecurringRuleType));
                }
            }

            foreach (var rule in transfers)
                list.Add(ForLedgerRow(rule, rule.FromAccountId, UnifiedTransactionView.RecurringTransferRuleType));

            return UnifiedTransactionView.OrderForDisplay(list).ToList();
        }

        public static UnifiedTransactionView ForLedgerRow(
            BaseTransaction transaction,
            Guid accountId,
            string type,
            DateTime? date = null,
            decimal? amount = null,
            Guid? occurrenceId = null)
        {
            var occurrenceAmount = amount ?? transaction.Amount;
            var summarizeSplits = transaction.HasSplits && Math.Abs(occurrenceAmount) == Math.Abs(transaction.Amount);
            return new UnifiedTransactionView
            {
                Id = occurrenceId ?? transaction.Id,
                ParentTransactionId = transaction.Id,
                AccountId = accountId,
                Date = date ?? transaction.Date,
                Description = transaction.Description ?? "",
                Amount = occurrenceAmount,
                Category = summarizeSplits ? UnifiedTransactionView.SplitCategory : transaction.Category,
                CategoryId = summarizeSplits ? null : transaction.CategoryId,
                Type = type,
                ForecastBalance = null
            };
        }

        public static UnifiedTransactionView ForRuleOccurrence(
            BaseTransaction rule,
            Guid accountId,
            string type,
            DateTime? date = null,
            decimal? amount = null,
            Guid? occurrenceId = null) =>
            ForLedgerRow(rule, accountId, type, date, amount, occurrenceId);

        public static List<UnifiedTransactionView> BuildCategoryRows(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction>? postedTransfers = null)
        {
            var list = new List<UnifiedTransactionView>();
            foreach (var tx in posted)
                list.AddRange(ExpandForCategory(tx, tx.AccountId, UnifiedTransactionView.PostedType));
            foreach (var tx in postedTransfers ?? [])
                list.AddRange(ExpandForCategory(tx, tx.AccountId, UnifiedTransactionView.PostedTransferType));
            return UnifiedTransactionView.OrderForDisplay(list).ToList();
        }

        public static List<UnifiedTransactionView> FilterCategoryRows(
            IEnumerable<UnifiedTransactionView> rows,
            CategoryFilterChoice filter,
            IEnumerable<ExpenseCategory> categories)
        {
            if (filter.UncategorizedOnly)
            {
                return rows.Where(row =>
                    row.SplitKind != nameof(SplitType.Transfer) &&
                    SplitTransactionMath.IsUncategorized(row.CategoryId, row.Category)).ToList();
            }

            if (filter.CategoryId is Guid id)
            {
                var ids = ExpenseCategoryTree.ExpandWithDescendants([id], categories);
                return rows.Where(row => row.CategoryId is Guid categoryId && ids.Contains(categoryId)).ToList();
            }

            return rows.ToList();
        }

        public static UnifiedTransactionView ForTransferSplit(
            BaseTransaction parent,
            SplitTransactionRow split,
            Guid accountId,
            string type,
            DateTime? date = null,
            Guid? occurrenceId = null) =>
            new()
            {
                Id = occurrenceId ?? split.Id,
                ParentTransactionId = parent.Id,
                SplitRowId = split.Id,
                SplitKind = nameof(SplitType.Transfer),
                AccountId = accountId,
                Date = date ?? parent.Date,
                Description = parent.Description ?? "",
                Amount = SplitTransactionMath.CounterpartAmount(split),
                Category = string.IsNullOrWhiteSpace(split.Category) ? "Transfer" : split.Category,
                CategoryId = split.CategoryId,
                Type = type,
                ForecastBalance = null
            };

        private static IEnumerable<UnifiedTransactionView> ExpandForCategory(
            BaseTransaction transaction,
            Guid accountId,
            string type)
        {
            if (!transaction.HasSplits)
            {
                yield return CategoryRow(
                    transaction, accountId, type, transaction.Id, transaction.Amount,
                    transaction.CategoryId, transaction.Category, splitRowId: null, splitKind: null);
                yield break;
            }

            foreach (var split in transaction.Splits.OrderBy(s => s.Id))
            {
                var category = split.Category;
                if (split.Type == SplitType.Transfer && string.IsNullOrWhiteSpace(category))
                    category = "Transfer";

                yield return CategoryRow(
                    transaction,
                    accountId,
                    type,
                    split.Id,
                    split.Amount,
                    split.CategoryId,
                    category,
                    split.Id,
                    split.Type.ToString());
            }
        }

        private static UnifiedTransactionView CategoryRow(
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

        private static void AddLedgerRows(
            List<UnifiedTransactionView> list,
            BaseTransaction transaction,
            Guid parentAccountId,
            string parentType,
            string transferType,
            Guid? forAccountId)
        {
            if (forAccountId is Guid accountId)
            {
                if (parentAccountId == accountId)
                    list.Add(ForLedgerRow(transaction, parentAccountId, parentType));
                AddTransferSplitRows(list, transaction, transferType, accountId);
                return;
            }

            list.Add(ForLedgerRow(transaction, parentAccountId, parentType));
            AddTransferSplitRows(list, transaction, transferType, forAccountId: null);
        }

        private static void AddTransferSplitRows(
            List<UnifiedTransactionView> list,
            BaseTransaction parent,
            string type,
            Guid? forAccountId,
            DateTime? date = null)
        {
            if (!parent.HasSplits)
                return;

            Guid? parentAccountId = parent is BaseSingleAccountTransaction single ? single.AccountId : null;
            foreach (var split in parent.Splits)
            {
                if (split.Type != SplitType.Transfer || split.TransferAccountId is not Guid dest || dest == Guid.Empty)
                    continue;
                if (parentAccountId == dest)
                    continue;
                if (forAccountId is Guid accountId && dest != accountId)
                    continue;

                list.Add(ForTransferSplit(parent, split, dest, type, date));
            }
        }
    }
}
