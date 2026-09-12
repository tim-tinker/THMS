namespace THMS.Domain.Finance.Planning
{
    public abstract class AccountStatement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }

        public DateTime StatementDate { get; set; }
        public DateTime DueDate { get; set; }

        public decimal AmountDue { get; set; }
        public decimal MinimumPayment { get; set; }

        public string? Notes { get; set; }

        public abstract StatementType Type { get; }
    }
}
