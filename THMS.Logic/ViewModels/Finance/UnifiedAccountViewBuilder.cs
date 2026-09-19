using THMS.Domain.Finance.Accounts;
using THMS.Logic.Finance.Model;

namespace THMS.Logic.ViewModels.Finance
{
    public static class UnifiedAccountViewBuilder
    {
        public static List<UnifiedAccountView> Build(
            IEnumerable<Account> accounts,
            IReadOnlyDictionary<Guid, DateTime?>? nextPaymentByAccount = null,
            IReadOnlySet<Guid>? usablePostedBalanceAccountIds = null,
            IReadOnlyDictionary<Guid, PostedBalanceDisplay>? livePostedBalances = null)
        {
            var list = new List<UnifiedAccountView>();

            foreach (var acct in accounts)
            {
                var live = LookupLive(acct.Id, livePostedBalances);
                var useStoredPosted = livePostedBalances is null
                    && IsUsable(acct.Id, usablePostedBalanceAccountIds);
                var view = new UnifiedAccountView
                {
                    Id = acct.Id,
                    Name = acct.Name,
                    Institution = acct.Institution,
                    AccountNumber = acct.AccountNumber,
                    AccountType = Kind(acct),
                    WebsiteUrl = acct.WebsiteUrl ?? "",
                    AsOfDate = live?.AsOf ?? acct.BalanceAsOf
                };

                if (live is PostedBalanceDisplay snapshot)
                    ApplySnapshot(view, snapshot);

                switch (acct)
                {
                    case BankAccount bank:
                        if (live is null && useStoredPosted)
                            view.Balance = bank.PostedBalance;
                        view.BankCreditAvailable = bank.OverdraftLimit;
                        break;

                    case CreditAccount credit:
                        if (live is null && useStoredPosted)
                            view.Balance = PostedBalanceCalculator.ToDisplayBalance(credit, credit.PostedBalance);
                        if (view.Balance is decimal creditBalance)
                            view.BankCreditAvailable = credit.CreditLimit - creditBalance;
                        view.CreditLimit = credit.CreditLimit;
                        view.APR = PositiveRate(credit.APR);
                        view.DueDate = live?.DueDate ?? PostedBalanceCalculator.UsableDate(credit.DueDate);
                        break;

                    case InvestmentAccount inv:
                        view.Balance = inv.CashBalance;
                        break;

                    case LoanAccount loan:
                        if (live is null && useStoredPosted)
                            view.Balance = loan.Principal;
                        view.APR = PositiveRate(loan.InterestRate);
                        view.DueDate = LookupNextPayment(acct.Id, nextPaymentByAccount) ?? live?.DueDate;
                        break;

                    case MortgageAccount mortgage:
                        if (live is null && useStoredPosted)
                            view.Balance = mortgage.Principal;
                        view.APR = PositiveRate(mortgage.InterestRate);
                        view.DueDate = LookupNextPayment(acct.Id, nextPaymentByAccount) ?? live?.DueDate;
                        break;
                }

                list.Add(view);
            }

            return list;
        }

        private static PostedBalanceDisplay? LookupLive(
            Guid accountId,
            IReadOnlyDictionary<Guid, PostedBalanceDisplay>? livePostedBalances)
        {
            if (livePostedBalances is not null && livePostedBalances.TryGetValue(accountId, out var live))
                return live;
            return null;
        }

        private static void ApplySnapshot(UnifiedAccountView view, PostedBalanceDisplay snapshot)
        {
            view.StatementDate = snapshot.StatementDate;
            view.StatementBalance = snapshot.StatementBalance;
            view.AsOfDate = snapshot.AsOf;
            view.Balance = snapshot.Balance;
            view.AmountDue = snapshot.AmountDue;
            view.DueDate = snapshot.DueDate;
        }

        private static decimal? PositiveRate(decimal rate) =>
            rate > 0 ? rate : null;

        private static bool IsUsable(Guid accountId, IReadOnlySet<Guid>? usablePostedBalanceAccountIds) =>
            usablePostedBalanceAccountIds is null || usablePostedBalanceAccountIds.Contains(accountId);

        private static DateTime? LookupNextPayment(
            Guid accountId,
            IReadOnlyDictionary<Guid, DateTime?>? nextPaymentByAccount)
        {
            if (nextPaymentByAccount is null)
                return null;

            return nextPaymentByAccount.TryGetValue(accountId, out var next) ? next : null;
        }

        private static string Kind(Account acct) => AccountKinds.Of(acct);
    }
}
