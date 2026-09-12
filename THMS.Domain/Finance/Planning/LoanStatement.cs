namespace THMS.Domain.Finance.Planning
{
    public class LoanStatement : AccountStatement
    {
        public decimal PrincipalBalance { get; set; }
        public decimal InterestBalance { get; set; }
        public decimal InterestCharged { get; set; }
        public decimal Fees { get; set; }

        public override StatementType Type => StatementType.Loan;
    }
}
