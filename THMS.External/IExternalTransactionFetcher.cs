namespace THMS.External
{
    public interface IExternalTransactionFetcher
    {
        Task<List<TransactionDto>> FetchTransactionsAsync(
            AccountDto account,
            DateTime start,
            DateTime end);

        Task<TransactionSyncPage> SyncTransactionsAsync(
            string accessToken,
            string? cursor,
            CancellationToken cancellationToken = default);
    }
}
