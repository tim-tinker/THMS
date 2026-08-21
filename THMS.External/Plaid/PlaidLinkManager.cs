using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;

namespace THMS.External.Plaid
{
    public class PlaidLinkManager
    {
        private readonly PlaidClient _client;

        public PlaidLinkManager(PlaidClient client)
        {
            _client = client;
        }

        public virtual async Task<string> CreateLinkTokenAsync(string userId)
        {
            var response = await _client.Raw.LinkTokenCreateAsync(new LinkTokenCreateRequest
            {
                User = new LinkTokenCreateRequestUser { ClientUserId = userId },
                ClientName = "THMS",
                Products = [Products.Transactions],
                CountryCodes = [CountryCode.Us],
                Language = Language.English
            });

            return response.LinkToken ?? throw new InvalidOperationException(
                response.Error?.ErrorMessage ?? "Plaid link token create failed.");
        }

        public virtual async Task<string> ExchangePublicTokenAsync(string publicToken)
        {
            var response = await _client.Raw.ItemPublicTokenExchangeAsync(new ItemPublicTokenExchangeRequest
            {
                PublicToken = publicToken
            });

            return response.AccessToken ?? throw new InvalidOperationException(
                response.Error?.ErrorMessage ?? "Plaid public token exchange failed.");
        }
    }
}
