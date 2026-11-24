namespace THMS.Logic.ViewModels.Finance
{
    public sealed class NetWorthMonthRow
    {
        public DateTime Month { get; init; }
        public string Label { get; init; } = "";
        public decimal NetWorth { get; init; }
        public decimal? Change { get; init; }
        public decimal Cash { get; init; }
        public decimal Investments { get; init; }
        public decimal Property { get; init; }
        public decimal Vehicles { get; init; }
        public decimal Personal { get; init; }
        public decimal Debt { get; init; }
    }

    public sealed class NetWorthLine
    {
        public string Group { get; init; } = "";
        public string Name { get; init; } = "";
        public decimal? Amount { get; init; }
        public string Source { get; init; } = "";
    }

    public sealed class NetWorthSnapshot
    {
        public const string MethodNote =
            "Net worth is cash, investment market value, and other asset valuations, minus debt. Debt is negative. A leased asset is what you keep: its value, minus the remaining payments, minus the residual. When that is negative, the vehicle is debt. The current month uses today's bank, credit, and loan balances. Earlier months use the latest statement on or before month-end, plus activity posted after that statement. Accounts with no earlier statement are left out of those months. Investment values use the latest price on or before month-end. An asset keeps the latest total market value on or before that day, then assessed value, then market value.";

        public decimal NetWorth { get; init; }
        public decimal Cash { get; init; }
        public decimal Investments { get; init; }
        public decimal OtherAssets { get; init; }
        public decimal Debt { get; init; }
        public string Note { get; init; } = MethodNote;
        public IReadOnlyList<NetWorthMonthRow> Months { get; init; } = [];
        public IReadOnlyList<NetWorthLine> Lines { get; init; } = [];
    }
}
