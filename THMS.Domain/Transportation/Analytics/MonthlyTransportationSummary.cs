namespace THMS.Domain.Transportation.Analytics
{
    public class MonthlyTransportationSummary
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public Guid VehicleId { get; set; }

        // Mileage
        public decimal MilesDriven { get; set; }

        // Costs
        public decimal FuelCost { get; set; }
        public decimal ChargeCost { get; set; }
        public decimal MaintenanceCost { get; set; }

        // Derived metrics
        public decimal CostPerMile =>
            MilesDriven > 0 ? (FuelCost + ChargeCost + MaintenanceCost) / MilesDriven : 0;

        public bool IsPartial { get; set; }
    }
}
