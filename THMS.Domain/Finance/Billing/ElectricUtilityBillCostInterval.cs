namespace THMS.Domain.Finance.Billing
{
    /// <summary>
    /// Represents cost and credit information from the electric bill
    /// for a specific billing period. This contains no energy data.
    /// It is pure economics and is reconciled against HomeEnergyFlowInterval.
    /// </summary>
    public class ElectricUtilityBillCostInterval
    {
        /// <summary>
        /// Start of the billing period (inclusive).
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// End of the billing period (inclusive).
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Total cost charged for grid import during the billing period.
        /// This includes tiered rates, TOU rates, and seasonal variations.
        /// </summary>
        public decimal GridImportCost { get; set; }

        /// <summary>
        /// Total credit received for grid export during the billing period.
        /// This includes buyback rates, seasonal adjustments, and utility rules.
        /// </summary>
        public decimal GridExportCredit { get; set; }

        /// <summary>
        /// Fixed monthly charges (e.g., base service fee).
        /// </summary>
        public decimal FixedCharges { get; set; }

        /// <summary>
        /// Delivery charges, which may be separate from energy charges.
        /// </summary>
        public decimal DeliveryCharges { get; set; }

        /// <summary>
        /// Taxes, fees, and other miscellaneous charges.
        /// </summary>
        public decimal TaxesAndFees { get; set; }

        /// <summary>
        /// Total bill amount for the period.
        /// Useful for reconciliation and validation.
        /// </summary>
        public decimal TotalBillAmount =>
            GridImportCost
            - GridExportCredit
            + FixedCharges
            + DeliveryCharges
            + TaxesAndFees;

        /// <summary>
        /// Indicates whether this billing interval overlaps a given timestamp.
        /// Useful for cost attribution on a per-interval basis.
        /// </summary>
        public bool Covers(DateTime timestamp) =>
            timestamp >= Start && timestamp <= End;
    }
}
