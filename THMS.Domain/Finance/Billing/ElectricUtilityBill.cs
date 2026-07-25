namespace THMS.Domain.Finance.Billing
{
    /// <summary>
    /// Represents a monthly electric utility bill.
    /// Contains all cost components for the billing cycle.
    /// </summary>
    public class ElectricUtilityBill
    {
        /// <summary>
        /// First day of the billing cycle.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Last day of the billing cycle ("as of" date).
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Total cost for grid-imported energy.
        /// </summary>
        public decimal GridImportCost { get; set; }

        /// <summary>
        /// Total credit for grid-exported energy (solar buyback).
        /// </summary>
        public decimal GridExportCredit { get; set; }

        /// <summary>
        /// Delivery charges for the billing cycle.
        /// </summary>
        public decimal DeliveryCharges { get; set; }

        /// <summary>
        /// Fixed monthly charges (service fees, meter fees, etc.).
        /// </summary>
        public decimal FixedCharges { get; set; }

        /// <summary>
        /// Taxes and fees applied to the bill.
        /// </summary>
        public decimal TaxesAndFees { get; set; }
    }
}
