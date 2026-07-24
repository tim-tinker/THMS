namespace THMS.Domain.Transportation
{
    public class ChargingCostRecord : BaseDomainModel
    {
        public Guid VehicleId { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal EnergyWh { get; set; }
        public decimal Cost { get; set; }
        public string? Location { get; set; }
    }
}
