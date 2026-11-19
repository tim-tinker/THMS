namespace THMS.Domain.Assets
{
    public class PropertyAsset : Asset
    {
        public string ParcelAddress { get; set; } = "";
        public string CadNumber { get; set; } = "";
        public string TaxAccountNumber { get; set; } = "";
        public string LegalDescription { get; set; } = "";
        public decimal LegalAcres { get; set; }
        public string RollCode { get; set; } = "";
        public AssessmentMethod AssessmentMethod { get; set; } = AssessmentMethod.ApplyHomesteadCap;
        public LevyMethod LevyMethod { get; set; } = LevyMethod.RatePerHundred;
    }
}
