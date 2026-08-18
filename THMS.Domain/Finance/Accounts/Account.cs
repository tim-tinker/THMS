namespace THMS.Domain.Finance.Accounts
{
    public abstract class Account : BaseDomainModel
    {
        public string Name { get; set; }
        public string Institution { get; set; }
        public string AccountNumber { get; set; }
        public AccountType Type { get; set; }
        public DateTime? BalanceAsOf { get; set; }
    }
}
