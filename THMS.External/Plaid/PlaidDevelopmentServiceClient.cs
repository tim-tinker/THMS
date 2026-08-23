using THMS.Configuration;

namespace THMS.External.Plaid
{
    public class PlaidDevelopmentServiceClient : PlaidServiceClient
    {
        public PlaidDevelopmentServiceClient()
        : base(AppConfig.Instance.PlaidClientId,
               AppConfig.Instance.Secrets.GetSandboxSecret(),
               Going.Plaid.Environment.Development)
        {
        }
    }
}
