using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Forecast
{
    public class FutureReconciler
    {
        public List<RecurringSingleTransactionRule> MatchedSingleRules { get; } = [];
        public List<RecurringTransferRule> MatchedTransferRules { get; } = [];

        public void ReconcileSingles(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<RecurringSingleTransactionRule> rules,
            int dayTolerance = 4)
        {
            MatchedSingleRules.Clear();
            var usedPosted = new HashSet<Guid>();
            var postedList = posted.OrderBy(p => p.Date).ToList();

            foreach (var rule in rules.Where(r => r.IsActive))
            {
                var matched = false;

                foreach (var p in postedList)
                {
                    if (usedPosted.Contains(p.Id))
                        continue;
                    if (p.AccountId != rule.AccountId)
                        continue;
                    if (!IsAmountMatch(p.Amount, rule.Amount))
                        continue;
                    if (!IsDateMatch(p.Date, rule.NextOccurrence, dayTolerance) &&
                        !IsLateDescriptionMatch(p, rule, dayTolerance))
                        continue;

                    ApplyPostedToSingleRule(rule, p);
                    usedPosted.Add(p.Id);
                    matched = true;
                }

                if (matched)
                    MatchedSingleRules.Add(rule);
            }
        }

        public void ReconcileTransfers(
            IEnumerable<PostedTransferTransaction> postedTransfers,
            IEnumerable<RecurringTransferRule> rules,
            int dayTolerance = 4)
        {
            MatchedTransferRules.Clear();
            var usedPosted = new HashSet<Guid>();
            var postedList = postedTransfers.OrderBy(p => p.Date).ToList();

            foreach (var rule in rules.Where(r => r.IsActive))
            {
                var matched = false;

                foreach (var p in postedList)
                {
                    if (usedPosted.Contains(p.Id))
                        continue;
                    if (p.AccountId != rule.FromAccountId && p.AccountId != rule.ToAccountId)
                        continue;
                    if (!IsAmountMatch(p.Amount, rule.Amount))
                        continue;
                    if (!IsDateMatch(p.Date, rule.NextOccurrence, dayTolerance) &&
                        !IsLateTransferDescriptionMatch(p, rule, dayTolerance))
                        continue;

                    ApplyPostedToTransferRule(rule, p);
                    usedPosted.Add(p.Id);
                    matched = true;
                }

                if (matched)
                    MatchedTransferRules.Add(rule);
            }
        }

        private static void ApplyPostedToSingleRule(RecurringSingleTransactionRule rule, PostedTransaction posted)
        {
            rule.LastOccurrence = posted.Date;
            rule.NextOccurrence = posted.Date.AddFrequency(rule.Frequency);
            rule.Amount = posted.Amount;
            if (!string.IsNullOrWhiteSpace(posted.Category))
                rule.Category = posted.Category;
            if (!string.IsNullOrWhiteSpace(posted.Description))
                rule.Description = posted.Description;
        }

        private static void ApplyPostedToTransferRule(RecurringTransferRule rule, PostedTransferTransaction posted)
        {
            rule.LastOccurrence = posted.Date;
            rule.NextOccurrence = posted.Date.AddFrequency(rule.Frequency);
            rule.Amount = Math.Abs(posted.Amount);
            if (!string.IsNullOrWhiteSpace(posted.Category))
                rule.Category = posted.Category;
            if (!string.IsNullOrWhiteSpace(posted.Description))
                rule.Description = posted.Description;
        }

        private static bool IsAmountMatch(decimal postedAmount, decimal ruleAmount) =>
            Math.Abs(Math.Abs(postedAmount) - Math.Abs(ruleAmount)) < 0.01m;

        private static bool IsDateMatch(DateTime postedDate, DateTime nextOccurrence, int dayTolerance) =>
            Math.Abs((postedDate.Date - nextOccurrence.Date).TotalDays) <= dayTolerance;

        private static bool IsLateDescriptionMatch(
            PostedTransaction posted,
            RecurringSingleTransactionRule rule,
            int dayTolerance)
        {
            if (!string.Equals(posted.Description, rule.Description, StringComparison.OrdinalIgnoreCase))
                return false;

            return posted.Date.Date >= rule.NextOccurrence.Date.AddDays(-dayTolerance);
        }

        private static bool IsLateTransferDescriptionMatch(
            PostedTransferTransaction posted,
            RecurringTransferRule rule,
            int dayTolerance)
        {
            if (!string.Equals(posted.Description, rule.Description, StringComparison.OrdinalIgnoreCase))
                return false;

            return posted.Date.Date >= rule.NextOccurrence.Date.AddDays(-dayTolerance);
        }
    }
}
