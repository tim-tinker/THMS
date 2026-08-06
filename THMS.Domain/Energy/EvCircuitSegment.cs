namespace THMS.Domain.Energy
{
    /// <summary>
    /// A processed EV circuit segment belonging to a charging session.
    /// Raw readings are converted into segments with attribution fields.
    /// </summary>
    public class EvCircuitSegment
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The charging session this segment belongs to.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Timestamp at the start of the segment.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Duration of the segment in seconds, computed from the next timestamp.
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// Total energy consumed during the segment (in kWh).
        /// This comes directly from the circuit file.
        /// </summary>
        public decimal Kwh { get; set; }

        /// <summary>
        /// Energy attributed to the grid (in kWh).
        /// Initially equals Kwh until solar/battery attribution is applied.
        /// </summary>
        public decimal GridKwh { get; set; }

        /// <summary>
        /// Energy attributed to solar (in kWh).
        /// Initially zero.
        /// </summary>
        public decimal SolarKwh { get; set; }

        /// <summary>
        /// Energy attributed to battery (in kWh).
        /// Initially zero.
        /// </summary>
        public decimal BatteryKwh { get; set; }
    }
}
