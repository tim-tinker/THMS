using THMS.Configuration;

namespace THMS.External.Plaid
{
    public class PlaidProductionServiceClient : PlaidServiceClient
    {
        public PlaidProductionServiceClient()
        : base(AppConfig.Instance.PlaidClientId,
               AppConfig.Instance.Secrets.GetProductionSecret(),
               Going.Plaid.Environment.Production)
        {
        }
    }
}
