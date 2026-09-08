namespace THMS.Logic.ViewModels.Finance
{
    public class FinanceDashboardSnapshot
    {
        public decimal BankBalance { get; init; }
        public decimal CreditOwed { get; init; }
        public decimal LoanPrincipal { get; init; }
        public decimal InvestmentCash { get; init; }
        public decimal NetLiquid { get; init; }
        public decimal NetPosition { get; init; }

        public List<FinanceDashboardBudgetRow> Budgets { get; init; } = [];
        public List<FinanceDashboardPaymentRow> UpcomingPayments { get; init; } = [];
        public List<string> Alerts { get; init; } = [];
        public List<FinanceDashboardCategorySlice> CategorySlices { get; init; } = [];
        public List<FinanceDashboardMonthlyPoint> MonthlyTrend { get; init; } = [];
        public List<UnifiedTransactionView> RecentPosted { get; init; } = [];
        public List<UnifiedTransactionView> UpcomingForecast { get; init; } = [];
        public int UncategorizedCount { get; init; }
    }

    public class FinanceDashboardBudgetRow
    {
        public Guid RuleId { get; init; }
        public string AccountName { get; init; } = "";
        public string BudgetName { get; init; } = "";
        public string Frequency { get; init; } = "";
        public decimal Remaining { get; init; }
        public decimal Ending { get; init; }
        public decimal Recommended { get; init; }
        public string Status { get; init; } = "";
    }

    public class FinanceDashboardPaymentRow
    {
        public DateTime Date { get; init; }
        public string AccountName { get; init; } = "";
        public string Description { get; init; } = "";
        public decimal? Amount { get; init; }
    }

    public class FinanceDashboardCategorySlice
    {
        public string Name { get; init; } = "";
        public decimal Amount { get; init; }
    }

    public class FinanceDashboardMonthlyPoint
    {
        public DateTime Month { get; init; }
        public string Label { get; init; } = "";
        public decimal Spending { get; init; }
        public decimal Income { get; init; }
    }
}
