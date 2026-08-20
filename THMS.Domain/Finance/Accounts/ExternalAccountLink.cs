using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Finance.Accounts
{
    public class ExternalAccountLink
    {
        public string Provider { get; set; } = "Plaid";
        public string ItemId { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string PlaidAccountId { get; set; } = "";
        public string InstitutionId { get; set; } = "";
        public string AccountMask { get; set; } = "";
    }
}
