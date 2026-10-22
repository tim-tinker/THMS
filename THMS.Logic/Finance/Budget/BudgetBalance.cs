using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Categories;

namespace THMS.Logic.Finance.Budget
{
    public sealed class BudgetBalanceResult
    {
        public decimal PlannedIncome { get; init; }
        public decimal PlannedExpenses { get; init; }
        public decimal PlannedSetAside { get; init; }
        public decimal PlanNet { get; init; }
        public decimal FullNet { get; init; }
        public decimal UnbudgetedExpenseRecommendation { get; init; }
        public decimal CoverageTotal { get; init; }
        public decimal CoverageMonthly { get; init; }
        public List<string> MixedRoleBudgets { get; init; } = [];

        public string Summary
        {
            get
            {
                var text =
                    $"Plan net {PlanNet:C2}    Full net {FullNet:C2}    Set aside {PlannedSetAside:C2}    " +
                    $"Unbudgeted spending {CoverageTotal:C2} ({CoverageMonthly:C2}/month)";
                if (MixedRoleBudgets.Count == 0)
                    return text;
                return text + Environment.NewLine +
                    "Mixed-role budgets left out: " + string.Join(", ", MixedRoleBudgets);
            }
        }
    }

    public static class BudgetBalance
    {
        public static decimal ToMonthly(decimal amount, BudgetFrequency frequency) => frequency switch
        {
            BudgetFrequency.Weekly => amount * 52m / 12m,
            BudgetFrequency.Biweekly => amount * 26m / 12m,
            BudgetFrequency.Quarterly => amount / 3m,
            BudgetFrequency.Annual => amount / 12m,
            _ => amount
        };

        public static CategoryRole? UniformRole(
            ExpenseBudgetRule rule,
            IReadOnlyList<ExpenseCategory> categories)
        {
            var expanded = ExpenseCategoryTree.ExpandWithDescendants(rule.IncludedCategoryIds, categories);
            return CategoryRoleResolver.UniformRole(expanded, categories);
        }

        public static BudgetBalanceResult Compute(
            DateTime asOf,
            IReadOnlyList<ExpenseBudgetRule> rules,
            IReadOnlyDictionary<Guid, ExpenseBudgetHistory?> activePeriods,
            IReadOnlyList<ExpenseCategory> categories,
            IReadOnlyList<CashContribution> activity)
        {
            var active = rules.Where(r => r.IsActive).ToList();
            var roles = CategoryRoleResolver.Map(categories);
            decimal income = 0;
            decimal expenses = 0;
            decimal setAside = 0;
            var mixed = new List<string>();
            var budgeted = new HashSet<Guid>();

            foreach (var rule in active)
            {
                var expanded = ExpenseCategoryTree.ExpandWithDescendants(rule.IncludedCategoryIds, categories);
                foreach (var id in expanded)
                    budgeted.Add(id);

                var role = CategoryRoleResolver.UniformRole(expanded, roles);
                if (role is null)
                {
                    if (expanded.Count > 0)
                        mixed.Add(rule.BudgetName);
                    continue;
                }

                if (role == CategoryRole.Transfer)
                    continue;

                activePeriods.TryGetValue(rule.Id, out var period);
                var monthly = ToMonthly(BudgetAmount(rule, period), rule.BudgetFrequency);
                switch (role)
                {
                    case CategoryRole.Income:
                        income += monthly;
                        break;
                    case CategoryRole.Investment:
                        setAside += monthly;
                        break;
                    default:
                        expenses += monthly;
                        break;
                }
            }

            var openMonth = new DateTime(asOf.Year, asOf.Month, 1);
            var unbudgetedExpenses = categories
                .Where(c => c.IsActive)
                .Select(c => c.Id)
                .Where(id => !budgeted.Contains(id))
                .Where(id => CategoryRoleResolver.Resolve(id, roles) == CategoryRole.Expense)
                .ToHashSet();

            var recommendation = UnbudgetedMonthlyRecommendation(openMonth, unbudgetedExpenses, activity);
            var coverageStart = openMonth.AddMonths(-12);
            var coverage = activity
                .Where(line => line.Date.Date >= coverageStart && line.Date.Date < openMonth)
                .Where(line => unbudgetedExpenses.Contains(line.CategoryId))
                .Sum(line => line.Spending);
            if (coverage < 0)
                coverage = 0;

            var planNet = income - expenses - setAside;
            return new BudgetBalanceResult
            {
                PlannedIncome = income,
                PlannedExpenses = expenses,
                PlannedSetAside = setAside,
                PlanNet = planNet,
                FullNet = planNet - recommendation,
                UnbudgetedExpenseRecommendation = recommendation,
                CoverageTotal = coverage,
                CoverageMonthly = coverage / 12m,
                MixedRoleBudgets = mixed
            };
        }

        public static decimal BudgetAmount(ExpenseBudgetRule rule, ExpenseBudgetHistory? period) =>
            Math.Abs(period?.BudgetAmount ?? rule.DefaultBudgetAmount);

        private static decimal UnbudgetedMonthlyRecommendation(
            DateTime openMonth,
            HashSet<Guid> categoryIds,
            IReadOnlyList<CashContribution> activity)
        {
            if (categoryIds.Count == 0)
                return 0;

            var byCategory = activity
                .Where(line => line.Date.Date < openMonth && categoryIds.Contains(line.CategoryId))
                .GroupBy(line => line.CategoryId)
                .ToDictionary(group => group.Key, group => group.ToList());

            decimal total = 0;
            foreach (var (categoryId, lines) in byCategory)
            {
                if (lines.Count == 0)
                    continue;
                var firstDate = lines.Min(line => line.Date);
                var first = new DateTime(firstDate.Year, firstDate.Month, 1);
                var windowStart = openMonth.AddMonths(-36);
                if (first < windowStart)
                    first = windowStart;
                var lastClosed = openMonth.AddMonths(-1);
                if (first > lastClosed)
                    continue;

                var byMonth = lines
                    .GroupBy(line => new DateTime(line.Date.Year, line.Date.Month, 1))
                    .ToDictionary(group => group.Key, group => group.Sum(line => line.Spending));
                var samples = new List<(DateTime Start, DateTime End, decimal Actual)>();
                for (var month = first; month <= lastClosed; month = month.AddMonths(1))
                {
                    byMonth.TryGetValue(month, out var actual);
                    samples.Add((month, month.AddMonths(1).AddDays(-1), actual));
                }

                total += BudgetRecommendation.Compute(samples, BudgetFrequency.Monthly, cycleStart: null, asOf: openMonth);
            }

            return total;
        }
    }
}
