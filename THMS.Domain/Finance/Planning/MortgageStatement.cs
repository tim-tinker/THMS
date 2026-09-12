namespace THMS.Domain.Finance.Planning
{
    public class MortgageStatement : AccountStatement
    {
        public decimal PrincipalBalance { get; set; }
        public decimal InterestBalance { get; set; }
        public decimal EscrowBalance { get; set; }

        public decimal InterestCharged { get; set; }
        public decimal Fees { get; set; }

        public override StatementType Type => StatementType.Mortgage;
    }
}
