using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Forecast
{
    public class ExpenseBudgetForecastGenerator
    {
        public List<FutureSingleTransaction> Generate(ExpenseBudgetRule rule)
        {
            var results = new List<FutureSingleTransaction>();

            var next = rule.NextOccurrence;
            var end = DateTime.Today.AddMonths(3);

            while (next <= end)
            {
                results.Add(new FutureSingleTransaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = rule.AccountId,
                    Date = next,
                    Amount = rule.CurrentAverage,
                    Category = rule.Category,
                    Description = $"{rule.Category} Budget",
                    IsRealized = false
                });

                next = next.AddMonths(1);
            }

            rule.NextOccurrence = next;
            return results;
        }
    }
}
