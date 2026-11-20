using THMS.Domain.Assets;

namespace THMS.Domain.Finance.Planning
{
    public class PropertyTaxStatement : AccountStatement
    {
        public Guid AssetId { get; set; }
        public int TaxYear { get; set; }
        public AssessmentMethod AssessmentMethod { get; set; } = AssessmentMethod.ApplyHomesteadCap;
        public LevyMethod LevyMethod { get; set; } = LevyMethod.RatePerHundred;
        public decimal LandMarketValue { get; set; }
        public decimal ImprovementMarketValue { get; set; }
        public decimal TotalMarketValue { get; set; }
        public decimal HomesteadCap { get; set; }
        public decimal AssessmentRatioPercent { get; set; } = 100m;
        public decimal AssessedValue { get; set; }
        public Guid ValuationId { get; set; }
        public string Exemptions { get; set; } = "";
        public List<PropertyTaxJurisdictionLine> Jurisdictions { get; set; } = [];

        public override StatementType Type => StatementType.PropertyTax;
    }
}
