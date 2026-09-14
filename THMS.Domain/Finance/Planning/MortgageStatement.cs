namespace THMS.Domain.Finance.Planning
{
    public class MortgageStatement : AccountStatement
    {
        public decimal StatementBalance { get; set; }
        public decimal EscrowBalance { get; set; }

        public override StatementType Type => StatementType.Mortgage;
    }
}
