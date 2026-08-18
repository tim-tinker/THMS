namespace THMS.Domain.Finance.Transactions
{
    public class TransactionCategory : BaseDomainModel
    {
        public string Name { get; set; }
        public string? ParentCategory { get; set; }
    }
}
