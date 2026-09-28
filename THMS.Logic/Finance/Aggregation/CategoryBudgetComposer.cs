using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Aggregation
{
    public static class CategoryBudgetComposer
    {
        public static List<CategoryBudgetRow> Build(
            IReadOnlyList<ExpenseCategory> categories,
            IReadOnlyList<ExpenseBudgetRule> rules,
            IReadOnlyDictionary<Guid, ExpenseBudgetHistory?> activePeriods,
            DateTime today)
        {
            var catalog = categories.ToList();
            var home = AssignBudgets(catalog, rules);
            var rows = new List<CategoryBudgetRow>();
            foreach (var category in ExpenseCategoryTree.Flatten(catalog, includeInactive: true))
            {
                home.TryGetValue(category.Id, out var rule);
                ExpenseBudgetHistory? period = null;
                if (rule is not null)
                    activePeriods.TryGetValue(rule.Id, out period);

                rows.Add(new CategoryBudgetRow
                {
                    CategoryId = category.Id,
                    BudgetRuleId = rule?.Id,
                    Name = ExpenseCategoryTree.IndentedName(catalog, category),
                    Active = category.IsActive ? "Active" : "Inactive",
                    Remaining = period?.Remaining,
                    PeriodStart = period?.PeriodStart.Date,
                    PeriodEnd = period?.PeriodEnd.Date,
                    Recommended = period?.RecommendedAmount,
                    Status = StatusOf(rule, period, today)
                });
            }

            return rows;
        }

        public static List<CategoryBudgetPeriodRow> BuildPeriods(IEnumerable<ExpenseBudgetHistory> history) =>
            history
                .OrderByDescending(h => h.PeriodStart)
                .Select(h => new CategoryBudgetPeriodRow
                {
                    HistoryId = h.Id,
                    BudgetRuleId = h.BudgetRuleId,
                    Period = $"{h.PeriodStart:d} – {h.PeriodEnd:d}",
                    Starting = h.StartingBalance,
                    BudgetAmount = h.BudgetAmount,
                    Actual = h.ActualExpenses,
                    Remaining = h.Remaining,
                    Recommended = h.RecommendedAmount,
                    Status = h.IsClosed ? "Closed" : "Open",
                    IsClosed = h.IsClosed
                })
                .ToList();

        public static Dictionary<Guid, ExpenseBudgetRule> AssignBudgets(
            IReadOnlyList<ExpenseCategory> categories,
            IEnumerable<ExpenseBudgetRule> rules)
        {
            var byId = categories.ToDictionary(c => c.Id);
            var assigned = new Dictionary<Guid, ExpenseBudgetRule>();
            foreach (var rule in rules.OrderBy(r => r.BudgetName, StringComparer.CurrentCultureIgnoreCase))
            {
                var homeId = HomeCategoryId(rule, byId);
                if (homeId is not Guid id || assigned.ContainsKey(id))
                    continue;
                assigned[id] = rule;
            }

            return assigned;
        }

        private static Guid? HomeCategoryId(
            ExpenseBudgetRule rule,
            IReadOnlyDictionary<Guid, ExpenseCategory> byId)
        {
            var included = rule.IncludedCategoryIds.Where(byId.ContainsKey).ToList();
            if (included.Count == 0)
            {
                return byId.Values
                    .FirstOrDefault(c =>
                        string.Equals(c.Name, rule.BudgetName, StringComparison.OrdinalIgnoreCase))
                    ?.Id;
            }

            var named = included
                .Where(id => string.Equals(byId[id].Name, rule.BudgetName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (named.Count > 0)
                return named[0];

            return included[0];
        }

        private static string StatusOf(
            ExpenseBudgetRule? rule,
            ExpenseBudgetHistory? period,
            DateTime today)
        {
            if (rule is null)
                return "";
            if (!rule.IsActive)
                return "Inactive";
            return period is null ? "" : FinanceDashboardComposer.BudgetStatus(period, today, rule.BudgetFrequency);
        }
    }
}
