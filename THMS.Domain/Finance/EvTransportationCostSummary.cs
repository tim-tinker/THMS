namespace THMS.Domain.Finance
{
    public class EvTransportationCostSummary : TransportationCostSummaryBase
    {
        public decimal TotalMiles { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CostPerMile { get; set; }

        public decimal HomeChargingCost { get; set; }
        public decimal CommercialChargingCost { get; set; }
    }
}
