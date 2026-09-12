namespace THMS.Domain.Finance.Planning
{
    public class InsuranceStatement : AccountStatement
    {
        public decimal Premium { get; set; }
        public decimal Fees { get; set; }

        public override StatementType Type => StatementType.Insurance;
    }
}
