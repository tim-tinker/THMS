namespace THMS.Domain.Finance.Investments
{
    public enum InvestmentActionKind
    {
        Unknown = 0,
        Opening = 1,
        OpeningCash = 2,
        Contribution = 3,
        Distribution = 4,
        Dividend = 5,
        Interest = 6,
        Income = 7,
        Buy = 8,
        Sell = 9,
        Reinvestment = 10,
        Split = 11
    }
}
