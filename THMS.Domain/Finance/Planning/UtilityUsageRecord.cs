namespace THMS.Domain.Finance.Planning
{
    public class UtilityUsageRecord
    {
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }

        public decimal Cost =>
            Math.Round(Amount * Rate, 2, MidpointRounding.AwayFromZero);
    }
}
