namespace THMS.Domain.Finance.Transactions
{
    public class PostedTransferTransaction : PostedTransaction
    {
        // The other side of the transfer
        public Guid RelatedPostedTransactionId { get; set; }

        // Direction relative to THIS account
        public TransferDirection Direction { get; set; }
    }

}
