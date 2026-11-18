namespace THMS.Domain.Finance.Planning
{
    public class PropertyTaxJurisdictionLine
    {
        public string Name { get; set; } = "";
        public decimal ExemptionAmount { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal RatePerHundred { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
