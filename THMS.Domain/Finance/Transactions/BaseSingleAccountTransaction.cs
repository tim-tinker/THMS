namespace THMS.Domain.Finance.Transactions
{
    public class BaseSingleAccountTransaction : BaseTransaction
    {
        public Guid AccountId { get; set; }
    }
}
