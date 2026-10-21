using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.Logic.Finance.Budget
{
    public class ExpenseBudgetDetector
    {
        public decimal ComputeActualExpenses(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<Guid> includedCategoryIds,
            DateTime periodStart,
            DateTime periodEnd,
            IEnumerable<ExpenseCategory>? categories = null)
        {
            var included = Expand(includedCategoryIds, categories);
            decimal actual = 0;

            foreach (var transaction in posted.Where(t =>
                         t.Date.Date >= periodStart.Date &&
                         t.Date.Date <= periodEnd.Date))
            {
                if (transaction.HasSplits)
                {
                    foreach (var split in transaction.Splits)
                    {
                        if (!SplitTransactionMath.AffectsBudget(split.Type))
                            continue;
                        if (!Matches(split.CategoryId, split.Category, included, categories))
                            continue;
                        actual = SplitTransactionMath.ApplyBudgetAmount(actual, split.Amount);
                    }

                    continue;
                }

                if (!Matches(transaction.CategoryId, transaction.Category, included, categories))
                    continue;

                actual = SplitTransactionMath.ApplyBudgetAmount(actual, transaction.Amount);
            }

            return Math.Max(actual, 0);
        }

        public bool IncludesTransaction(
            PostedTransaction transaction,
            IEnumerable<Guid> includedCategoryIds,
            IEnumerable<ExpenseCategory>? categories = null)
        {
            var included = Expand(includedCategoryIds, categories);
            if (transaction.HasSplits)
            {
                foreach (var split in transaction.Splits)
                {
                    if (!SplitTransactionMath.AffectsBudget(split.Type))
                        continue;
                    if (Matches(split.CategoryId, split.Category, included, categories))
                        return true;
                }

                return false;
            }

            return Matches(transaction.CategoryId, transaction.Category, included, categories);
        }

        public decimal ComputeRecommendedAmount(
            IEnumerable<ExpenseBudgetHistory> history,
            BudgetFrequency frequency,
            IEnumerable<PostedTransaction>? posted = null,
            IEnumerable<Guid>? includedCategoryIds = null,
            IEnumerable<ExpenseCategory>? categories = null,
            DateTime? cycleStart = null,
            DateTime? asOf = null)
        {
            var samples = history
                .Select(h => (Start: h.PeriodStart.Date, End: h.PeriodEnd.Date, Actual: h.ActualExpenses))
                .ToList();

            if (posted is not null && includedCategoryIds is not null)
            {
                var covered = samples
                    .Select(s => (s.Start, s.End))
                    .ToHashSet();
                var dates = posted.Select(t => t.Date.Date).ToList();
                if (dates.Count > 0)
                {
                    foreach (var (start, end) in BudgetPeriodCalculator.PeriodsOverlapping(
                                 dates.Min(),
                                 dates.Max(),
                                 frequency,
                                 cycleStart))
                    {
                        if (covered.Contains((start, end)))
                            continue;

                        samples.Add((
                            start,
                            end,
                            ComputeActualExpenses(posted, includedCategoryIds, start, end, categories)));
                    }
                }
            }

            var persistedStarts = history.Select(h => h.PeriodStart.Date).ToHashSet();
            samples = samples
                .Where(s => s.Actual > 0 || persistedStarts.Contains(s.Start))
                .ToList();

            return BudgetRecommendation.Compute(samples, frequency, cycleStart, asOf);
        }

        private static HashSet<Guid> Expand(
            IEnumerable<Guid> includedCategoryIds,
            IEnumerable<ExpenseCategory>? categories)
        {
            var selected = includedCategoryIds.Where(id => id != Guid.Empty).ToHashSet();
            if (categories is null)
                return selected;

            return ExpenseCategoryTree.ExpandWithDescendants(selected, categories);
        }

        private static bool Matches(
            Guid? categoryId,
            string? categoryName,
            HashSet<Guid> included,
            IEnumerable<ExpenseCategory>? categories)
        {
            if (categoryId is Guid id && included.Contains(id))
                return true;

            if (string.IsNullOrWhiteSpace(categoryName) || categories is null)
                return false;

            var canonical = DefaultExpenseCategories.CanonicalName(categoryName);
            var named = categories.FirstOrDefault(c =>
                string.Equals(c.Name, canonical, StringComparison.OrdinalIgnoreCase));
            return named is not null && included.Contains(named.Id);
        }
    }
}
