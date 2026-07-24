namespace THMS.Domain.Transportation
{
    public class MileageRecord : BaseDomainModel
    {
        public Guid VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal OdometerMiles { get; set; }
        public string? Notes { get; set; }
    }
}
