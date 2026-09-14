namespace THMS.Domain.Finance.Planning
{
    public class LoanStatement : AccountStatement
    {
        public decimal StatementBalance { get; set; }

        public override StatementType Type => StatementType.Loan;
    }
}
