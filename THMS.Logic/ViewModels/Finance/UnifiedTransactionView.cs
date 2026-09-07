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

        public Guid Id { get; set; }
        public Guid AccountId { get; set; }

        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }

        public string? Category { get; set; }
        public string Type { get; set; } = "";

        public decimal? ForecastBalance { get; set; }

        public bool IsForecasted =>
            Type is ForecastType or ForecastTransferType or ForecastBudgetType;
    }
}
