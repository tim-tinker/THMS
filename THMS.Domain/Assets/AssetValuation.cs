namespace THMS.Domain.Assets
{
    public class AssetValuation : BaseDomainModel
    {
        public Guid AssetId { get; set; }
        public string ValueType { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime AsOf { get; set; }
        public string Source { get; set; } = "";
        public Guid AppraisalId { get; set; }
        public decimal AssessmentRatioPercent { get; set; } = 100m;
        public Guid? SourceStatementId { get; set; }
    }
}
