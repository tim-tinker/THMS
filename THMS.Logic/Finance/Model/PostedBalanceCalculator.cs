using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Model
{
    public readonly record struct PostedBalanceAnchor(decimal LedgerBalance, DateTime AsOf);

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

        public static decimal ComputeFromAnchor(PostedBalanceAnchor anchor, decimal activityAfterAsOf) =>
            anchor.LedgerBalance + activityAfterAsOf;

        public static bool HasUsablePostedBalance(
            Account account,
            IEnumerable<AccountStatement> statements) =>
            TryResolveAnchor(account, statements, out _);

        public static HashSet<Guid> UsablePostedBalanceAccountIds(
            IEnumerable<Account> accounts,
            Func<Guid, IEnumerable<AccountStatement>> statementsForAccount)
        {
            var usable = new HashSet<Guid>();
            foreach (var account in accounts)
            {
                if (HasUsablePostedBalance(account, statementsForAccount(account.Id)))
                    usable.Add(account.Id);
            }

            return usable;
        }

        public static bool TryResolveAnchor(
            Account account,
            IEnumerable<AccountStatement> statements,
            out PostedBalanceAnchor anchor)
        {
            foreach (var statement in statements
                .OrderByDescending(s => s.StatementDate)
                .ThenByDescending(s => s.Id))
            {
                if (TryGetStatementAnchor(account, statement, out anchor))
                    return true;
            }

            anchor = default;
            return false;
        }

        public static bool TryGetStatementAnchor(
            Account account,
            AccountStatement statement,
            out PostedBalanceAnchor anchor)
        {
            switch (statement)
            {
                case BankStatement bank when account is BankAccount:
                    anchor = new PostedBalanceAnchor(bank.EndingBalance, bank.StatementDate);
                    return true;
                case CreditCardStatement card when account is CreditAccount credit:
                    anchor = new PostedBalanceAnchor(ToLedgerBalance(credit, card.StatementBalance), card.StatementDate);
                    return true;
                case LoanStatement loan when account is LoanAccount:
                    anchor = new PostedBalanceAnchor(loan.PrincipalBalance, loan.StatementDate);
                    return true;
                case MortgageStatement mortgage when account is MortgageAccount:
                    anchor = new PostedBalanceAnchor(mortgage.PrincipalBalance, mortgage.StatementDate);
                    return true;
                default:
                    anchor = default;
                    return false;
            }
        }

        public static void ApplyPostedBalance(Account account, decimal postedBalance) =>
            ApplyLedgerBalance(account, postedBalance);

        public static void ApplyLedgerBalance(Account account, decimal ledgerBalance)
        {
            switch (account)
            {
                case BankAccount bank:
                    bank.PostedBalance = ledgerBalance;
                    break;
                case CreditAccount credit:
                    credit.PostedBalance = ledgerBalance;
                    break;
                case LoanAccount loan:
                    loan.Principal = ledgerBalance;
                    break;
                case MortgageAccount mortgage:
                    mortgage.Principal = ledgerBalance;
                    break;
            }
        }

        public static bool SupportsStartingBalance(Account account) =>
            account is BankAccount or CreditAccount;

        public static void AdjustStartingBalance(Account account, decimal postedBalanceDelta)
        {
            if (postedBalanceDelta == 0)
                return;

            switch (account)
            {
                case BankAccount bank:
                    bank.StartingBalance += postedBalanceDelta;
                    bank.PostedBalance += postedBalanceDelta;
                    bank.BalanceAsOf = DateTime.Today;
                    break;
                case CreditAccount credit:
                    credit.StartingBalance += postedBalanceDelta;
                    credit.PostedBalance += postedBalanceDelta;
                    credit.BalanceAsOf = DateTime.Today;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Starting balance is only used for bank and credit accounts, not {account.GetType().Name}.");
            }
        }

        /// <summary>
        /// Credit balances are stored in ledger space (charges negative). Display space
        /// is inverted so a positive value is the amount owed.
        /// </summary>
        public static decimal ToDisplayBalance(Account? account, decimal ledgerBalance) =>
            account is CreditAccount ? -ledgerBalance : ledgerBalance;

        public static decimal ToLedgerBalance(Account? account, decimal displayBalance) =>
            account is CreditAccount ? -displayBalance : displayBalance;

        public static decimal ToLedgerPostedDelta(Account? account, decimal displayDelta) =>
            account is CreditAccount ? -displayDelta : displayDelta;
    }
}
