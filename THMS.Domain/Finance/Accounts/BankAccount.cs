namespace THMS.Domain.Finance.Accounts
{
    public class BankAccount : Account
    {
        public decimal StartingBalance { get; set; }
        public decimal PostedBalance { get; set; }
        public decimal OverdraftLimit { get; set; }
    }
}
