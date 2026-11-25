namespace THMS.Domain.Finance.Accounts
{
    /// <summary>
    /// A lease uses the same balance, term, and secured asset as a loan.
    /// Financing is a money factor, not an interest rate. Multiply the money factor
    /// by 2,400 for the approximate APR percent.
    /// The balance is the remaining payments and counts as debt.
    /// Lease amount and residual describe the contract and stay out of net worth.
    /// </summary>
    public class LeaseAccount : LoanAccount
    {
        /// <summary>Money factor times this value is the approximate APR percent.</summary>
        public const decimal AprMultiplier = 2400m;

        /// <summary>Total of the payments over the life of the lease.</summary>
        public decimal LeaseAmount { get; set; }

        /// <summary>Buyout due at the end of the lease if you keep the vehicle.</summary>
        public decimal Residual { get; set; }

        /// <summary>Agreed vehicle value plus amounts paid over the term.</summary>
        public decimal GrossCapitalizedCost { get; set; }

        /// <summary>Trade-in, rebate, and cash that reduce the gross capitalized cost.</summary>
        public decimal CapitalizedCostReduction { get; set; }

        /// <summary>Gross capitalized cost minus the reduction. This is the amount the payment is based on.</summary>
        public decimal AdjustedCapitalizedCost { get; set; }

        /// <summary>Finance charge over the whole term, called the rent charge on the lease.</summary>
        public decimal RentCharge { get; set; }

        /// <summary>Lease finance charge, such as 0.00125. Not an APR.</summary>
        public decimal MoneyFactor { get; set; }

        /// <summary>Approximate annual rate in percent, such as 3 for a money factor of 0.00125.</summary>
        public decimal ApproximateAprPercent => MoneyFactor * AprMultiplier;

        public static decimal MoneyFactorFromAprPercent(decimal aprPercent) =>
            aprPercent / AprMultiplier;

        /// <summary>
        /// Rent charge ÷ (term × (adjusted capitalized cost + residual)).
        /// </summary>
        public static decimal? MoneyFactorFromRentCharge(
            decimal rentCharge,
            decimal adjustedCapitalizedCost,
            decimal residual,
            int termMonths)
        {
            if (rentCharge <= 0 || adjustedCapitalizedCost <= 0 || termMonths <= 0)
                return null;

            var baseAmount = adjustedCapitalizedCost + residual;
            if (baseAmount <= 0)
                return null;

            return rentCharge / (termMonths * baseAmount);
        }
    }

    public static class LeaseEquity
    {
        /// <summary>
        /// What you would own if you keep the vehicle: its value, minus the remaining
        /// payments, minus the residual. The balance is the signed ledger amount.
        /// </summary>
        public static decimal IfKept(decimal valuation, decimal leaseBalance, decimal residual) =>
            valuation + leaseBalance - residual;
    }
}
