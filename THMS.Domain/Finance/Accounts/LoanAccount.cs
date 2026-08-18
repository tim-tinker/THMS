namespace THMS.Domain.Finance.Accounts
{
    public class LoanAccount : Account
    {
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime NextPaymentDate { get; set; }
    }
}
