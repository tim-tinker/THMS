namespace THMS.Domain.Finance.Accounts
{
    public class MortgageAccount : Account
    {
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
        public DateTime NextPaymentDate { get; set; }
    }
}
