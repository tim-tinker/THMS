namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents EV charging data at a commercial charger.
    /// </summary>
    public class EvChargingSession
    {
        public string Source { get; set; }

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

        public TimeSpan Duration { get; set; }
    }
}
