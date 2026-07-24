namespace THMS.Domain.Finance.Roi
{
    /// <summary>
    /// Represents solar ROI for a single month.
    /// </summary>
    public class SolarRoiResult
    {
        public int Year { get; set; }
        public int Month { get; set; }

        // Economics
        public decimal SolarAvoidedCost { get; set; }
        public decimal GridExportCredit { get; set; }
        public decimal BatteryValue { get; set; }

        // Loan
        public decimal LoanPayment { get; set; }
        public decimal LoanInterestPaid { get; set; }
        public decimal LoanPrincipalPaid { get; set; }

        // Net ROI
        public decimal NetRoi => SolarAvoidedCost + GridExportCredit + BatteryValue - LoanPayment;

        // Flags
        public bool IsPartial { get; set; }
    }
}
