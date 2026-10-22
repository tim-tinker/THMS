using THMS.Domain.Finance.Accounts;
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
            IEnumerable<ExpenseCategory>? categories = null,
            IReadOnlyDictionary<Guid, Account>? accounts = null,
            IEnumerable<PostedTransferTransaction>? transfers = null)
        {
            var catalog = categories?.ToList() ?? [];
            var included = Expand(includedCategoryIds, catalog.Count == 0 ? null : catalog);
            var role = RuleRole(included, catalog);
            decimal actual = 0;
            foreach (var line in Lines(posted, transfers, catalog, accounts, periodStart, periodEnd))
            {
                if (!Matches(line.CategoryId, line.CategoryName, included, catalog.Count == 0 ? null : catalog))
                    continue;
                actual += Portion(role, line);
            }

            return role == CategoryRole.Expense ? Math.Max(actual, 0) : actual;
        }

        public bool IncludesTransaction(
            PostedTransaction transaction,
            IEnumerable<Guid> includedCategoryIds,
            IEnumerable<ExpenseCategory>? categories = null,
            IReadOnlyDictionary<Guid, Account>? accounts = null)
        {
            var catalog = categories?.ToList() ?? [];
            var included = Expand(includedCategoryIds, catalog.Count == 0 ? null : catalog);
            var role = RuleRole(included, catalog);
            return Lines([transaction], transfers: null, catalog, accounts, DateTime.MinValue, DateTime.MaxValue)
                .Any(line =>
                    Matches(line.CategoryId, line.CategoryName, included, catalog.Count == 0 ? null : catalog) &&
                    Portion(role, line) != 0);
        }

        private static List<CashContribution> Lines(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction>? transfers,
            List<ExpenseCategory> catalog,
            IReadOnlyDictionary<Guid, Account>? accounts,
            DateTime periodStart,
            DateTime periodEnd) =>
            CategoryCashFlow.Read(posted, transfers, catalog, accounts)
                .Where(line => line.Date.Date >= periodStart.Date && line.Date.Date <= periodEnd.Date)
                .ToList();

        private static CategoryRole RuleRole(HashSet<Guid> included, List<ExpenseCategory> catalog)
        {
            if (catalog.Count == 0 || included.Count == 0)
                return CategoryRole.Expense;
            return CategoryRoleResolver.UniformRole(included, catalog) ?? CategoryRole.Expense;
        }

        private static decimal Portion(CategoryRole role, CashContribution line) => role switch
        {
            CategoryRole.Income => line.Income,
            CategoryRole.Investment => line.SetAside,
            CategoryRole.Transfer => 0,
            _ => line.Spending
        };

        public decimal ComputeRecommendedAmount(
            IEnumerable<ExpenseBudgetHistory> history,
            BudgetFrequency frequency,
            IEnumerable<PostedTransaction>? posted = null,
            IEnumerable<Guid>? includedCategoryIds = null,
            IEnumerable<ExpenseCategory>? categories = null,
            DateTime? cycleStart = null,
            DateTime? asOf = null,
            IReadOnlyDictionary<Guid, Account>? accounts = null,
            IEnumerable<PostedTransferTransaction>? transfers = null,
            IReadOnlyList<CashContribution>? prepared = null)
        {
            var samples = history
                .Select(h => (Start: h.PeriodStart.Date, End: h.PeriodEnd.Date, Actual: h.ActualExpenses))
                .ToList();

            if (prepared is not null && includedCategoryIds is not null)
                AddGapSamples(samples, prepared, includedCategoryIds, categories, frequency, cycleStart);
            else if (posted is not null && includedCategoryIds is not null)
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
                            ComputeActualExpenses(posted, includedCategoryIds, start, end, categories, accounts, transfers)));
                    }
                }
            }

            var persistedStarts = history.Select(h => h.PeriodStart.Date).ToHashSet();
            samples = samples
                .Where(s => s.Actual > 0 || persistedStarts.Contains(s.Start))
                .ToList();

            return BudgetRecommendation.Compute(samples, frequency, cycleStart, asOf);
        }

        public decimal SumPrepared(
            IReadOnlyList<CashContribution> lines,
            IEnumerable<Guid> includedCategoryIds,
            DateTime periodStart,
            DateTime periodEnd,
            IEnumerable<ExpenseCategory>? categories = null)
        {
            var catalog = categories?.ToList() ?? [];
            var included = Expand(includedCategoryIds, catalog.Count == 0 ? null : catalog);
            var role = RuleRole(included, catalog);
            var catalogOrNull = catalog.Count == 0 ? null : catalog;
            decimal actual = 0;
            foreach (var line in lines)
            {
                if (line.Date.Date < periodStart.Date || line.Date.Date > periodEnd.Date)
                    continue;
                if (!Matches(line.CategoryId, line.CategoryName, included, catalogOrNull))
                    continue;
                actual += Portion(role, line);
            }

            return role == CategoryRole.Expense ? Math.Max(actual, 0) : actual;
        }

        private void AddGapSamples(
            List<(DateTime Start, DateTime End, decimal Actual)> samples,
            IReadOnlyList<CashContribution> lines,
            IEnumerable<Guid> includedCategoryIds,
            IEnumerable<ExpenseCategory>? categories,
            BudgetFrequency frequency,
            DateTime? cycleStart)
        {
            if (lines.Count == 0)
                return;

            var catalog = categories?.ToList() ?? [];
            var included = Expand(includedCategoryIds, catalog.Count == 0 ? null : catalog);
            var role = RuleRole(included, catalog);
            var catalogOrNull = catalog.Count == 0 ? null : catalog;
            var names = catalog
                .GroupBy(category => DefaultExpenseCategories.CanonicalName(category.Name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
            var covered = samples.Select(s => (s.Start, s.End)).ToHashSet();
            var totals = new Dictionary<(DateTime Start, DateTime End), decimal>();
            DateTime first = DateTime.MaxValue;
            DateTime last = DateTime.MinValue;
            foreach (var line in lines)
            {
                var date = line.Date.Date;
                if (date < first)
                    first = date;
                if (date > last)
                    last = date;
                if (!MatchesPrepared(line, included, names))
                    continue;
                var window = BudgetPeriodCalculator.PeriodContaining(date, frequency, cycleStart);
                totals.TryGetValue(window, out var sum);
                totals[window] = sum + Portion(role, line);
            }

            foreach (var window in BudgetPeriodCalculator.PeriodsOverlapping(first, last, frequency, cycleStart))
            {
                if (covered.Contains(window))
                    continue;
                totals.TryGetValue(window, out var sum);
                var actual = role == CategoryRole.Expense ? Math.Max(sum, 0) : sum;
                samples.Add((window.Start, window.End, actual));
            }
        }

        private static bool MatchesPrepared(
            CashContribution line,
            HashSet<Guid> included,
            Dictionary<string, Guid> names)
        {
            if (included.Contains(line.CategoryId))
                return true;
            if (string.IsNullOrWhiteSpace(line.CategoryName))
                return false;
            var canonical = DefaultExpenseCategories.CanonicalName(line.CategoryName);
            return names.TryGetValue(canonical, out var id) && included.Contains(id);
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
