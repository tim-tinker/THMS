using THMS.Domain.Finance.Investments;

namespace THMS.Logic.Finance.Investments
{
    public sealed class InvestmentMonth
    {
        public DateTime Month { get; init; }
        public decimal Cash { get; init; }
        public decimal MarketValue { get; init; }
        public decimal Value => Cash + MarketValue;
        public decimal Income { get; init; }
        public decimal EstimatedAnnualIncome { get; init; }
        public int Holdings { get; init; }
        public int CarriedForward { get; init; }
        public int Unpriced { get; init; }
    }

    /// <summary>
    /// Lines prices and dividend pay dates up by calendar month. A month-end value uses each
    /// holding's latest price on or before that day, so securities do not need a quote on the same date.
    /// </summary>
    public static class InvestmentMonthlySummary
    {
        public static IReadOnlyList<InvestmentMonth> Build(InvestmentBookState state, DateTime asOf)
        {
            ArgumentNullException.ThrowIfNull(state);
            asOf = asOf.Date;
            var start = Start(state);
            if (start is null || start.Value.Date > asOf)
                return [];

            var months = new List<InvestmentMonth>();
            var first = new DateTime(start.Value.Year, start.Value.Month, 1);
            var last = new DateTime(asOf.Year, asOf.Month, 1);
            for (var month = first; month <= last; month = month.AddMonths(1))
            {
                var end = month.AddMonths(1).AddDays(-1);
                var through = end > asOf ? asOf : end;
                months.Add(Summarize(state, month, through));
            }

            return months;
        }

        private static InvestmentMonth Summarize(InvestmentBookState state, DateTime month, DateTime through)
        {
            var (cash, holdings) = InvestmentBookkeeper.HoldingsAsOf(state, through);
            decimal market = 0;
            var carried = 0;
            var unpriced = 0;
            foreach (var holding in holdings)
            {
                var price = InvestmentBookkeeper.PriceAsOf(state, holding.Symbol, through);
                if (price.Price is not decimal unit)
                {
                    unpriced++;
                    continue;
                }

                market += holding.Shares * unit;
                if (price.AsOf is DateTime quoted && quoted.Date < month)
                    carried++;
            }

            return new InvestmentMonth
            {
                Month = month,
                Cash = cash,
                MarketValue = market,
                Income = IncomeReceived(state, month, through),
                EstimatedAnnualIncome = InvestmentBookkeeper.EstimatedAnnualIncomeAsOf(state, through),
                Holdings = holdings.Count,
                CarriedForward = carried,
                Unpriced = unpriced
            };
        }

        private static decimal IncomeReceived(InvestmentBookState state, DateTime month, DateTime through)
        {
            decimal total = 0;
            foreach (var action in state.Actions)
            {
                if (action.Status != InvestmentActionStatus.Applied)
                    continue;
                if (action.Kind is not (InvestmentActionKind.Dividend or InvestmentActionKind.Interest or InvestmentActionKind.Income))
                    continue;
                var date = action.RunDate.Date;
                if (date < month || date > through)
                    continue;
                total += action.Amount ?? 0;
            }

            return total;
        }

        private static DateTime? Start(InvestmentBookState state)
        {
            DateTime? start = state.OpenedOn?.Date;
            foreach (var action in state.Actions)
            {
                if (action.Status != InvestmentActionStatus.Applied)
                    continue;
                var date = action.RunDate.Date;
                if (start is null || date < start)
                    start = date;
            }

            return start;
        }
    }
}
