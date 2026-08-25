using Going.Plaid.Accounts;

namespace THMS.External.Plaid
{
    public class PlaidAccountFetcher
    {
        private readonly PlaidServiceClient _client;

        public PlaidAccountFetcher(PlaidServiceClient client)
        {
            _client = client;
        }

        public virtual async Task<List<AccountDto>> FetchAccountsAsync(string accessToken)
        {
            var response = await _client.Raw.AccountsGetAsync(new AccountsGetRequest
            {
                AccessToken = accessToken
            });

            if (response.Error is not null)
                throw new InvalidOperationException(response.Error.ErrorMessage);

            return response.Accounts.Select(a => a.ToDto()).ToList();
        }
    }
}
