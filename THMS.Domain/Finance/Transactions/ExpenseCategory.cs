namespace THMS.Domain.Finance.Transactions
{
    public class ExpenseCategory : BaseDomainModel
    {
        public string Name { get; set; } = "";
        public Guid? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Null inherits the parent role. A root with no role resolves to Expense.
        /// </summary>
        public CategoryRole? Role { get; set; }
    }
}
