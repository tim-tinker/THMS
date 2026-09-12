namespace THMS.Domain.Finance.Planning
{
    public class UtilityStatement : AccountStatement
    {
        public List<UtilityUsageRecord> Usage { get; set; } = [];
        public List<UtilityChargeLine> Charges { get; set; } = [];

        public override StatementType Type => StatementType.Utility;
    }
}
