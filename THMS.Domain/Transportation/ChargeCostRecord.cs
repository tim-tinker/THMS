namespace THMS.Domain.Transportation
{
    public class ChargeCostRecord : BaseDomainModel
    {
        public Guid VehicleId { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal EnergyWh { get; set; }
        public decimal Cost { get; set; }
        public string? Location { get; set; }
    }
}
