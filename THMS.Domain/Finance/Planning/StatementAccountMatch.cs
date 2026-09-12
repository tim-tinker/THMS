using THMS.Domain.Finance.Accounts;

namespace THMS.Domain.Finance.Planning
{
    public static class StatementAccountMatch
    {
        public static bool Matches(Account account, StatementType type) => type switch
        {
            StatementType.Bank => account is BankAccount,
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
            LoanAccount => StatementType.Loan,
            MortgageAccount => StatementType.Mortgage,
            CreditAccount => StatementType.CreditCard,
            UntrackedAccount when account.Type == AccountType.Utility => StatementType.Utility,
            UntrackedAccount when account.Type == AccountType.Service => StatementType.Service,
            UntrackedAccount when account.Type == AccountType.Insurance => StatementType.Insurance,
            _ => null
        };

        private static bool IsUntracked(Account account, AccountType type) =>
            account is UntrackedAccount && account.Type == type;
    }
}
