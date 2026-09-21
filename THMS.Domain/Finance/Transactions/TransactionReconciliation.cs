namespace THMS.Domain.Finance.Transactions
{
    public class TransactionReconciliation : BaseDomainModel
    {
        public Guid ImportedTransactionId { get; set; }
        public Guid? ExpectedTransactionId { get; set; }
        public Guid? RecommendedExpectedId { get; set; }
        public DateTime AcceptedOn { get; set; } = DateTime.UtcNow;
        public DateTime? RuleLastOccurrenceBefore { get; set; }
        public DateTime? RuleNextOccurrenceBefore { get; set; }
    }
}
