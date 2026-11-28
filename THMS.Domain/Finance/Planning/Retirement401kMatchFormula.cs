namespace THMS.Domain.Finance.Planning
{
    /// <summary>
    /// Employer match for one plan. Rates are fractions: 1 is a 100% match, and 0.06 is 6% of pay.
    /// </summary>
    public sealed record Retirement401kMatchFormula
    {
        public decimal FirstMatchRate { get; init; }
        public decimal FirstMatchOfPay { get; init; }
        public decimal NextMatchRate { get; init; }
        public decimal NextMatchOfPay { get; init; }
        public bool PlanOffersRoth { get; init; } = true;
    }
}
