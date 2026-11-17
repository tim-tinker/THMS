namespace THMS.Logic.ViewModels.Finance
{
    public sealed class InvestmentDashboardMonthRow
    {
        public DateTime Month { get; init; }
        public string Label { get; init; } = "";
        public decimal Value { get; init; }
        public decimal? Change { get; init; }
        public decimal Income { get; init; }
        public decimal EstimatedAnnualIncome { get; init; }
        public string Prices { get; init; } = "";
    }

    public sealed class InvestmentDashboardSnapshot
    {
        public const string MethodNote =
            "Each month uses the latest price for every holding on or before month end, and carries that price forward until a newer quote arrives. The current month runs through today. Income is the dividends, interest, and capital gains posted during the month. Estimated annual income is the shares held then, times the latest annual income per share known on or before that day. Value includes contributions and withdrawals.";

        public decimal TotalValue { get; init; }
        public decimal IncomeThisMonth { get; init; }
        public decimal IncomeLastTwelveMonths { get; init; }
        public decimal EstimatedAnnualIncome { get; init; }
        public bool PricesIncomplete { get; init; }
        public string Note { get; init; } = MethodNote;
        public IReadOnlyList<InvestmentDashboardMonthRow> Months { get; init; } = [];
    }
}
