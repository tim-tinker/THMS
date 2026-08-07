namespace THMS.Domain.Finance
{
    public class EvTransportationCostSummary : TransportationCostSummaryBase
    {
        public decimal TotalMiles { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CostPerMile { get; set; }

        public decimal HomeChargeCost { get; set; }
        public decimal CommercialChargeCost { get; set; }
    }
}
