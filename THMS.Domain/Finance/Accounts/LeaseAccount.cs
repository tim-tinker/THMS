namespace THMS.Domain.Finance.Accounts
{
    /// <summary>
    /// A lease uses the same balance, rate, term, and secured asset as a loan.
    /// The balance is the remaining payments and counts as debt.
    /// Lease amount and residual describe the contract and stay out of net worth.
    /// </summary>
    public class LeaseAccount : LoanAccount
    {
        /// <summary>Total of the payments over the life of the lease.</summary>
        public decimal LeaseAmount { get; set; }

        /// <summary>Buyout due at the end of the lease if you keep the vehicle.</summary>
        public decimal Residual { get; set; }
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
