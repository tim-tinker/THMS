namespace THMS.Logic.ViewModels.Finance
{
    public class UnifiedTransactionView
    {
        public const string PostedType = "Posted";
        public const string PostedTransferType = "PostedTransfer";
        public const string FutureType = "Future";
        public const string FutureTransferType = "FutureTransfer";
        public const string ForecastType = "Forecast";
        public const string ForecastTransferType = "ForecastTransfer";
        public const string ForecastBudgetType = "ForecastBudget";
        public const string RecurringRuleType = "RecurringRule";
        public const string RecurringTransferRuleType = "RecurringTransferRule";

        public Guid Id { get; set; }
        public Guid AccountId { get; set; }

        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }

        public Guid? CategoryId { get; set; }
        public string? Category { get; set; }
        public string Type { get; set; } = "";

        public decimal? ForecastBalance { get; set; }

        public bool IsForecasted =>
            Type is ForecastType or ForecastTransferType or ForecastBudgetType;

        public bool IsRecurringRule =>
            Type is RecurringRuleType or RecurringTransferRuleType;

        public static IOrderedEnumerable<UnifiedTransactionView> OrderForDisplay(
            IEnumerable<UnifiedTransactionView> items) =>
            items.OrderByDescending(t => t.Date)
                .ThenBy(t => t.Amount)
                .ThenByDescending(t => t.Id);

        public static IOrderedEnumerable<UnifiedTransactionView> OrderForRunningBalance(
            IEnumerable<UnifiedTransactionView> items) =>
            items.OrderBy(t => t.Date)
                .ThenByDescending(t => t.Amount)
                .ThenBy(t => t.Id);
    }
}
