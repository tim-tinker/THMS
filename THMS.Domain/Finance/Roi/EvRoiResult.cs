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
        public decimal SolarChargingValue { get; set; }
        public decimal BatteryChargingValue { get; set; }
        public decimal GridChargingCost { get; set; }
        public decimal CommercialChargingCost { get; set; }

        // Loan economics
        public decimal LoanPayment { get; set; }
        public decimal LoanInterestPaid { get; set; }
        public decimal LoanPrincipalPaid { get; set; }

        // Net ROI
        public decimal NetRoi =>
            SolarChargingValue +
            BatteryChargingValue -
            GridChargingCost -
            CommercialChargingCost -
            LoanPayment;

        // Flags
        public bool IsPartial { get; set; }
    }
}
