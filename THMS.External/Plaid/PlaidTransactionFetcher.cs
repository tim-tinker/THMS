using Going.Plaid.Transactions;

namespace THMS.External.Plaid
{
    public class PlaidTransactionFetcher : IExternalTransactionFetcher
    {
        private readonly PlaidServiceClient _client;

        public PlaidTransactionFetcher(PlaidServiceClient client)
        {
            _client = client;
        }

        public virtual async Task<List<TransactionDto>> FetchTransactionsAsync(
            AccountDto account,
            DateTime start,
            DateTime end)
        {
            var response = await _client.Raw.TransactionsGetAsync(new TransactionsGetRequest
            {
                AccessToken = account.AccessToken,
                StartDate = DateOnly.FromDateTime(start),
                EndDate = DateOnly.FromDateTime(end)
            });

            if (response.Error is not null)
                throw new InvalidOperationException(response.Error.ErrorMessage);

            return response.Transactions.Select(t => t.ToDto()).ToList();
        }
    }
}
