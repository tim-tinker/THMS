using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.External.Plaid
{
    public class PlaidAccountDto
    {
        public string PlaidAccountId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Mask { get; set; } = "";
        public string Type { get; set; } = "";
        public string Subtype { get; set; } = "";
        public decimal? Available { get; set; }
        public decimal? Current { get; set; }
        public decimal? Limit { get; set; }
    }
}
