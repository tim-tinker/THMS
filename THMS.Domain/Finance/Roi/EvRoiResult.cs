namespace THMS.Domain.Finance.Roi
{
    /// <summary>
    /// Represents EV ROI for a single month.
    /// </summary>
    public class EvRoiResult
    {
        public int Year { get; set; }
        public int Month { get; set; }

        // Energy-based economics
        public decimal SolarChargeValue { get; set; }
        public decimal BatteryChargeValue { get; set; }
        public decimal GridChargeCost { get; set; }
        public decimal CommercialChargeCost { get; set; }

        // Loan economics
        public decimal LoanPayment { get; set; }
        public decimal LoanInterestPaid { get; set; }
        public decimal LoanPrincipalPaid { get; set; }

        // Net ROI
        public decimal NetRoi =>
            SolarChargeValue +
            BatteryChargeValue -
            GridChargeCost -
            CommercialChargeCost -
            LoanPayment;

        // Flags
        public bool IsPartial { get; set; }
    }
}
