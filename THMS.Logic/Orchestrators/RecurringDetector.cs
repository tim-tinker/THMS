using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class RecurringDetector
    {
        // ------------------------------------------------------------
        // Detect recurring single-account rules
        // ------------------------------------------------------------
        public List<RecurringSingleTransactionRule> DetectRecurringSingles(
            IEnumerable<PostedTransaction> posted)
        {
            var results = new List<RecurringSingleTransactionRule>();

            var groups = posted.GroupBy(t => t.Description);

            foreach (var g in groups)
            {
                var ordered = g.OrderBy(t => t.Date).ToList();
                if (ordered.Count < 3) continue;

                var avg = ordered.Average(t => t.Amount);
                var variance = ordered.Max(t => t.Amount) - ordered.Min(t => t.Amount);

                if (variance < 5)
                {
                    results.Add(new RecurringSingleTransactionRule
                    {
                        Id = Guid.NewGuid(),
                        AccountId = ordered.First().AccountId,
                        Description = g.Key,
                        Amount = avg,
                        Frequency = Frequency.Monthly, // heuristic
                        NextOccurrence = ordered.Last().Date.AddMonths(1),
                        IsActive = true
                    });
                }
            }

            return results;
        }

        // ------------------------------------------------------------
        // Detect recurring transfer rules
        // ------------------------------------------------------------
        public List<RecurringTransferRule> DetectRecurringTransfers(
            IEnumerable<PostedTransaction> posted)
        {
            var results = new List<RecurringTransferRule>();

            var transfers = posted.Where(t => t.IsTransferCandidate()).ToList();
            var groups = transfers.GroupBy(t => t.Description);

            foreach (var g in groups)
            {
                var ordered = g.OrderBy(t => t.Date).ToList();
                if (ordered.Count < 3) continue;

                var avg = ordered.Average(t => t.Amount);
                var variance = ordered.Max(t => t.Amount) - ordered.Min(t => t.Amount);

                if (variance < 5)
                {
                    results.Add(new RecurringTransferRule
                    {
                        Id = Guid.NewGuid(),
                        FromAccountId = ordered.First().AccountId,
                        ToAccountId = ordered.First().AccountId, // placeholder
                        Description = g.Key,
                        Amount = avg,
                        Frequency = Frequency.Monthly,
                        NextOccurrence = ordered.Last().Date.AddMonths(1),
                        IsActive = true
                    });
                }
            }

            return results;
        }
    }
}
