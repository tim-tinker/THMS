namespace THMS.Domain.Finance
{
    /// <summary>
    /// Represents the financial portion of a commercial EV charging session.
    /// Energy (Wh) is stored separately in IEnergyDataStore.
    /// </summary>
    public class CommercialChargingCostRecord
    {
        /// <summary>
        /// Timestamp of the charging session start.
        /// This aligns with the EvChargingSession.Timestamp stored in IEnergyDataStore.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Total cost charged by the commercial charging provider.
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Optional vendor name (ChargePoint, EVgo, Electrify America, etc.).
        /// Useful for dashboards and future analytics.
        /// </summary>
        public string Vendor { get; set; } = string.Empty;

        /// <summary>
        /// Optional session identifier from the vendor.
        /// Useful for reconciliation and debugging.
        /// </summary>
        public string? SessionId { get; set; }
    }
}
