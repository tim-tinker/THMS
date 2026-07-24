namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents EV charging load for a specific timestamp.
    /// This is raw energy data (Wh), not cost, not attribution.
    /// </summary>
    public class EvChargingInterval
    {
        /// <summary>
        /// The timestamp representing the start of the interval.
        /// Normalized to 30-minute buckets by the ingestion pipeline.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Energy consumed by the EV during this interval (in Wh).
        /// This may come from SPAN (home charging) or commercial charging.
        /// </summary>
        public decimal EvChargingWh { get; set; }

        /// <summary>
        /// Optional cost for commercial charging.
        /// Home charging cost is computed later using BillingCostInterval.
        /// </summary>
        public decimal? CommercialChargingCost { get; set; }

        /// <summary>
        /// Indicates whether this interval came from a commercial charger.
        /// Helps attribution and cost engines distinguish sources.
        /// </summary>
        public bool IsCommercialCharging => CommercialChargingCost.HasValue;
    }
}
