namespace THMS.Domain.Finance.Transactions
{
    public static class SplitTransactionMath
    {
        public const decimal SumTolerance = 0.01m;

        public static bool AffectsBudget(SplitType type) =>
            type is SplitType.Expense or SplitType.Income or SplitType.Interest;

        public static bool RequiresCategory(SplitType type) =>
            type is not SplitType.Transfer;

        public static bool AmountsMatch(decimal parentAmount, IEnumerable<SplitTransactionRow> splits) =>
            Math.Abs(parentAmount - splits.Sum(s => s.Amount)) <= SumTolerance;

        public static bool IsTransferTo(SplitTransactionRow split, Guid accountId) =>
            split.Type == SplitType.Transfer && split.TransferAccountId == accountId;

        public static decimal CounterpartAmount(SplitTransactionRow split) => -split.Amount;

        public static decimal TransferAmountForAccount(
            Guid fromAccountId,
            Guid toAccountId,
            decimal amount,
            Guid accountId)
        {
            var magnitude = Math.Abs(amount);
            if (fromAccountId == toAccountId)
                return amount;
            if (accountId == fromAccountId)
                return -magnitude;
            if (accountId == toAccountId)
                return magnitude;
            return 0;
        }

        public static bool AffectsAccount(FutureSingleTransaction transaction, Guid accountId) =>
            transaction.AccountId == accountId
            || transaction.Splits.Any(split => IsTransferTo(split, accountId) && transaction.AccountId != accountId);

        public static bool AffectsAccount(FutureTransferTransaction transaction, Guid accountId) =>
            transaction.FromAccountId == accountId || transaction.ToAccountId == accountId;

        public static decimal AmountForAccount(FutureSingleTransaction transaction, Guid accountId)
        {
            if (transaction.AccountId == accountId)
                return transaction.Amount;
            return transaction.Splits
                .Where(split => IsTransferTo(split, accountId))
                .Sum(CounterpartAmount);
        }

        public static decimal AmountForAccount(FutureTransferTransaction transaction, Guid accountId) =>
            TransferAmountForAccount(
                transaction.FromAccountId, transaction.ToAccountId, transaction.Amount, accountId);

        public static Guid? OtherAccountId(FutureSingleTransaction transaction, Guid accountId)
        {
            if (transaction.AccountId == accountId)
                return null;
            return transaction.AccountId;
        }

        public static Guid? OtherAccountId(FutureTransferTransaction transaction, Guid accountId)
        {
            if (accountId == transaction.FromAccountId)
                return transaction.ToAccountId;
            if (accountId == transaction.ToAccountId)
                return transaction.FromAccountId;
            return null;
        }

        public static decimal Remaining(decimal parentAmount, IEnumerable<SplitTransactionRow> splits) =>
            parentAmount - splits.Sum(s => s.Amount);

        public static decimal ApplyBudgetAmount(decimal actual, decimal amount)
        {
            if (amount < 0)
                return actual + -amount;
            if (amount > 0)
                return actual - amount;
            return actual;
        }

        public static bool IsUncategorized(Guid? categoryId, string? category)
        {
            if (categoryId is not Guid id || id == Guid.Empty)
                return true;
            if (id == DefaultExpenseCategories.UncategorizedId)
                return true;
            return string.Equals(category, DefaultExpenseCategories.Uncategorized, StringComparison.OrdinalIgnoreCase);
        }

        public static (decimal PrincipalPaid, decimal InterestPaid, decimal RemainingPrincipal) SummarizeLoanPayments(
            decimal originalPrincipal,
            IEnumerable<SplitTransactionRow> splits)
        {
            var list = splits.ToList();
            var principalPaid = list.Where(s => s.Type == SplitType.Principal).Sum(s => Math.Abs(s.Amount));
            var interestPaid = list.Where(s => s.Type == SplitType.Interest).Sum(s => Math.Abs(s.Amount));
            return (principalPaid, interestPaid, originalPrincipal - principalPaid);
        }
    }
}
