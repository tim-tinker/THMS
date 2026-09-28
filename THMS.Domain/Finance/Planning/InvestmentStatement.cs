namespace THMS.Domain.Finance.Planning
{
    public class InvestmentStatement : AccountStatement
    {
        public decimal StatementBalance { get; set; }

        public override StatementType Type => StatementType.Investment;
    }
}
