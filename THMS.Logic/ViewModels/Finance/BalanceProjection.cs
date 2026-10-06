namespace THMS.Logic.ViewModels.Finance
{
    public sealed class BalanceProjection
    {
        public DateTime AsOf { get; init; }
        public DateTime Through { get; init; }
        public decimal OpeningBalance { get; init; }
        public IReadOnlyList<BalanceProjectionRow> Rows { get; init; } = [];

        public static BalanceProjection Empty(DateTime asOf) => new()
        {
            AsOf = asOf.Date,
            Through = asOf.Date.AddMonths(1)
        };
    }

    public sealed class BalanceProjectionRow
    {
        public DateTime Date { get; init; }
        public string Description { get; init; } = "";
        public decimal Amount { get; init; }
        public decimal Balance { get; init; }
    }
}
