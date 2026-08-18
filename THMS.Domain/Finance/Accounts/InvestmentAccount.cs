namespace THMS.Domain.Finance.Accounts
{
    public class InvestmentAccount : Account
    {
        public decimal CashBalance { get; set; }
        public decimal MarketValue { get; set; }
    }
}
