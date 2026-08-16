namespace THMS.Domain.Finance.Billing
{
    /// <summary>
    /// Represents a monthly electric utility bill.
    /// Contains all cost components for the billing cycle.
    /// </summary>
    public class ElectricUtilityBill
    {
        /// <summary>
        /// Unique identifier for this bill.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// As-of date for the electric bill
        /// </summary>
        public DateTime BillingDate { get; set; }

        /// <summary>
        /// First day of the billing cycle.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Last day of the billing cycle ("as of" date).
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Number of kWh used in the month
        /// </summary>
        public int KwhUsage { get; set; }

        /// <summary>
        /// Number of days covered by the bill
        /// </summary>
        public int DaysInCycle { get; set; }

        /// <summary>
        /// Utility base charge
        /// </summary>
        public decimal BaseCharge { get; set; }

        /// <summary>
        /// $/kWh charged by utility
        /// </summary>
        public decimal EnergyChargeRate { get; set; }

        /// <summary>
        /// Charge by utility for kWh usage
        /// </summary>
        public decimal EnergyCharge { get; set; }

        /// <summary>
        /// kWh exported to grid
        /// </summary>
        public decimal ExportKwh { get; set; }

        /// <summary>
        /// $/kWh credited by utility for exported energy
        /// </summary>
        public decimal ExportCreditRate { get; set; }

        /// <summary>
        /// Credit from utility for kWh exported
        /// </summary>
        public decimal ExportCredit { get; set; }

        /// <summary>
        /// Delivery charge for kWh used
        /// </summary>
        public decimal DeliveryCharge { get; set; }

        /// <summary>
        /// Total cost for the billing cycle:
        /// BaseCharge + EnergyCharge - ExportCredit + DeliveryCharge
        /// </summary>
        public decimal TotalCost =>
            BaseCharge
            + EnergyCharge
            - ExportCredit
            + DeliveryCharge;
    }
}
