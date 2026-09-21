namespace THMS.Domain.Finance.Accounts
{
    public abstract class Account : BaseDomainModel
    {
        public string Name { get; set; }
        public string Institution { get; set; }
        public string AccountNumber { get; set; }
        public AccountType Type { get; set; }
        public DateTime? BalanceAsOf { get; set; }

        public ExternalAccountLink? ExternalLink { get; set; }
        public string WebsiteUrl { get; set; }
        public bool AutoPay { get; set; }
        public Guid? AutoPayFromAccountId { get; set; }

        public bool SupportsAutoPay =>
            this is CreditAccount or LoanAccount or MortgageAccount or UntrackedAccount;
    }
}
