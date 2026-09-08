namespace THMS.Logic.ViewModels.Finance
{
    public class UnifiedBudgetView
    {
        public Guid Id { get; set; }
        public string BudgetName { get; set; } = "";
        public string Frequency { get; set; } = "";
        public decimal? StartingBalance { get; set; }
        public decimal? PeriodBudget { get; set; }
        public decimal? Actual { get; set; }
        public decimal? Remaining { get; set; }
        public decimal? EndingBalance { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = "";
    }
}
