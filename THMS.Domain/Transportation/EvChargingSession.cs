namespace THMS.Domain.Transportation
{
    public class EvChargingSession
    {
        public Guid Id { get; set; }

        // power data
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal KwhAdded { get; set; }
        public bool IsHomeCharging { get; set; }

        // Cost data
        public decimal? ChargingCost { get; set; }

        // FK to vehicle-specific enrichment (nullable until user provides it)
        public Guid? VehicleDataId { get; set; }
    }
}
