namespace THMS.Domain.Finance.Transactions
{
    public class BaseTransaction : BaseDomainModel
    {
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }

        public Guid? CategoryId { get; set; }
        public string? Category { get; set; }

        public List<SplitTransactionRow> Splits { get; set; } = [];
        public bool HasSplits => Splits.Count > 0;

        public void ApplyCategory(ExpenseCategory? category)
        {
            if (category is null)
            {
                CategoryId = null;
                Category = null;
                return;
            }

            CategoryId = category.Id;
            Category = category.Name;
        }
    }
}
