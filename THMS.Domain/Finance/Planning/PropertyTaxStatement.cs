namespace THMS.Domain.Finance.Planning
{
    public class PropertyTaxStatement : AccountStatement
    {
        public Guid AssetId { get; set; }
        public int TaxYear { get; set; }
        public decimal LandMarketValue { get; set; }
        public decimal ImprovementMarketValue { get; set; }
        public decimal TotalMarketValue { get; set; }
        public decimal HomesteadCap { get; set; }
        public decimal AssessedValue { get; set; }
        public string Exemptions { get; set; } = "";
        public List<PropertyTaxJurisdictionLine> Jurisdictions { get; set; } = [];

        public override StatementType Type => StatementType.PropertyTax;
    }
}
