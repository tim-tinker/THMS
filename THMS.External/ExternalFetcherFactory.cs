using THMS.Configuration;
using THMS.External.Plaid;

namespace THMS.External
{
    public class ExternalFetcherFactory
    {
        private PlaidServiceClient? _client;
        protected PlaidServiceClient Client => _client ??= CreateClient();

        private IExternalAccountFetcher? _accountFetcher;
        private IExternalTransactionFetcher? _transactionFetcher;

        public IExternalAccountFetcher GetAccountFetcher()
        {
            return _accountFetcher ??= CreateAccountFetcher();
        }

        public IExternalTransactionFetcher GetTransactionFetcher()
        {
            return _transactionFetcher ??= CreateTransactionFetcher();
        }

        private IExternalAccountFetcher CreateAccountFetcher()
        {
            return new PlaidAccountFetcher(Client);
        }

        private IExternalTransactionFetcher CreateTransactionFetcher()
        {
            return new PlaidTransactionFetcher(Client);
        }

        private PlaidServiceClient CreateClient()
        {
            PlaidServiceClient client;
            switch (AppConfig.Instance.PlaidEnvironment)
            {
                case "Production":
                    client = new PlaidProductionServiceClient();
                    break;
                case "Sandbox":
                    client = new PlaidSandboxServiceClient();
                    break;
                case "Development":
                    client = new PlaidDevelopmentServiceClient();
                    break;
                default:
                    client = new PlaidDevelopmentServiceClient();
                    break;
            }
            return client;
        }
    }
}
