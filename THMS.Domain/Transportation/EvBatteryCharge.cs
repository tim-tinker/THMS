namespace THMS.Domain.Transportation
{
    /// <summary>
    /// Battery kWh added during one charge session.
    /// Stored apart from the session so a coefficient refit can replace calculated
    /// values without rewriting the session.
    /// </summary>
    public class EvBatteryCharge
    {
        public decimal KwhAdded { get; set; }

        /// <summary>
        /// True when <see cref="KwhAdded"/> was filled from the SOC energy curve.
        /// Calculated values are kept out of the next coefficient fit.
        /// </summary>
        public bool IsCalculated { get; set; }
    }
}