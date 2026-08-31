using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class ForecastGenerator
    {
        // ------------------------------------------------------------
        // Generate future singles from recurring single rules
        // ------------------------------------------------------------
        public List<FutureSingleTransaction> GenerateFutureSingles(
            IEnumerable<RecurringSingleTransactionRule> rules)
        {
            var results = new List<FutureSingleTransaction>();

            foreach (var rule in rules.Where(r => r.IsActive))
            {
                var next = rule.NextOccurrence;

                // simple 3-month forecast
                var end = DateTime.Today.AddMonths(3);

                while (next <= end && (rule.EndDate == null || next <= rule.EndDate.Value))
                {
                    var amount = rule.IsFinalPaymentDifferent &&
                                 rule.EndDate.HasValue &&
                                 next == rule.EndDate.Value
                        ? rule.FinalPaymentAmount ?? rule.Amount
                        : rule.Amount;

                    results.Add(new FutureSingleTransaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = rule.AccountId,
                        Date = next,
                        Amount = amount,
                        Category = rule.Category,
                        Description = rule.Description,
                        IsRealized = false
                    });

                    next = next.AddFrequency(rule.Frequency);
                }

                rule.NextOccurrence = next;
            }

            return results;
        }

        // ------------------------------------------------------------
        // Generate future transfers from recurring transfer rules
        // ------------------------------------------------------------
        public List<FutureTransferTransaction> GenerateFutureTransfers(
            IEnumerable<RecurringTransferRule> rules)
        {
            var results = new List<FutureTransferTransaction>();

            foreach (var rule in rules.Where(r => r.IsActive))
            {
                var next = rule.NextOccurrence;
                var end = DateTime.Today.AddMonths(3);

                while (next <= end && (rule.EndDate == null || next <= rule.EndDate.Value))
                {
                    var amount = rule.IsFinalPaymentDifferent &&
                                 rule.EndDate.HasValue &&
                                 next == rule.EndDate.Value
                        ? rule.FinalPaymentAmount ?? rule.Amount
                        : rule.Amount;

                    results.Add(new FutureTransferTransaction
                    {
                        Id = Guid.NewGuid(),
                        FromAccountId = rule.FromAccountId,
                        ToAccountId = rule.ToAccountId,
                        Date = next,
                        Amount = amount,
                        Category = rule.Category,
                        Description = rule.Description,
                        IsRealized = false
                    });

                    next = next.AddFrequency(rule.Frequency);
                }

                rule.NextOccurrence = next;
            }

            return results;
        }
    }
}
