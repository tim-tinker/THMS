using Going.Plaid.Transactions;

namespace THMS.External.Plaid
{
    public class PlaidTransactionFetcher
    {
        private readonly PlaidClient _client;

        public PlaidTransactionFetcher(PlaidClient client)
        {
            _client = client;
        }

        public virtual async Task<List<PlaidTransactionDto>> FetchTransactionsAsync(
            string accessToken,
            DateTime start,
            DateTime end)
        {
            var response = await _client.Raw.TransactionsGetAsync(new TransactionsGetRequest
            {
                AccessToken = accessToken,
                StartDate = DateOnly.FromDateTime(start),
                EndDate = DateOnly.FromDateTime(end)
            });

            if (response.Error is not null)
                throw new InvalidOperationException(response.Error.ErrorMessage);

            return response.Transactions.Select(t => t.ToDto()).ToList();
        }
    }
}
