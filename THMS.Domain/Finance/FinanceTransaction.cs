using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.Domain.Finance
{
    public class FinanceTransaction
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
    }
}
