namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents the attribution of EV charging energy for a specific timestamp.
    /// </summary>
    public class EnergyAttributionResult
    {
        public DateTime Timestamp { get; set; }

        public decimal EvChargeWh { get; set; }

        public decimal SolarWh { get; set; }
        public decimal BatteryWh { get; set; }
        public decimal GridWh { get; set; }

        /// <summary>
        /// True if attribution is incomplete due to missing solar vendor data.
        /// </summary>
        public bool IsPartial { get; set; }
    }
}
