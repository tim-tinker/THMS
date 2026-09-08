using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Recurrence
{
    public class RecurringDetector
    {
        private const decimal AmountVarianceThreshold = 5m;
        private const int MinOccurrences = 3;

        public List<RecurringSingleTransactionRule> DetectRecurringSingles(
            IEnumerable<PostedTransaction> historical,
            IEnumerable<RecurringSingleTransactionRule> existingRules)
        {
            var results = new List<RecurringSingleTransactionRule>();
            var existing = existingRules.ToList();

            var groups = historical.GroupBy(t => (
                t.AccountId,
                RecurringRulePattern.NormalizeDescription(t.Description),
                t.Amount));

            foreach (var g in groups)
            {
                var distinct = DistinctByDate(g);
                if (distinct.Count < MinOccurrences)
                    continue;

                var freq = ClassifyFrequency(Deltas(distinct));
                if (freq == null)
                    continue;

                var variance = distinct.Max(t => t.Amount) - distinct.Min(t => t.Amount);
                if (variance > AmountVarianceThreshold)
                    continue;

                var last = distinct[^1];
                var amount = g.Key.Amount;
                var match = existing.FirstOrDefault(r =>
                    RecurringRulePattern.MatchesMerchantSchedule(r, last.AccountId, last.Description, freq.Value));
                if (match is not null)
                {
                    ApplyDetectedSchedule(match, last, amount, freq.Value);
                    continue;
                }

                var created = new RecurringSingleTransactionRule
                {
                    Id = Guid.NewGuid(),
                    AccountId = last.AccountId,
                    Description = last.Description,
                    Amount = amount,
                    Category = last.Category,
                    CategoryId = last.CategoryId,
                    Frequency = freq.Value,
                    LastOccurrence = last.Date,
                    NextOccurrence = last.Date.AddFrequency(freq.Value),
                    IsActive = true,
                    IsUserCreated = false
                };
                results.Add(created);
                existing.Add(created);
            }

            return results;
        }

        public List<RecurringTransferRule> DetectRecurringTransfers(
            IEnumerable<PostedTransferTransaction> historical,
            IEnumerable<RecurringTransferRule> existingRules)
        {
            var results = new List<RecurringTransferRule>();
            var existing = existingRules.ToList();

            var groups = historical.GroupBy(t => (
                t.AccountId,
                RecurringRulePattern.NormalizeDescription(t.Description),
                t.Amount));

            foreach (var g in groups)
            {
                var distinct = DistinctByDate(g);
                if (distinct.Count < MinOccurrences)
                    continue;

                var freq = ClassifyFrequency(Deltas(distinct));
                if (freq == null)
                    continue;

                var variance = distinct.Max(t => t.Amount) - distinct.Min(t => t.Amount);
                if (variance > AmountVarianceThreshold)
                    continue;

                var last = distinct[^1];
                var amount = g.Key.Amount;
                var match = existing.FirstOrDefault(r =>
                    RecurringRulePattern.MatchesMerchantSchedule(r, last.AccountId, last.Description, freq.Value));
                if (match is not null)
                {
                    ApplyDetectedSchedule(match, last, amount, freq.Value);
                    continue;
                }

                var created = new RecurringTransferRule
                {
                    Id = Guid.NewGuid(),
                    FromAccountId = last.AccountId,
                    ToAccountId = last.AccountId,
                    Description = last.Description,
                    Amount = amount,
                    Category = last.Category,
                    CategoryId = last.CategoryId,
                    Frequency = freq.Value,
                    LastOccurrence = last.Date,
                    NextOccurrence = last.Date.AddFrequency(freq.Value),
                    IsActive = true,
                    IsUserCreated = false
                };
                results.Add(created);
                existing.Add(created);
            }

            return results;
        }

        public static IEnumerable<Guid> DuplicateAutoRuleIds(
            IEnumerable<RecurringSingleTransactionRule> rules)
        {
            foreach (var group in rules.GroupBy(r => (
                r.AccountId,
                RecurringRulePattern.NormalizeDescription(r.Description),
                decimal.Round(r.Amount, 2),
                r.Frequency)))
            {
                foreach (var extra in ExtrasToDelete(group.ToList(), r => r.IsUserCreated, r => r.LastOccurrence ?? r.NextOccurrence))
                    yield return extra.Id;
            }
        }

        public static IEnumerable<Guid> DuplicateAutoRuleIds(
            IEnumerable<RecurringTransferRule> rules)
        {
            foreach (var group in rules.GroupBy(r => (
                r.FromAccountId,
                RecurringRulePattern.NormalizeDescription(r.Description),
                decimal.Round(r.Amount, 2),
                r.Frequency)))
            {
                foreach (var extra in ExtrasToDelete(group.ToList(), r => r.IsUserCreated, r => r.LastOccurrence ?? r.NextOccurrence))
                    yield return extra.Id;
            }
        }

        private static void ApplyDetectedSchedule(
            RecurringSingleTransactionRule rule,
            PostedTransaction last,
            decimal amount,
            RecurrenceFrequency frequency)
        {
            if (rule.IsUserCreated)
                return;

            rule.Amount = amount;
            rule.Category = last.Category;
            rule.CategoryId = last.CategoryId;
            rule.Description = last.Description;
            rule.LastOccurrence = last.Date;
            rule.NextOccurrence = last.Date.AddFrequency(frequency);
        }

        private static void ApplyDetectedSchedule(
            RecurringTransferRule rule,
            PostedTransferTransaction last,
            decimal amount,
            RecurrenceFrequency frequency)
        {
            if (rule.IsUserCreated)
                return;

            rule.Amount = amount;
            rule.Category = last.Category;
            rule.CategoryId = last.CategoryId;
            rule.Description = last.Description;
            rule.LastOccurrence = last.Date;
            rule.NextOccurrence = last.Date.AddFrequency(frequency);
        }

        private static List<T> DistinctByDate<T>(IEnumerable<T> items) where T : BaseTransaction =>
            items
                .GroupBy(t => t.Date.Date)
                .Select(day => day.OrderBy(t => t.Date).Last())
                .OrderBy(t => t.Date)
                .ToList();

        private static List<double> Deltas<T>(IReadOnlyList<T> ordered) where T : BaseTransaction =>
            ordered.Zip(ordered.Skip(1), (a, b) => (b.Date.Date - a.Date.Date).TotalDays).ToList();

        private static IEnumerable<T> ExtrasToDelete<T>(
            List<T> members,
            Func<T, bool> isUserCreated,
            Func<T, DateTime> recency) where T : BaseDomainModel
        {
            if (members.Count < 2)
                yield break;

            var keep = members.FirstOrDefault(isUserCreated)
                ?? members.OrderByDescending(recency).ThenBy(r => r.Id).First();

            foreach (var extra in members.Where(r => r.Id != keep.Id && !isUserCreated(r)))
                yield return extra;
        }

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
