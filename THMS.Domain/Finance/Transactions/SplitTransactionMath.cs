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
