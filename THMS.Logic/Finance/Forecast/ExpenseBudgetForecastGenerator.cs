using THMS.Domain.Finance.Transactions;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Forecast
{
    public class ExpenseBudgetForecastGenerator
    {
        private const int MaxOccurrences = 36;

        public List<UnifiedTransactionView> Generate(ExpenseBudgetRule rule, DateTime from, DateTime to)
        {
            var results = new List<UnifiedTransactionView>();
            var next = rule.NextOccurrence;
            var count = 0;

            while (next <= to && count < MaxOccurrences)
            {
                if (next >= from)
                {
                    results.Add(new UnifiedTransactionView
                    {
                        Id = Guid.NewGuid(),
                        AccountId = rule.AccountId,
                        Date = next,
                        Amount = rule.CurrentAverage,
                        Category = rule.Category,
                        Description = $"{rule.Category} Budget",
                        Type = UnifiedTransactionView.ForecastBudgetType,
                        ForecastBalance = null
                    });
                }

                next = next.AddMonths(1);
                count++;
            }

            return results;
        }
    }
}
