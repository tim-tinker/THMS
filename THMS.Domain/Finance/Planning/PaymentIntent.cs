namespace THMS.Domain.Finance.Planning
{
    public enum PaymentIntentSource
    {
        Statement = 0,
        RecurringSingle = 1,
        RecurringTransfer = 2,
        Manual = 3
    }

    public enum PaymentIntentStatus
    {
        Scheduled = 0,
        Matched = 1
    }

    public class PaymentIntent : BaseDomainModel
    {
        public PaymentIntentSource Source { get; set; }
        public Guid? SourceId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public Guid FundingAccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PayDate { get; set; }
        public PaymentIntentStatus Status { get; set; } = PaymentIntentStatus.Scheduled;
        public Guid? MatchedPostedTransactionId { get; set; }
        public Guid? MatchedCounterpartTransactionId { get; set; }
        public string? Description { get; set; }
    }
}
