namespace THMS.Domain.Assets
{
    public abstract class Asset : BaseDomainModel
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime? AcquiredOn { get; set; }
        public decimal CostBasis { get; set; }
        public string HowAcquired { get; set; } = "";
        public string AcquisitionNotes { get; set; } = "";
    }
}
