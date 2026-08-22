using Going.Plaid;

namespace THMS.External.Plaid
{
    public class PlaidClient
    {
        private readonly Going.Plaid.PlaidClient _client;

        public PlaidClient(string clientId, string secret, string baseUrl)
        {
            _client = new Going.Plaid.PlaidClient(
                new HttpClient(),
                new PlaidClientOptions
                {
                    ClientId = clientId,
                    Secret = secret,
                    BaseUrl = baseUrl
                });
        }

        public virtual Going.Plaid.PlaidClient Raw => _client;
    }
}
