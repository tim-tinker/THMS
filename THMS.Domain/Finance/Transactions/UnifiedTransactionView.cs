namespace THMS.Domain.Finance.Transactions
{
    public class UnifiedTransactionView
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }

        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }

        public string? Category { get; set; }
        public string Type { get; set; } // Posted, Future, RecurringRule, Transfer, FutureTransfer
    }
}
