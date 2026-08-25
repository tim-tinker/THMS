using System.Collections.Generic;
using System.Linq;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class TransferDetector
    {
        public List<PostedTransferTransaction> DetectTransfers(List<PostedTransaction> posted)
        {
            var results = new List<PostedTransferTransaction>();

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
                            results.Add(new PostedTransferTransaction(
                                p,
                                n.Id,
                                TransferDirection.Incoming));

                            results.Add(new PostedTransferTransaction(
                                n,
                                p.Id,
                                TransferDirection.Outgoing));
                        }
                    }
            }

            return results;
        }
    }
}
