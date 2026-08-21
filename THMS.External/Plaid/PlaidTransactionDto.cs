using System;
using System.Collections.Generic;
using System.Text;

namespace THMS.External.Plaid
{
    public class PlaidTransactionDto
    {
        public string TransactionId { get; set; } = "";
        public string AccountId { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime? Date { get; set; }
        public string Name { get; set; } = "";
        public string? Category { get; set; }
        public bool Pending { get; set; }
    }
}
