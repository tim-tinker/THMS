namespace THMS.Domain.Finance.Transactions
{
    public class PostedTransferTransaction : PostedTransaction
    {
        // The other side of the transfer
        public Guid RelatedPostedTransactionId { get; set; }

        // Direction relative to THIS account
        public TransferDirection Direction { get; set; }

        public PostedTransferTransaction()
        { }

        public PostedTransferTransaction(
            PostedTransaction original,
            Guid relatedId,
            TransferDirection direction)
        {
            // Copy all PostedTransaction fields
            this.Id = original.Id;
            this.AccountId = original.AccountId;
            this.Amount = original.Amount;
            this.Date = original.Date;
            this.Description = original.Description;
            this.Category = original.Category;

            // Transfer-specific fields
            this.RelatedPostedTransactionId = relatedId;
            this.Direction = direction;
        }
    }
}
