using THMS.Configuration;

namespace THMS.External.Plaid
{
    public class PlaidSandboxServiceClient : PlaidServiceClient
    {
        public PlaidSandboxServiceClient()
            : base(AppConfig.Instance.PlaidClientId, 
                  AppConfig.Instance.Secrets.GetSandboxSecret(), 
                  Going.Plaid.Environment.Sandbox)
        {
        }
    }
}
