namespace THMS.Domain.Energy
{
    public class HomeCircuitReading
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal KiloWattHours { get; set; }
        public string? CircuitId { get; set; }
    }
}
