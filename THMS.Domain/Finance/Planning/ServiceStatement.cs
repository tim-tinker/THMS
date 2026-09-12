namespace THMS.Domain.Finance.Planning
{
    public class ServiceStatement : AccountStatement
    {
        public List<ServiceChargeLine> Charges { get; set; } = [];

        public override StatementType Type => StatementType.Service;
    }
}
