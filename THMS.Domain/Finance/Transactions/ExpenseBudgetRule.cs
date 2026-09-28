namespace THMS.Domain.Finance.Transactions
{
    public class ExpenseBudgetRule : BaseDomainModel
    {
        public Guid AccountId { get; set; }
        public string BudgetName { get; set; } = "";
        public List<Guid> IncludedCategoryIds { get; set; } = [];
        public BudgetFrequency BudgetFrequency { get; set; } = BudgetFrequency.Monthly;
        public DateTime? PeriodStart { get; set; }
        public decimal DefaultBudgetAmount { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
