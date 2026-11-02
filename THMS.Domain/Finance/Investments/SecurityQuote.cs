namespace THMS.Domain.Finance.Investments
{
    public sealed class SecurityQuote
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AccountId { get; set; }
        public string Symbol { get; set; } = "";
        public DateTime AsOf { get; set; }
        public decimal? AnnualIncomePerShare { get; set; }
        public decimal? DistributionRate { get; set; }
        public decimal? AmountPerShare { get; set; }
        public DateTime? ExDate { get; set; }
        public DateTime? PayDate { get; set; }
    }
}
