namespace THMS.Domain.Finance.Loans
{
    /// <summary>
    /// Represents a single loan payment in the amortization schedule.
    /// This is pure domain data produced by the LoanAmortizationEngine.
    /// </summary>
    public class LoanCashFlow
    {
        /// <summary>
        /// The date of the payment (typically monthly).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Total payment amount for the period.
        /// </summary>
        public decimal PaymentAmount { get; set; }

        /// <summary>
        /// Portion of the payment that goes toward interest.
        /// </summary>
        public decimal InterestPaid { get; set; }

        /// <summary>
        /// Portion of the payment that goes toward principal.
        /// </summary>
        public decimal PrincipalPaid { get; set; }

        /// <summary>
        /// Remaining principal after this payment.
        /// </summary>
        public decimal RemainingPrincipal { get; set; }

        /// <summary>
        /// True if this payment includes a lump-sum principal reduction.
        /// </summary>
        public bool HasLumpSumPayment { get; set; }

        /// <summary>
        /// Amount of any lump-sum principal payment applied at this date.
        /// </summary>
        public decimal LumpSumAmount { get; set; }
    }
}
