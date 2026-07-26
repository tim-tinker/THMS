namespace THMS.Domain.Energy
{
    public class EvCircuitReading
    {
        public DateTime Timestamp { get; set; }
        public decimal WattHours { get; set; }
        public string CircuitId { get; set; } = string.Empty;
    }
}
