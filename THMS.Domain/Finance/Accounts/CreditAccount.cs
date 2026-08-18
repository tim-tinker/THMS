namespace THMS.Domain.Finance.Accounts
{
    public class CreditAccount : Account
    {
        public decimal CreditLimit { get; set; }
        public decimal APR { get; set; }
        public DateTime StatementDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal PostedBalance { get; set; }
    }
}
