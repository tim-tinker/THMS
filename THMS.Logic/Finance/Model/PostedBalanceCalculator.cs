using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Model
{
    public static class PostedBalanceCalculator
    {
        public static decimal GetStartingBalance(Account? account) => account switch
        {
            BankAccount bank => bank.StartingBalance,
            CreditAccount credit => credit.StartingBalance,
            InvestmentAccount investment => investment.CashBalance,
            LoanAccount loan => loan.Principal,
            MortgageAccount mortgage => mortgage.Principal,
            _ => 0
        };

        public static decimal Compute(
            decimal startingBalance,
            IEnumerable<PostedTransaction> posted,
            IEnumerable<PostedTransferTransaction> postedTransfers)
        {
            return startingBalance
                + posted.Sum(t => t.Amount)
                + postedTransfers.Sum(t => t.Amount);
        }

        public static void ApplyPostedBalance(Account account, decimal postedBalance)
        {
            switch (account)
            {
                case BankAccount bank:
                    bank.PostedBalance = postedBalance;
                    break;
                case CreditAccount credit:
                    credit.PostedBalance = postedBalance;
                    break;
            }
        }

        public static bool SupportsStartingBalance(Account account) =>
            account is BankAccount or CreditAccount;

        public static void AdjustStartingBalance(Account account, decimal postedBalanceDelta)
        {
            switch (account)
            {
                case BankAccount bank:
                    bank.StartingBalance += postedBalanceDelta;
                    bank.PostedBalance += postedBalanceDelta;
                    break;
                case CreditAccount credit:
                    credit.StartingBalance += postedBalanceDelta;
                    credit.PostedBalance += postedBalanceDelta;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Starting balance is only used for bank and credit accounts, not {account.GetType().Name}.");
            }
        }
    }
}
