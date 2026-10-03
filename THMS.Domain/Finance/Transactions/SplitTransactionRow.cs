namespace THMS.Domain.Finance.Transactions
{
    public class SplitTransactionRow
    {
        public Guid Id { get; set; }
        public Guid ParentTransactionId { get; set; }

        public decimal Amount { get; set; }
        public Guid? CategoryId { get; set; }
        public string? Category { get; set; }

        public SplitType Type { get; set; }

        public Guid? TransferAccountId { get; set; }
        public Guid? FromAccountId { get; set; }
        public Guid? ToAccountId { get; set; }
        public string? Notes { get; set; }

        public SplitTransactionRow Clone() =>
            new()
            {
                Id = Id,
                ParentTransactionId = ParentTransactionId,
                Amount = Amount,
                CategoryId = CategoryId,
                Category = Category,
                Type = Type,
                TransferAccountId = TransferAccountId,
                FromAccountId = FromAccountId,
                ToAccountId = ToAccountId,
                Notes = Notes
            };
    }
}
