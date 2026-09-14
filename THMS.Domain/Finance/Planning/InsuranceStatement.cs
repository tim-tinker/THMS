namespace THMS.Domain.Finance.Planning
{
    public class InsuranceStatement : AccountStatement
    {
        public override StatementType Type => StatementType.Insurance;
    }
}
