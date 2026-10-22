namespace THMS.Logic.ViewModels.Finance
{
    public class CategoryBudgetRow
    {
        public Guid CategoryId { get; init; }
        public Guid? BudgetRuleId { get; init; }
        public string Name { get; init; } = "";
        public string Active { get; init; } = "";
        public decimal? Remaining { get; init; }
        public DateTime? PeriodStart { get; init; }
        public DateTime? PeriodEnd { get; init; }
        public decimal? Recommended { get; init; }
        public string Status { get; init; } = "";
        public string Role { get; init; } = "";
        public bool HasBudget => BudgetRuleId is Guid id && id != Guid.Empty;
    }

    public class CategoryBudgetPeriodRow
    {
        public Guid HistoryId { get; init; }
        public Guid BudgetRuleId { get; init; }
        public string Period { get; init; } = "";
        public DateTime? PeriodStart { get; init; }
        public DateTime? PeriodEnd { get; init; }
        public decimal? Starting { get; init; }
        public decimal? BudgetAmount { get; init; }
        public decimal? Actual { get; init; }
        public decimal? Remaining { get; init; }
        public decimal? Recommended { get; init; }
        public string Status { get; init; } = "";
        public bool IsClosed { get; init; }
        public bool IsHistory { get; init; }

        public bool Contains(DateTime date)
        {
            var day = date.Date;
            if (PeriodStart is DateTime start && day < start.Date)
                return false;
            if (PeriodEnd is DateTime end && day > end.Date)
                return false;
            return true;
        }
    }
}
