namespace THMS.Domain.Finance.Transactions
{
    public class FutureSingleTransaction : BaseSingleAccountTransaction
    {
        // Whether this forecast has been realized by a posted transaction
        public bool IsRealized { get; set; }

        // Link to the posted transaction that fulfilled it
        public Guid? PostedTransactionId { get; set; }
    }
}
