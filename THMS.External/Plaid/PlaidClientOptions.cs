using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.External.Plaid
{
    public class PlaidClientOptions
    {
        public string? ClientId { get; set; }
        public string? Secret { get; set; }
        public string? BaseUrl { get; set; }
    }
}
