namespace THMS.Domain.Transportation
{
    public class EvChargingSessionVehicleData
    {
        public Guid Id { get; set; }

        // Vehicle identity
        public Guid VehicleId { get; set; }

        // Vehicle-specific charging details
        public decimal StartSocPercent { get; set; }
        public decimal EndSocPercent { get; set; }
        public decimal OdometerMiles { get; set; }
    }
}
