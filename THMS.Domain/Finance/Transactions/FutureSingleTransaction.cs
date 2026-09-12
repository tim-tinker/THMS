namespace THMS.Domain.Finance.Transactions
{
    public class FutureSingleTransaction : BaseSingleAccountTransaction
    {
        public bool IsUserCreated { get; set; }

        // Whether this forecast has been realized by a posted transaction
        public bool IsRealized { get; set; }

        // Link to the posted transaction that fulfilled it
        public Guid? PostedTransactionId { get; set; }

        public bool IsPlannedPayment { get; set; }
        public Guid? StatementId { get; set; }
        public Guid? PromotionalBalanceId { get; set; }
        public string? PlanningNote { get; set; }
    }
}
