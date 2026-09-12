namespace THMS.Domain.Finance.Planning
{
    public class BankStatement : AccountStatement
    {
        public DateTime PeriodStart { get; set; }

        public decimal BeginningBalance { get; set; }
        public decimal Deposits { get; set; }
        public decimal Withdrawals { get; set; }
        public decimal InterestEarned { get; set; }
        public decimal Fees { get; set; }
        public decimal EndingBalance { get; set; }

        public override StatementType Type => StatementType.Bank;

        public decimal ComputedEndingBalance =>
            ComputeEnding(BeginningBalance, Deposits, InterestEarned, Withdrawals, Fees);

        public static decimal ComputeEnding(
            decimal beginningBalance,
            decimal deposits,
            decimal interestEarned,
            decimal withdrawals,
            decimal fees) =>
            beginningBalance + deposits + interestEarned - withdrawals - fees;
    }
}
