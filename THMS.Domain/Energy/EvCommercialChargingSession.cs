namespace THMS.Domain.Energy
{
    public class EvCommercialChargingSession
    {
        public Guid Id { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal KwhAdded { get; set; }

        public decimal? ChargingCost { get; set; }

        // Optional vendor metadata
        public string? VendorSessionId { get; set; }
        public string? Location { get; set; }
    }
}
