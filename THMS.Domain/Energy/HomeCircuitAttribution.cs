namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents the attribution of home circuit energy for a specific timestamp.
    /// </summary>
    public class HomeCircuitAttribution
    {
        public DateTime Timestamp { get; set; }

        public decimal TotalWh { get; set; }

        public decimal SolarWh { get; set; }
        public decimal BatteryWh { get; set; }
        public decimal GridWh { get; set; }
    }
}
