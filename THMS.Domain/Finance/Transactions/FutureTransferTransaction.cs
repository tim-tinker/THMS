namespace THMS.Domain.Finance.Transactions
{
    public class FutureTransferTransaction : BaseTransaction
    {
        public Guid FromAccountId { get; set; }
        public Guid ToAccountId { get; set; }

        public bool IsRealized { get; set; }
        public Guid? PostedFromTransactionId { get; set; }
        public Guid? PostedToTransactionId { get; set; }
    }
}
