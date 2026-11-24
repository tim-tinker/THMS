namespace THMS.Domain.Assets
{
    public static class ValuationTypes
    {
        public const string LandMarket = "Land market";
        public const string ImprovementMarket = "Improvement market";
        public const string TotalMarket = "Total market";
        public const string HomesteadCap = "Homestead cap";
        public const string Assessed = "Assessed";
        public const string Market = "Market";
        public const string CostBasis = "Cost basis";
        public const string PrivateParty = "Private party";
        public const string TradeIn = "Trade-in";
        public const string Retail = "Retail";
        public const string Replacement = "Replacement";
        public const string Appraised = "Appraised";

        public static readonly string[] Vehicle =
        [
            Market,
            PrivateParty,
            TradeIn,
            Retail
        ];

        public static readonly string[] Personal =
        [
            Market,
            Replacement,
            Appraised
        ];

        public static readonly string[] All =
        [
            LandMarket,
            ImprovementMarket,
            TotalMarket,
            HomesteadCap,
            Assessed,
            Market,
            CostBasis
        ];

        public const string TaxStatementSource = "Property tax statement";
    }
}
