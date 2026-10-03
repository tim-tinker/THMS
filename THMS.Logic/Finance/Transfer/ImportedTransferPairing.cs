using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Transfer
{
    public sealed class ImportedTransferPair
    {
        public required PostedTransaction FromSide { get; init; }
        public required PostedTransaction ToSide { get; init; }
        public RecurringTransferRule? Rule { get; init; }
    }

    public static class ImportedTransferPairing
    {
        public const int DayWindow = 4;

        public static List<ImportedTransferPair> Find(
            IReadOnlyList<PostedTransaction> imported,
            IReadOnlyList<PostedTransaction> existing,
            IReadOnlyList<RecurringTransferRule> rules)
        {
            var importedIds = imported.Select(t => t.Id).ToHashSet();
            var pool = existing
                .Concat(imported)
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .Where(t => t.Amount != 0)
                .ToList();
            var used = new HashSet<Guid>();
            var pairs = new List<ImportedTransferPair>();

            foreach (var candidate in imported.Where(t => t.Amount != 0))
            {
                if (!used.Add(candidate.Id))
                    continue;

                if (BestMatch(candidate, pool, used, importedIds, rules) is not Match match)
                {
                    used.Remove(candidate.Id);
                    continue;
                }

                used.Add(match.Other.Id);
                var negative = candidate.Amount < 0 ? candidate : match.Other;
                var positive = candidate.Amount > 0 ? candidate : match.Other;
                pairs.Add(new ImportedTransferPair
                {
                    FromSide = negative,
                    ToSide = positive,
                    Rule = match.Rule
                });
            }

            return pairs;
        }

        private static Match? BestMatch(
            PostedTransaction candidate,
            IReadOnlyList<PostedTransaction> pool,
            HashSet<Guid> used,
            HashSet<Guid> importedIds,
            IReadOnlyList<RecurringTransferRule> rules)
        {
            Match? best = null;
            foreach (var other in pool)
            {
                if (other.Id == candidate.Id || used.Contains(other.Id))
                    continue;
                if (!importedIds.Contains(candidate.Id) && !importedIds.Contains(other.Id))
                    continue;
                if (other.AccountId == candidate.AccountId || other.AccountId == Guid.Empty)
                    continue;
                if (other.Amount != -candidate.Amount)
                    continue;
                if (Math.Abs((other.Date.Date - candidate.Date.Date).TotalDays) > DayWindow)
                    continue;

                var negative = candidate.Amount < 0 ? candidate : other;
                var positive = candidate.Amount > 0 ? candidate : other;
                var rule = MatchingRule(negative, positive, rules);
                var described = DescribesTransfer(candidate.Description) || DescribesTransfer(other.Description);
                if (rule is null && !described)
                    continue;

                var rank = (rule is null ? 1 : 0, Math.Abs((other.Date.Date - candidate.Date.Date).TotalDays));
                if (best is null || Compare(rank, best.Value.Rank) < 0)
                    best = new Match(other, rule, rank);
            }

            return best;
        }

        private static int Compare((int RuleRank, double Days) left, (int RuleRank, double Days) right)
        {
            var byRule = left.RuleRank.CompareTo(right.RuleRank);
            return byRule != 0 ? byRule : left.Days.CompareTo(right.Days);
        }

        private static RecurringTransferRule? MatchingRule(
            PostedTransaction fromSide,
            PostedTransaction toSide,
            IReadOnlyList<RecurringTransferRule> rules)
        {
            return rules.FirstOrDefault(rule =>
                rule.IsActive
                && rule.FromAccountId == fromSide.AccountId
                && rule.ToAccountId == toSide.AccountId
                && Math.Abs(Math.Abs(fromSide.Amount) - Math.Abs(rule.Amount)) < 0.01m
                && (DescribesTransfer(fromSide.Description)
                    || DescribesTransfer(toSide.Description)
                    || Math.Abs((fromSide.Date.Date - rule.NextOccurrence.Date).TotalDays) <= DayWindow
                    || Math.Abs((toSide.Date.Date - rule.NextOccurrence.Date).TotalDays) <= DayWindow));
        }

        private static bool DescribesTransfer(string? description) =>
            (description ?? "").Contains("TRANSFER", StringComparison.OrdinalIgnoreCase);

        private readonly record struct Match(PostedTransaction Other, RecurringTransferRule? Rule, (int RuleRank, double Days) Rank);
    }
}
