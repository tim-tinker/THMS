namespace THMS.Domain.Finance.Transactions
{
    public class ExpenseCategory : BaseDomainModel
    {
        public string Name { get; set; } = "";
        public Guid? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
    }
}
