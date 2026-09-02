using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class RecurringDetector
    {
        private const decimal AmountVarianceThreshold = 5m;
        private const int MinOccurrences = 3;

        // ------------------------------------------------------------
        // Detect recurring single-account rules (historical ledger only)
        // ------------------------------------------------------------
        public List<RecurringSingleTransactionRule> DetectRecurringSingles(
            IEnumerable<PostedTransaction> historical,
            IEnumerable<RecurringSingleTransactionRule> existingRules)
        {
            var results = new List<RecurringSingleTransactionRule>();

            var groups = historical.GroupBy(t => t.Description);

            foreach (var g in groups)
            {
                var ordered = g.OrderBy(t => t.Date).ToList();
                if (ordered.Count < MinOccurrences) continue;

                var deltas = ordered.Zip(ordered.Skip(1),
                    (a, b) => (b.Date - a.Date).TotalDays).ToList();

                var freq = ClassifyFrequency(deltas);
                if (freq == null) continue;

                var avgAmount = ordered.Average(t => t.Amount);
                var variance = ordered.Max(t => t.Amount) - ordered.Min(t => t.Amount);
                if (variance > AmountVarianceThreshold) continue;

                // dedup
                if (existingRules.Any(r =>
                    r.Description == g.Key &&
                    r.AccountId == ordered.First().AccountId &&
                    r.Frequency == freq))
                    continue;

                results.Add(new RecurringSingleTransactionRule
                {
                    Id = Guid.NewGuid(),
                    AccountId = ordered.First().AccountId,
                    Description = g.Key,
                    Amount = avgAmount,
                    Frequency = freq.Value,
                    NextOccurrence = ordered.Last().Date.AddFrequency(freq.Value),
                    IsActive = true
                });
            }

            return results;
        }

        // ------------------------------------------------------------
        // Detect recurring transfer rules (historical ledger only)
        // ------------------------------------------------------------
        public List<RecurringTransferRule> DetectRecurringTransfers(
            IEnumerable<PostedTransferTransaction> historical,
            IEnumerable<RecurringTransferRule> existingRules)
        {
            var results = new List<RecurringTransferRule>();

            var groups = historical.GroupBy(t => t.Description);

            foreach (var g in groups)
            {
                var ordered = g.OrderBy(t => t.Date).ToList();
                if (ordered.Count < MinOccurrences) continue;

                var deltas = ordered.Zip(ordered.Skip(1),
                    (a, b) => (b.Date - a.Date).TotalDays).ToList();

                var freq = ClassifyFrequency(deltas);
                if (freq == null) continue;

                var avgAmount = ordered.Average(t => t.Amount);
                var variance = ordered.Max(t => t.Amount) - ordered.Min(t => t.Amount);
                if (variance > AmountVarianceThreshold) continue;

                // dedup
                if (existingRules.Any(r =>
                    r.Description == g.Key &&
                    r.FromAccountId == ordered.First().AccountId &&
                    r.Frequency == freq))
                    continue;

                results.Add(new RecurringTransferRule
                {
                    Id = Guid.NewGuid(),
                    FromAccountId = ordered.First().AccountId,
                    ToAccountId = ordered.First().AccountId, // refine later
                    Description = g.Key,
                    Amount = avgAmount,
                    Frequency = freq.Value,
                    NextOccurrence = ordered.Last().Date.AddFrequency(freq.Value),
                    IsActive = true
                });
            }

            return results;
        }

        // ------------------------------------------------------------
        // Frequency classifier
        // ------------------------------------------------------------
        private RecurrenceFrequency? ClassifyFrequency(List<double> deltas)
        {
            if (deltas.All(d => Math.Abs(d - 7) <= 2))
                return RecurrenceFrequency.Weekly;

            if (deltas.All(d => Math.Abs(d - 14) <= 2))
                return RecurrenceFrequency.BiWeekly;

            if (deltas.All(d => Math.Abs(d - 30) <= 3))
                return RecurrenceFrequency.Monthly;

            if (deltas.All(d => Math.Abs(d - 90) <= 5))
                return RecurrenceFrequency.Quarterly;

            if (deltas.All(d => Math.Abs(d - 365) <= 7))
                return RecurrenceFrequency.Yearly;

            return null;
        }
    }
}
