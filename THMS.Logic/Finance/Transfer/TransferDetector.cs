using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Transfer
{
    public class TransferDetector
    {
        public List<PostedTransferTransaction> Detected { get; } = [];

        public List<PostedTransaction> Matched { get; } = [];

        public void DetectTransfers(List<PostedTransaction> posted)
        {
            var used = new HashSet<Guid>();
            foreach (var day in posted.GroupBy(t => t.Date.Date))
            {
                var positives = day.Where(t => t.Amount > 0).ToList();
                var negatives = day.Where(t => t.Amount < 0).ToList();

                foreach (var positive in positives)
                {
                    if (!used.Add(positive.Id))
                        continue;

                    var negative = negatives.FirstOrDefault(candidate =>
                        candidate.Amount == -positive.Amount
                        && candidate.AccountId != positive.AccountId
                        && used.Add(candidate.Id));
                    if (negative is null)
                    {
                        used.Remove(positive.Id);
                        continue;
                    }

                    Detected.Add(new PostedTransferTransaction(
                        positive,
                        negative.Id,
                        TransferDirection.Incoming));
                    Detected.Add(new PostedTransferTransaction(
                        negative,
                        positive.Id,
                        TransferDirection.Outgoing));
                    Matched.Add(positive);
                    Matched.Add(negative);
                }
            }
        }
    }
}
