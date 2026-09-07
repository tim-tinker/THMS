using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.ViewModels.Finance
{
    public static class UnifiedTransactionViewBuilder
    {
        public static List<UnifiedTransactionView> Build(
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction> postedTransfers,
            IEnumerable<FutureSingleTransaction>? userFutureSingles = null,
            IEnumerable<FutureTransferTransaction>? userFutureTransfers = null)
        {
            var list = new List<UnifiedTransactionView>();

            foreach (var tx in posted)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.AccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = UnifiedTransactionView.PostedType,
                    ForecastBalance = null
                });
            }

            foreach (var tx in postedTransfers)
            {
                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.AccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = UnifiedTransactionView.PostedTransferType,
                    ForecastBalance = null
                });
            }

            foreach (var tx in userFutureSingles ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.AccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = UnifiedTransactionView.FutureType,
                    ForecastBalance = null
                });
            }

            foreach (var tx in userFutureTransfers ?? [])
            {
                if (!tx.IsUserCreated || tx.IsRealized)
                    continue;

                list.Add(new UnifiedTransactionView
                {
                    Id = tx.Id,
                    AccountId = tx.FromAccountId,
                    Date = tx.Date,
                    Description = tx.Description ?? "",
                    Amount = tx.Amount,
                    Category = tx.Category,
                    Type = UnifiedTransactionView.FutureTransferType,
                    ForecastBalance = null
                });
            }

            return list.OrderBy(t => t.Date).ThenBy(t => t.Id).ToList();
        }
    }
}
