namespace THMS.Domain.Assets
{
    /// <summary>
    /// Which recorded value net worth uses.
    /// Property keeps the latest total market value, then assessed, then market.
    /// A vehicle you keep is worth its private-party value, then a market value,
    /// then trade-in, then retail. Each source contributes its latest figure of
    /// that kind, and those figures are combined as the median.
    /// </summary>
    public static class ValuationPolicy
    {
        private static readonly string[] PropertyKinds =
        [
            ValuationTypes.TotalMarket,
            ValuationTypes.Assessed,
            ValuationTypes.Market
        ];

        private static readonly string[] VehicleKinds =
        [
            ValuationTypes.PrivateParty,
            ValuationTypes.Market,
            ValuationTypes.TradeIn,
            ValuationTypes.Retail,
            ValuationTypes.TotalMarket,
            ValuationTypes.Assessed
        ];

        private static readonly string[] OtherKinds =
        [
            ValuationTypes.TotalMarket,
            ValuationTypes.Assessed,
            ValuationTypes.Market,
            ValuationTypes.Replacement,
            ValuationTypes.Appraised
        ];

        public static bool Counts(Asset asset, string valueType)
        {
            foreach (var kind in Kinds(asset))
            {
                if (kind == valueType)
                    return true;
            }

            return false;
        }

        public static ChosenValuation? Choose(Asset asset, IEnumerable<AssetValuation> valuations, DateTime through)
        {
            var day = through.Date;
            foreach (var type in Kinds(asset))
            {
                var ofType = valuations.Where(row => row.ValueType == type && row.AsOf.Date <= day);
                if (asset is VehicleAsset)
                {
                    var figures = LatestOfEachSource(ofType);
                    if (figures.Count == 0)
                        continue;
                    return new ChosenValuation(type, Median(figures.Select(row => row.Amount)), figures);
                }

                var latest = ofType.OrderByDescending(row => row.AsOf).FirstOrDefault();
                if (latest is null)
                    continue;
                return new ChosenValuation(type, latest.Amount, [latest]);
            }

            return null;
        }

        private static IReadOnlyList<string> Kinds(Asset asset) => asset switch
        {
            PropertyAsset => PropertyKinds,
            VehicleAsset => VehicleKinds,
            _ => OtherKinds
        };

        private static List<AssetValuation> LatestOfEachSource(IEnumerable<AssetValuation> rows) =>
            rows.GroupBy(row => (row.Source ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(row => row.AsOf)
                    .ThenByDescending(row => row.ModifiedOn)
                    .ThenByDescending(row => row.Id)
                    .First())
                .ToList();

        private static decimal Median(IEnumerable<decimal> amounts)
        {
            var ordered = amounts.OrderBy(amount => amount).ToList();
            var middle = ordered.Count / 2;
            if (ordered.Count % 2 == 1)
                return ordered[middle];
            return (ordered[middle - 1] + ordered[middle]) / 2m;
        }
    }

    public sealed class ChosenValuation
    {
        public ChosenValuation(string valueType, decimal amount, IReadOnlyList<AssetValuation> figures)
        {
            ValueType = valueType;
            Amount = amount;
            Figures = figures;
        }

        public string ValueType { get; }
        public decimal Amount { get; }
        public IReadOnlyList<AssetValuation> Figures { get; }
        public DateTime AsOf => Figures.Max(row => row.AsOf);

        public string Describe(DateTime asOf)
        {
            var text = Figures.Count == 1
                ? $"{ValueType} as of {AsOf:d}"
                : $"{ValueType} median of {Figures.Count} as of {AsOf:d}";
            if (AsOf.Date < new DateTime(asOf.Year, asOf.Month, 1))
                text += ", carried forward";
            if (Figures.Count < 2)
                return text;

            var parts = Figures
                .OrderBy(row => SourceName(row), StringComparer.OrdinalIgnoreCase)
                .Select(row => $"{SourceName(row)} {row.Amount:c2}");
            return text + ": " + string.Join(", ", parts);
        }

        private static string SourceName(AssetValuation row) =>
            string.IsNullOrWhiteSpace(row.Source) ? "Unnamed" : row.Source.Trim();
    }
}
