namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents EV circuit load for a specific timestamp.
    /// This is raw energy data (Wh), not cost, not attribution.
    /// </summary>
    public class EvCircuitReading
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
        public decimal CircuitUseWh { get; set; }

    }
}
