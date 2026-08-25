using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class RecurringDetector
    {
        public List<RecurringRule> DetectRecurring(List<PostedTransaction> tx)
        {
            var results = new List<RecurringRule>();

            var groups = tx.GroupBy(t => t.Description);

            foreach (var g in groups)
            {
                var ordered = g.OrderBy(t => t.Date).ToList();
                if (ordered.Count < 3) continue;

                // Simple heuristic: same description, similar amount
                var avg = ordered.Average(t => t.Amount);
                var variance = ordered.Max(t => t.Amount) - ordered.Min(t => t.Amount);

                if (variance < 5) // $5 variance threshold
                {
                    results.Add(new RecurringRule(g.Key, avg));
                }
            }

            return results;
        }
    }

    public record RecurringRule(string Description, decimal AverageAmount);
}
