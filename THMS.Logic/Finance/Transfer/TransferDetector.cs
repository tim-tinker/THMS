using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Transfer
{
    public class TransferDetector
    {
        public List<PostedTransferTransaction> Detected { get; } = [];

        public List<PostedTransaction> Matched { get; } = [];

        public void DetectTransfers(List<PostedTransaction> posted)
        {
            // same-date, same-amount, opposite sign heuristic
            var groups = posted.GroupBy(t => t.Date);

            foreach (var g in groups)
            {
                var positives = g.Where(t => t.Amount > 0).ToList();
                var negatives = g.Where(t => t.Amount < 0).ToList();

                foreach (var p in positives)
                    foreach (var n in negatives)
                    {
                        if (p.Amount == -n.Amount)
                        {
                            // add detected transactions
                            Detected.Add(new PostedTransferTransaction(
                                p,
                                n.Id,
                                TransferDirection.Incoming));

                            Detected.Add(new PostedTransferTransaction(
                                n,
                                p.Id,
                                TransferDirection.Outgoing));

                            // add matched transactions
                            Matched.Add(p);
                            Matched.Add(n);
                        }
                    }
            }
        }
    }
}
