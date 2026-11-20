namespace THMS.Domain.Assets
{
    public sealed class PropertyAppraisal
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public DateTime AsOf { get; set; }
        public string Source { get; set; } = "";
        public Guid? SourceStatementId { get; set; }
        public decimal LandMarketValue { get; set; }
        public decimal ImprovementMarketValue { get; set; }
        public decimal TotalMarketValue { get; set; }
        public decimal HomesteadCap { get; set; }
        public decimal AssessmentRatioPercent { get; set; } = 100m;
        public decimal AssessedValue { get; set; }
        public Dictionary<string, Guid> ComponentIds { get; } = new(StringComparer.Ordinal);

        public string Label =>
            string.IsNullOrWhiteSpace(Source) ? AsOf.ToString("d") : $"{AsOf:d} — {Source}";

        public static IReadOnlyList<PropertyAppraisal> Group(IEnumerable<AssetValuation> rows)
        {
            var list = rows.ToList();
            var used = new HashSet<Guid>();
            var appraisals = new List<PropertyAppraisal>();

            void Take(IEnumerable<IGrouping<Guid, AssetValuation>> groups)
            {
                foreach (var group in groups)
                {
                    var members = group.Where(row => !used.Contains(row.Id)).ToList();
                    if (!members.Any(row => ComponentTypes.Contains(row.ValueType)))
                        continue;
                    appraisals.Add(From(group.Key, members));
                    foreach (var row in members)
                        used.Add(row.Id);
                }
            }

            Take(list.Where(row => row.AppraisalId != Guid.Empty).GroupBy(row => row.AppraisalId));
            Take(list.Where(row => row.SourceStatementId is Guid id && id != Guid.Empty).GroupBy(row => row.SourceStatementId!.Value));
            Take(list.Where(row => !used.Contains(row.Id) && ComponentTypes.Contains(row.ValueType))
                .GroupBy(row => row.Id));

            return appraisals.OrderByDescending(item => item.AsOf).ThenBy(item => item.Label).ToList();
        }

        private static PropertyAppraisal From(Guid id, IReadOnlyList<AssetValuation> rows)
        {
            var first = rows[0];
            var ratio = rows.Select(row => row.AssessmentRatioPercent).FirstOrDefault(value => value > 0m);
            var appraisal = new PropertyAppraisal
            {
                Id = id,
                AssetId = first.AssetId,
                AsOf = first.AsOf,
                Source = first.Source ?? "",
                SourceStatementId = first.SourceStatementId,
                AssessmentRatioPercent = ratio > 0m ? ratio : 100m,
                LandMarketValue = Amount(rows, ValuationTypes.LandMarket),
                ImprovementMarketValue = Amount(rows, ValuationTypes.ImprovementMarket),
                TotalMarketValue = Amount(rows, ValuationTypes.TotalMarket),
                HomesteadCap = Amount(rows, ValuationTypes.HomesteadCap),
                AssessedValue = Amount(rows, ValuationTypes.Assessed)
            };
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.ValueType))
                    appraisal.ComponentIds[row.ValueType] = row.Id;
            }

            return appraisal;
        }

        private static decimal Amount(IReadOnlyList<AssetValuation> rows, string type) =>
            rows.FirstOrDefault(row => row.ValueType == type)?.Amount ?? 0m;

        private static readonly HashSet<string> ComponentTypes = new(StringComparer.Ordinal)
        {
            ValuationTypes.LandMarket,
            ValuationTypes.ImprovementMarket,
            ValuationTypes.TotalMarket,
            ValuationTypes.HomesteadCap,
            ValuationTypes.Assessed
        };
    }
}
