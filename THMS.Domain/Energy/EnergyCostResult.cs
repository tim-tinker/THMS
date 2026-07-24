namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents cost attribution for a specific timestamp.
    /// </summary>
    public class EnergyCostResult
    {
        public DateTime Timestamp { get; set; }

        public decimal EvChargingWh { get; set; }

        public decimal SolarAvoidedCost { get; set; }
        public decimal BatteryValue { get; set; }
        public decimal GridCost { get; set; }

        public decimal CommercialChargingCost { get; set; }

        /// <summary>
        /// True if cost attribution is incomplete due to missing billing data.
        /// </summary>
        public bool IsPartial { get; set; }
    }
}
