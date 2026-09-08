using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;
using THMS.Logic.Finance.Model;

namespace THMS.Logic.Finance.Budget
{
    public class ExpenseBudgetDetector
    {
        public const ExpenseSmoothingMode SmoothingMode = ExpenseSmoothingMode.Hybrid;

        private readonly ExpenseSmoothingModel _smoothing = new();

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
                         Matches(t.CategoryId, t.Category, included, categories) &&
                         t.Date.Date >= periodStart.Date &&
                         t.Date.Date <= periodEnd.Date))
            {
                if (transaction.Amount < 0)
                    actual += -transaction.Amount;
                else if (transaction.Amount > 0)
                    actual -= transaction.Amount;
            }

            return Math.Max(actual, 0);
        }

        public decimal ComputeRecommendedAmount(
            IEnumerable<ExpenseBudgetHistory> history,
            BudgetFrequency frequency,
            IEnumerable<PostedTransaction>? posted = null,
            IEnumerable<Guid>? includedCategoryIds = null,
            IEnumerable<ExpenseCategory>? categories = null)
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
                                 frequency))
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
                .OrderBy(s => s.Start)
                .ToList();

            if (samples.Count == 0)
                return 0;

            var totals = ToSmoothingSeries(samples, frequency);
            return Math.Max(0, _smoothing.ComputeSmoothedAverage(totals, SmoothingMode, PeriodsToAverage(frequency)));
        }

        private static List<MonthlyExpenseTotal> ToSmoothingSeries(
            List<(DateTime Start, DateTime End, decimal Actual)> samples,
            BudgetFrequency frequency)
        {
            if (frequency is BudgetFrequency.Weekly or BudgetFrequency.Biweekly)
            {
                return samples
                    .Select((sample, index) => new MonthlyExpenseTotal
                    {
                        Year = sample.Start.Year,
                        Month = (index % 12) + 1,
                        Amount = sample.Actual
                    })
                    .ToList();
            }

            return samples
                .GroupBy(s => new { s.Start.Year, Month = BucketMonth(s.Start, frequency) })
                .Select(g => new MonthlyExpenseTotal
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(x => x.Actual)
                })
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .ToList();
        }

        private static int BucketMonth(DateTime start, BudgetFrequency frequency) => frequency switch
        {
            BudgetFrequency.Quarterly => ((start.Month - 1) / 3) * 3 + 1,
            BudgetFrequency.Annual => 1,
            _ => start.Month
        };

        private static int PeriodsToAverage(BudgetFrequency frequency) => frequency switch
        {
            BudgetFrequency.Weekly => 52,
            BudgetFrequency.Biweekly => 26,
            BudgetFrequency.Quarterly => 12,
            BudgetFrequency.Annual => 5,
            _ => 36
        };

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
