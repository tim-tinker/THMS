namespace THMS.Domain.Finance.Transactions
{
    public class PostedTransaction : BaseSingleAccountTransaction
    {
        public string? PlaidCategory { get; set; }
    }
}
