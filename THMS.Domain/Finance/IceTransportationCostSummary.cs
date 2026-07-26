namespace THMS.Domain.Finance
{
    public class IceTransportationCostSummary : TransportationCostSummaryBase
    {
        public decimal TotalMiles { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CostPerMile { get; set; }

        public decimal FuelCost { get; set; }
    }
}
