using THMS.Domain.Finance.Accounts;

namespace THMS.Domain.Finance.Planning
{
    public static class StatementAccountMatch
    {
        public static bool Matches(Account account, StatementType type) => type switch
        {
            StatementType.Bank => account is BankAccount,
            StatementType.Investment => account is InvestmentAccount,
            StatementType.Loan => account is LoanAccount,
            StatementType.Mortgage => account is MortgageAccount,
            StatementType.CreditCard => account is CreditAccount,
            StatementType.Utility => IsUntracked(account, AccountType.Utility),
            StatementType.Service => IsUntracked(account, AccountType.Service),
            StatementType.Insurance => IsUntracked(account, AccountType.Insurance),
            _ => false
        };

        public static Account CreateAccount(StatementType type) => type switch
        {
            StatementType.Bank => new BankAccount { Type = AccountType.Checking },
            StatementType.Investment => new InvestmentAccount { Type = AccountType.Investment },
            StatementType.Loan => new LoanAccount { Type = AccountType.Loan },
            StatementType.Mortgage => new MortgageAccount
            {
                Type = AccountType.Mortgage,
                NextPaymentDate = DateTime.Today
            },
            StatementType.CreditCard => new CreditAccount
            {
                Type = AccountType.CreditCard,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today
            },
            StatementType.Utility => new UntrackedAccount { Type = AccountType.Utility },
            StatementType.Service => new UntrackedAccount { Type = AccountType.Service },
            StatementType.Insurance => new UntrackedAccount { Type = AccountType.Insurance },
            _ => new UntrackedAccount { Type = AccountType.Utility }
        };

        public static StatementType? ForAccount(Account account) => account switch
        {
            BankAccount => StatementType.Bank,
            InvestmentAccount => StatementType.Investment,
            LoanAccount => StatementType.Loan,
            MortgageAccount => StatementType.Mortgage,
            CreditAccount => StatementType.CreditCard,
            UntrackedAccount when account.Type == AccountType.Utility => StatementType.Utility,
            UntrackedAccount when account.Type == AccountType.Service => StatementType.Service,
            UntrackedAccount when account.Type == AccountType.Insurance => StatementType.Insurance,
            _ => null
        };

        public static AccountStatement CreateStatement(StatementType type) => type switch
        {
            StatementType.Bank => new BankStatement(),
            StatementType.Investment => new InvestmentStatement(),
            StatementType.Loan => new LoanStatement(),
            StatementType.Mortgage => new MortgageStatement(),
            StatementType.CreditCard => new CreditCardStatement(),
            StatementType.Utility => new UtilityStatement(),
            StatementType.Service => new ServiceStatement(),
            StatementType.Insurance => new InsuranceStatement(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported statement type.")
        };

        public static bool IsNonPayable(StatementType type) =>
            type is StatementType.Bank or StatementType.Investment;

        public static bool IsNonPayable(AccountStatement statement) =>
            IsNonPayable(statement.Type);

        private static bool IsUntracked(Account account, AccountType type) =>
            account is UntrackedAccount && account.Type == type;
    }
}
