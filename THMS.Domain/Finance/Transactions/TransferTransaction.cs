namespace THMS.Domain.Finance.Transactions
{
    public class TransferTransaction : BaseTransaction
    {
        public Guid FromAccountId { get; set; }
        public Guid ToAccountId { get; set; }
    }
}
