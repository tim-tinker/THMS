using THMS.Domain.Finance.Accounts;

namespace THMS.Logic.ViewModels.Finance
{
    public class AccountImportPreview
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string WebsiteUrl { get; set; } = "";
        public decimal? CreditLimit { get; set; }
        public decimal? Apr { get; set; }
        public decimal? Principal { get; set; }
        public int? TermMonths { get; set; }
        public Account Account { get; set; } = null!;

        public static AccountImportPreview FromAccount(Account account) =>
            new()
            {
                Name = account.Name,
                Type = TypeLabel(account),
                AccountNumber = account.AccountNumber,
                WebsiteUrl = account.WebsiteUrl,
                CreditLimit = account is CreditAccount credit ? credit.CreditLimit : null,
                Apr = account is CreditAccount creditApr ? creditApr.APR : null,
                Principal = account switch
                {
                    LoanAccount loan => loan.Principal,
                    MortgageAccount mortgage => mortgage.Principal,
                    _ => null
                },
                TermMonths = account switch
                {
                    LoanAccount loan => loan.TermMonths,
                    MortgageAccount mortgage => mortgage.TermMonths,
                    _ => null
                },
                Account = account
            };

        public Account ApplyToAccount()
        {
            if (Account is null)
                throw new InvalidOperationException("Preview row is missing an account.");

            var kind = string.IsNullOrWhiteSpace(Type) ? TypeLabel(Account) : Type.Trim();
            var account = string.Equals(kind, TypeLabel(Account), StringComparison.OrdinalIgnoreCase)
                ? Account
                : CreateAccount(kind, Account);

            account.Name = Name ?? "";
            account.AccountNumber = AccountNumber ?? "";
            account.WebsiteUrl = WebsiteUrl ?? "";

            switch (account)
            {
                case CreditAccount credit:
                    if (CreditLimit.HasValue)
                        credit.CreditLimit = CreditLimit.Value;
                    if (Apr.HasValue)
                        credit.APR = Apr.Value;
                    break;
                case LoanAccount loan:
                    if (Principal.HasValue)
                        loan.Principal = Principal.Value;
                    if (TermMonths.HasValue)
                        loan.TermMonths = TermMonths.Value;
                    break;
                case MortgageAccount mortgage:
                    if (Principal.HasValue)
                        mortgage.Principal = Principal.Value;
                    if (TermMonths.HasValue)
                        mortgage.TermMonths = TermMonths.Value;
                    break;
            }

            Account = account;
            Type = TypeLabel(account);
            return account;
        }

        public static string TypeLabel(Account account) => account switch
        {
            BankAccount => AccountKinds.Bank,
            CreditAccount => AccountKinds.Credit,
            LoanAccount => AccountKinds.Loan,
            MortgageAccount => AccountKinds.Mortgage,
            InvestmentAccount => AccountKinds.Investment,
            InternalAccount => AccountKinds.Internal,
            UntrackedAccount when account.Type == AccountType.Utility => AccountKinds.Utility,
            UntrackedAccount when account.Type == AccountType.Service => AccountKinds.Service,
            UntrackedAccount when account.Type == AccountType.Insurance => AccountKinds.Insurance,
            _ => account.Type.ToString()
        };

        private static Account CreateAccount(string kind, Account source)
        {
            Account account = kind switch
            {
                AccountKinds.Bank => new BankAccount { Type = AccountType.Checking },
                AccountKinds.Credit => new CreditAccount { Type = AccountType.CreditCard },
                AccountKinds.Loan => new LoanAccount { Type = AccountType.Loan },
                AccountKinds.Mortgage => new MortgageAccount { Type = AccountType.Mortgage },
                AccountKinds.Investment => new InvestmentAccount { Type = AccountType.Investment },
                AccountKinds.Internal => new InternalAccount { Type = AccountType.Internal },
                AccountKinds.Utility => new UntrackedAccount { Type = AccountType.Utility },
                AccountKinds.Service => new UntrackedAccount { Type = AccountType.Service },
                AccountKinds.Insurance => new UntrackedAccount { Type = AccountType.Insurance },
                _ => throw new InvalidOperationException($"Unknown account type '{kind}'.")
            };

            account.Id = source.Id;
            account.Institution = source.Institution;
            account.BalanceAsOf = source.BalanceAsOf;
            account.ExternalLink = source.ExternalLink;
            return account;
        }
    }
}
