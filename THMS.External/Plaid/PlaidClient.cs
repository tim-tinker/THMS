namespace THMS.External.Plaid
{
    public class PlaidClient
    {
        private readonly Going.Plaid.PlaidClient _client;

        public PlaidClient(string clientId, string secret, Going.Plaid.Environment env)
        {
            _client = new Going.Plaid.PlaidClient(env, clientId: clientId, secret: secret);
        }

        public virtual Going.Plaid.PlaidClient Raw => _client;
    }
}
