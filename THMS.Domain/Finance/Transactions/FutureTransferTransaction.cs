namespace THMS.Domain.Finance.Transactions
{
    public class FutureTransferTransaction : BaseTransaction
    {
        public Guid FromAccountId { get; set; }
        public Guid ToAccountId { get; set; }

        public bool IsUserCreated { get; set; }
        public bool IsRealized { get; set; }
        public Guid? PostedFromTransactionId { get; set; }
        public Guid? PostedToTransactionId { get; set; }

        public bool IsPlannedPayment { get; set; }
        public Guid? StatementId { get; set; }
        public Guid? PromotionalBalanceId { get; set; }
        public string? PlanningNote { get; set; }

        public ExpectedOrigin Origin { get; set; } = ExpectedOrigin.Manual;
        public Guid? OriginId { get; set; }
        public ExpectedStatus Status { get; set; } = ExpectedStatus.Planned;

        public string DisplayStatus(DateTime? asOf = null) =>
            TransactionStatuses.ForExpected(Date, IsRealized, Status, asOf);
    }
}
