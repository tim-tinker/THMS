namespace THMS.Domain.Finance.Accounts
{
    public static class AccountKinds
    {
        public const string Bank = "Bank";
        public const string Credit = "Credit";
        public const string Loan = "Loan";
        public const string Mortgage = "Mortgage";
        public const string Investment = "Investment";
        public const string Internal = "Internal";
        public const string Utility = "Utility";
        public const string Service = "Service";
        public const string Insurance = "Insurance";

        public static string Of(Account account) => account switch
        {
            BankAccount => Bank,
            CreditAccount => Credit,
            LoanAccount => Loan,
            MortgageAccount => Mortgage,
            InvestmentAccount => Investment,
            InternalAccount => Internal,
            UntrackedAccount when account.Type == AccountType.Utility => Utility,
            UntrackedAccount when account.Type == AccountType.Service => Service,
            UntrackedAccount when account.Type == AccountType.Insurance => Insurance,
            UntrackedAccount => "Untracked",
            _ => account.Type.ToString()
        };
    }
}
