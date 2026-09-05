using THMS.Domain.Finance.Loans;

namespace THMS.Logic.Finance.Model
{
    /// <summary>
    /// Generates an amortization schedule for a fixed-rate loan.
    /// Supports lump-sum payments and early payoff.
    /// </summary>
    public class LoanAmortizationEngine
    {
        /// <summary>
        /// Generates the amortization schedule.
        /// </summary>
        /// <param name="principal">Initial loan amount.</param>
        /// <param name="annualInterestRate">Annual interest rate (e.g., 0.0499 for 4.99%).</param>
        /// <param name="termMonths">Loan term in months.</param>
        /// <param name="monthlyPayment">Fixed monthly payment.</param>
        /// <param name="lumpSumPayments">Optional lump-sum payments keyed by date.</param>
        public IReadOnlyCollection<LoanCashFlow> GenerateSchedule(
            decimal principal,
            decimal annualInterestRate,
            int termMonths,
            decimal monthlyPayment,
            IReadOnlyDictionary<DateTime, decimal>? lumpSumPayments = null)
        {
            var schedule = new List<LoanCashFlow>();

            decimal remaining = principal;
            decimal monthlyRate = annualInterestRate / 12m;

            DateTime currentDate = DateTime.Today;

            for (int month = 1; month <= termMonths && remaining > 0; month++)
            {
                currentDate = currentDate.AddMonths(1);

                // Interest for the month
                decimal interest = remaining * monthlyRate;

                // Principal portion
                decimal principalPaid = monthlyPayment - interest;

                // If payment is too small (should not happen), clamp
                if (principalPaid < 0)
                    principalPaid = 0;

                // Apply principal reduction
                remaining -= principalPaid;

                bool hasLumpSum = false;
                decimal lumpSumAmount = 0;

                // Apply lump-sum payment if present
                if (lumpSumPayments != null &&
                    lumpSumPayments.TryGetValue(currentDate, out lumpSumAmount))
                {
                    hasLumpSum = true;
                    remaining -= lumpSumAmount;

                    if (remaining < 0)
                        remaining = 0;
                }

                // If remaining principal is less than the next payment,
                // adjust the final payment to avoid negative balance.
                decimal actualPayment = monthlyPayment;

                if (remaining < 0)
                    remaining = 0;

                if (remaining == 0)
                {
                    // Final payment is interest + principal needed to reach zero
                    actualPayment = interest + principalPaid + lumpSumAmount;
                }

                schedule.Add(new LoanCashFlow
                {
                    Date = currentDate,
                    PaymentAmount = actualPayment,
                    InterestPaid = interest,
                    PrincipalPaid = principalPaid,
                    RemainingPrincipal = remaining,
                    HasLumpSumPayment = hasLumpSum,
                    LumpSumAmount = lumpSumAmount
                });

                if (remaining == 0)
                    break;
            }

            return schedule.AsReadOnly();
        }
    }
}
