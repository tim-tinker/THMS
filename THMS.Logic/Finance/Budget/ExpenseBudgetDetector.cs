using System.Data;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;

namespace THMS.Logic.Finance.Budget
{
    public class ExpenseBudgetDetector
    {
        private readonly ExpenseBudgetRuleFactory _factory = new();
        private readonly ExpenseSmoothingModel _smoothing = new();

        public ExpenseBudgetRule Detect(
            Guid accountId,
            IEnumerable<PostedTransaction> posted,
            ExpenseBudgetRule? existingRule,
            IEnumerable<string> includedCategories)
        {
            var monthlyTotals = posted
                .Where(t => includedCategories.Contains(t.Category))
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new MonthlyExpenseTotal
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(t => t.Amount)
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToList();

            var category = existingRule?.Category ?? includedCategories.First();

            var rule = existingRule ?? _factory.Create(category);

            rule.AccountId = accountId;

            rule.CurrentAverage = _smoothing.ComputeSmoothedAverage(
                monthlyTotals,
                rule.SmoothingMode,
                rule.MonthsToAverage);

            if (existingRule is null)
                rule.NextOccurrence = DateTime.Today.AddMonths(1);

            return rule;
        }
    }
}
