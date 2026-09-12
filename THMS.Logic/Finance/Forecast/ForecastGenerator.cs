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

                    if (rule.HasSplits && amount == rule.Amount)
                    {
                        foreach (var split in rule.Splits)
                        {
                            yield return new UnifiedTransactionView
                            {
                                Id = Guid.NewGuid(),
                                ParentTransactionId = rule.Id,
                                SplitRowId = split.Id,
                                SplitKind = split.Type.ToString(),
                                AccountId = rule.AccountId,
                                Date = next,
                                Description = rule.Description ?? "",
                                Amount = split.Amount,
                                Category = split.Category,
                                CategoryId = split.CategoryId,
                                Type = UnifiedTransactionView.ForecastType,
                                ForecastBalance = null
                            };
                        }
                    }
                    else
                    {
                        yield return new UnifiedTransactionView
                        {
                            Id = Guid.NewGuid(),
                            ParentTransactionId = rule.Id,
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

                    if (rule.HasSplits && Math.Abs(amount) == Math.Abs(rule.Amount))
                    {
                        var sign = amount < 0 && rule.Amount >= 0 || amount >= 0 && rule.Amount < 0 ? -1m : 1m;
                        foreach (var split in rule.Splits)
                        {
                            yield return new UnifiedTransactionView
                            {
                                Id = Guid.NewGuid(),
                                ParentTransactionId = rule.Id,
                                SplitRowId = split.Id,
                                SplitKind = split.Type.ToString(),
                                AccountId = accountId,
                                Date = next,
                                Description = rule.Description ?? "",
                                Amount = split.Amount * sign,
                                Category = split.Category,
                                CategoryId = split.CategoryId,
                                Type = UnifiedTransactionView.ForecastTransferType,
                                ForecastBalance = null
                            };
                        }
                    }
                    else
                    {
                        yield return new UnifiedTransactionView
                        {
                            Id = Guid.NewGuid(),
                            ParentTransactionId = rule.Id,
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
                }

                next = next.AddFrequency(rule.Frequency);
                count++;
            }
        }
    }
}
