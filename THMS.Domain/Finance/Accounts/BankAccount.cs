namespace THMS.Domain.Finance.Accounts
{
    public class BankAccount : Account
    {
        public decimal PostedBalance { get; set; }
        public decimal OverdraftLimit { get; set; }
    }
}
