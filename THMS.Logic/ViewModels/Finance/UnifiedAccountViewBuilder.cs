using THMS.Domain.Finance.Accounts;

namespace THMS.Logic.ViewModels.Finance
{
    public static class UnifiedAccountViewBuilder
    {
        public static List<UnifiedAccountView> Build(
            IEnumerable<Account> accounts,
            IReadOnlyDictionary<Guid, DateTime?>? nextPaymentByAccount = null)
        {
            var list = new List<UnifiedAccountView>();

            foreach (var acct in accounts)
            {
                var view = new UnifiedAccountView
                {
                    Id = acct.Id,
                    Name = acct.Name,
                    Institution = acct.Institution,
                    AccountNumber = acct.AccountNumber,
                    AccountType = Kind(acct),
                    WebsiteUrl = acct.WebsiteUrl ?? "",
                    AsOfDate = acct.BalanceAsOf
                };

                switch (acct)
                {
                    case BankAccount bank:
                        view.Balance = bank.PostedBalance;
                        view.BankCreditAvailable = bank.OverdraftLimit;
                        break;

                    case CreditAccount credit:
                        view.Balance = credit.PostedBalance;
                        view.CreditLimit = credit.CreditLimit;
                        view.DueDate = credit.DueDate;
                        break;

                    case InvestmentAccount inv:
                        view.Balance = inv.CashBalance;
                        break;

                    case LoanAccount loan:
                        view.Balance = loan.Principal;
                        view.APR = loan.InterestRate;
                        view.DueDate = LookupNextPayment(acct.Id, nextPaymentByAccount);
                        break;

                    case MortgageAccount mortgage:
                        view.Balance = mortgage.Principal;
                        view.APR = mortgage.InterestRate;
                        view.DueDate = LookupNextPayment(acct.Id, nextPaymentByAccount);
                        break;
                }

                list.Add(view);
            }

            return list;
        }

        private static DateTime? LookupNextPayment(
            Guid accountId,
            IReadOnlyDictionary<Guid, DateTime?>? nextPaymentByAccount)
        {
            if (nextPaymentByAccount is null)
                return null;

            return nextPaymentByAccount.TryGetValue(accountId, out var next) ? next : null;
        }

        private static string Kind(Account acct) => acct switch
        {
            BankAccount => "Bank",
            CreditAccount => "Credit",
            LoanAccount => "Loan",
            MortgageAccount => "Mortgage",
            InvestmentAccount => "Investment",
            InternalAccount => "Internal",
            _ => acct.Type.ToString()
        };
    }
}
