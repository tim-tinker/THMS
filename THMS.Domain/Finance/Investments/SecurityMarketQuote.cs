namespace THMS.Domain.Finance.Investments
{
    public sealed class SecurityMarketQuote
    {
        public string Symbol { get; set; } = "";
        public decimal UnitValue { get; set; }
        public DateTime AsOf { get; set; }
        public MarketQuoteOrigin Origin { get; set; }

        public SecurityMarketQuote Copy() => new()
        {
            Symbol = Symbol,
            UnitValue = UnitValue,
            AsOf = AsOf,
            Origin = Origin
        };

        public static bool SameObservation(SecurityMarketQuote left, SecurityMarketQuote right) =>
            string.Equals(left.Symbol, right.Symbol, StringComparison.OrdinalIgnoreCase)
            && left.AsOf.Date == right.AsOf.Date
            && left.Origin == right.Origin;

        public static decimal? ForDate(IEnumerable<SecurityMarketQuote> quotes, string symbol, DateTime date)
        {
            decimal? imported = null;
            decimal? queried = null;
            foreach (var quote in quotes)
            {
                if (!string.Equals(quote.Symbol, symbol, StringComparison.OrdinalIgnoreCase) || quote.AsOf.Date != date.Date)
                    continue;
                if (quote.Origin == MarketQuoteOrigin.Import)
                    imported = quote.UnitValue;
                else
                    queried = quote.UnitValue;
            }

            return imported ?? queried;
        }
    }
}
