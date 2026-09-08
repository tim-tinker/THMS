using THMS.Domain.Finance.Transactions;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Forecast
{
    public class ForecastGenerator
    {
        private const int MaxOccurrencesPerRule = 500;

        public List<UnifiedTransactionView> GenerateForecast(
            Guid accountId,
            DateTime from,
            DateTime to,
            IEnumerable<RecurringSingleTransactionRule> singleRules,
            IEnumerable<RecurringTransferRule> transferRules)
        {
            var results = new List<UnifiedTransactionView>();

            foreach (var rule in singleRules.Where(r => r.IsActive && r.AccountId == accountId))
                results.AddRange(ExpandSingleRule(rule, from, to));

            foreach (var rule in transferRules.Where(r => r.IsActive &&
                         (r.FromAccountId == accountId || r.ToAccountId == accountId)))
                results.AddRange(ExpandTransferRule(rule, accountId, from, to));

            return UnifiedTransactionView.OrderForRunningBalance(results).ToList();
        }

        private static IEnumerable<UnifiedTransactionView> ExpandSingleRule(
            RecurringSingleTransactionRule rule,
            DateTime from,
            DateTime to)
        {
            var next = rule.NextOccurrence;
            var count = 0;

            while (next <= to && (rule.EndDate == null || next <= rule.EndDate.Value) && count < MaxOccurrencesPerRule)
            {
                if (next >= from)
                {
                    var amount = rule.IsFinalPaymentDifferent &&
                                 rule.EndDate.HasValue &&
                                 next == rule.EndDate.Value
                        ? rule.FinalPaymentAmount ?? rule.Amount
                        : rule.Amount;

                    yield return new UnifiedTransactionView
                    {
                        Id = Guid.NewGuid(),
                        AccountId = rule.AccountId,
                        Date = next,
                        Description = rule.Description ?? "",
                        Amount = amount,
                        Category = rule.Category,
                        CategoryId = rule.CategoryId,
                        Type = UnifiedTransactionView.ForecastType,
                        ForecastBalance = null
                    };
                }

                next = next.AddFrequency(rule.Frequency);
                count++;
            }
        }

        private static IEnumerable<UnifiedTransactionView> ExpandTransferRule(
            RecurringTransferRule rule,
            Guid accountId,
            DateTime from,
            DateTime to)
        {
            var next = rule.NextOccurrence;
            var count = 0;

            while (next <= to && (rule.EndDate == null || next <= rule.EndDate.Value) && count < MaxOccurrencesPerRule)
            {
                if (next >= from)
                {
                    var amount = rule.IsFinalPaymentDifferent &&
                                 rule.EndDate.HasValue &&
                                 next == rule.EndDate.Value
                        ? rule.FinalPaymentAmount ?? rule.Amount
                        : rule.Amount;

                    if (rule.FromAccountId != rule.ToAccountId && rule.ToAccountId == accountId)
                        amount = -amount;

                    yield return new UnifiedTransactionView
                    {
                        Id = Guid.NewGuid(),
                        AccountId = accountId,
                        Date = next,
                        Description = rule.Description ?? "",
                        Amount = amount,
                        Category = rule.Category,
                        CategoryId = rule.CategoryId,
                        Type = UnifiedTransactionView.ForecastTransferType,
                        ForecastBalance = null
                    };
                }

                next = next.AddFrequency(rule.Frequency);
                count++;
            }
        }
    }
}
