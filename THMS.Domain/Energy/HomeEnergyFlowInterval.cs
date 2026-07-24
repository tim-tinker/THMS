namespace THMS.Domain.Energy
{
    /// <summary>
    /// Represents whole-home energy flows for a specific timestamp.
    /// This is raw physical energy data (Wh), not cost or attribution.
    /// Derived from solar vendor data and normalized to 30-minute intervals.
    /// </summary>
    public class HomeEnergyFlowInterval
    {
        /// <summary>
        /// The timestamp representing the start of the interval.
        /// Normalized to 30-minute buckets by the ingestion pipeline.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Energy produced by the solar panels during this interval (Wh).
        /// </summary>
        public decimal SolarProducedWh { get; set; }

        /// <summary>
        /// Solar energy consumed directly by the home during this interval (Wh).
        /// This reduces grid import and increases solar ROI.
        /// </summary>
        public decimal SolarConsumedWh { get; set; }

        /// <summary>
        /// Energy imported from the grid during this interval (Wh).
        /// Used for billing validation and EV charging cost attribution.
        /// </summary>
        public decimal GridImportedWh { get; set; }

        /// <summary>
        /// Energy exported to the grid during this interval (Wh).
        /// Used for export credit calculations and solar ROI.
        /// </summary>
        public decimal GridExportedWh { get; set; }

        /// <summary>
        /// Energy stored in the battery during this interval (Wh).
        /// </summary>
        public decimal BatteryStoredWh { get; set; }

        /// <summary>
        /// Energy discharged from the battery during this interval (Wh).
        /// </summary>
        public decimal BatteryDischargedWh { get; set; }

        /// <summary>
        /// Indicates whether this interval contains any solar vendor data.
        /// Useful for partial ingestion and determining completeness.
        /// </summary>
        public bool HasSolarData =>
            SolarProducedWh > 0 ||
            SolarConsumedWh > 0 ||
            GridImportedWh > 0 ||
            GridExportedWh > 0 ||
            BatteryStoredWh > 0 ||
            BatteryDischargedWh > 0;
    }
}
