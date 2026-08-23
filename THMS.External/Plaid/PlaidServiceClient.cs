using Going.Plaid;
using Microsoft.Extensions.Options;

namespace THMS.External.Plaid
{
    public class PlaidServiceClient
    {
        private readonly PlaidClient _client;

        public PlaidServiceClient(string clientId, string secret, Going.Plaid.Environment environment)
        {
            var options = Options.Create(new PlaidOptions
            {
                ClientId = clientId,
                Secret = secret,
                Environment = environment
            });

            // IHttpClientFactory is optional — null is allowed
            _client = new PlaidClient(options, httpClientFactory: null);
        }

        public virtual PlaidClient Raw => _client;
    }
}
