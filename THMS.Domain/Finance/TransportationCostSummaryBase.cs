namespace THMS.Domain.Finance
{
    public abstract class TransportationCostSummaryBase
    {
        public Guid VehicleId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
