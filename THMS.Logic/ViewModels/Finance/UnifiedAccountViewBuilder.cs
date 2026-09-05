using THMS.Domain.Finance.Accounts;

namespace THMS.Logic.ViewModels.Finance
{
    public static class UnifiedAccountViewBuilder
    {
        public static List<UnifiedAccountView> Build(IEnumerable<Account> accounts)
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
                    AccountType = $"{acct.Type}",
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
                        break;

                    case MortgageAccount mortgage:
                        view.Balance = mortgage.Principal;
                        view.APR = mortgage.InterestRate;
                        view.DueDate = mortgage.NextPaymentDate;
                        break;
                }

                list.Add(view);
            }

            return list;
        }
    }
}
