using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Orchestrators
{
    public class FutureReconciler
    {
        public List<FutureSingleTransaction> MatchedSingles { get; } = [];

        public List<FutureTransferTransaction> MatchedTransfers { get; } = [];

        // ------------------------------------------------------------
        // Reconcile future singles
        // ------------------------------------------------------------
        public void ReconcileSingles(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<FutureSingleTransaction> futures)
        {
            foreach (var future in futures.Where(f => !f.IsRealized))
            {
                var match = posted.FirstOrDefault(p =>
                    Math.Abs(p.Amount) == Math.Abs(future.Amount) &&
                    p.Date.Date == future.Date.Date);

                if (match is not null)
                {
                    future.IsRealized = true;
                    future.PostedTransactionId = match.Id;

                    MatchedSingles.Add(future);
                }
            }
        }

        // ------------------------------------------------------------
        // Reconcile future transfers
        // ------------------------------------------------------------
        public void ReconcileTransfers(
            IEnumerable<PostedTransferTransaction> postedTransfers,
            IEnumerable<FutureTransferTransaction> futures)
        {
            foreach (var future in futures.Where(f => !f.IsRealized))
            {
                var fromPosted = postedTransfers.FirstOrDefault(p =>
                    p.AccountId == future.FromAccountId &&
                    Math.Abs(p.Amount) == Math.Abs(future.Amount) &&
                    p.Date.Date == future.Date.Date);

                if (fromPosted is not null)
                {
                    var toPosted = postedTransfers.FirstOrDefault(p =>
                        p.AccountId == future.ToAccountId &&
                        Math.Abs(p.Amount) == Math.Abs(future.Amount) &&
                        p.Date.Date == future.Date.Date);

                    if (toPosted is not null)
                    {

                        future.IsRealized = true;
                        future.PostedFromTransactionId = fromPosted.Id;
                        future.PostedToTransactionId = toPosted.Id;

                        MatchedTransfers.Add(future);
                    }
                }
            }
        }
    }
}
