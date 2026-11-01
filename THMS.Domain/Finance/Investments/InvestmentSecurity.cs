namespace THMS.Domain.Finance.Investments
{
    public sealed class InvestmentSecurity
    {
        public string Symbol { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsSweep { get; set; }
    }
}
