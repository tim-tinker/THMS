using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Model;
using THMS.Logic.Finance.Planning;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Logic.Finance.Forecast
{
    public static class AccountBalanceProjection
    {
        public static BalanceProjection Build(
            Account account,
            IReadOnlyList<AccountStatement> statements,
            ITransactionDataStore transactions,
            DateTime asOf)
        {
            ArgumentNullException.ThrowIfNull(account);
            ArgumentNullException.ThrowIfNull(statements);
            ArgumentNullException.ThrowIfNull(transactions);

            asOf = asOf.Date;
            var through = asOf.AddMonths(1);
            var opening = OpeningBalance(account, statements, transactions);
            var movements = Movements(account, statements, transactions, through)
                .Select(item => (item.Date, item.Description, Amount: DisplayDelta(account, item.LedgerAmount)))
                .OrderBy(item => item.Date)
                .ThenByDescending(item => item.Amount)
                .ThenBy(item => item.Description)
                .ToList();

            var balance = opening;
            var rows = new List<BalanceProjectionRow>(movements.Count);
            foreach (var item in movements)
            {
                var amount = item.Amount;
                balance += amount;
                rows.Add(new BalanceProjectionRow
                {
                    Date = item.Date,
                    Description = item.Description,
                    Amount = amount,
                    Balance = balance
                });
            }

            return new BalanceProjection
            {
                AsOf = asOf,
                Through = through,
                OpeningBalance = opening,
                Rows = rows
            };
        }

        private static decimal OpeningBalance(
            Account account,
            IReadOnlyList<AccountStatement> statements,
            ITransactionDataStore transactions)
        {
            if (!PostedBalanceCalculator.TryResolveAnchor(account, statements, out var anchor))
                return PostedBalanceCalculator.ToDisplayBalance(account, StoredBalance(account));

            var activity = transactions.SumPostedAmountsAfter(account.Id, anchor.AsOf)
                + transactions.SumPostedTransferAmountsAfter(account.Id, anchor.AsOf);
            return PostedBalanceCalculator.ToDisplayBalance(
                account,
                PostedBalanceCalculator.ComputeFromAnchor(anchor, activity));
        }

        private static decimal StoredBalance(Account account) => account switch
        {
            BankAccount bank => bank.PostedBalance,
            CreditAccount credit => credit.PostedBalance,
            LoanAccount loan => loan.Principal,
            MortgageAccount mortgage => mortgage.Principal,
            _ => 0m
        };

        private static List<Movement> Movements(
            Account account,
            IReadOnlyList<AccountStatement> statements,
            ITransactionDataStore transactions,
            DateTime through)
        {
            var singles = transactions.GetAllFutureSingleTransactions().Where(item => !item.IsRealized).ToList();
            var transfers = transactions.GetAllFutureTransferTransactions().Where(item => !item.IsRealized).ToList();
            var movements = new List<Movement>();

            foreach (var item in singles.Where(item => AffectsSingle(item, account.Id) && item.Date.Date <= through))
            {
                movements.Add(new Movement(
                    item.Date.Date,
                    string.IsNullOrWhiteSpace(item.Description) ? "Bill" : item.Description.Trim(),
                    SplitTransactionMath.AmountForAccount(item, account.Id)));
            }

            foreach (var item in transfers.Where(item => AffectsTransfer(item, account.Id) && item.Date.Date <= through))
            {
                movements.Add(new Movement(
                    item.Date.Date,
                    string.IsNullOrWhiteSpace(item.Description) ? "Payment" : item.Description.Trim(),
                    SplitTransactionMath.TransferAmountForAccount(
                        item.FromAccountId, item.ToAccountId, item.Amount, account.Id)));
            }

            var forecast = new ForecastGenerator().GenerateForecast(
                account.Id,
                DateTime.MinValue,
                through,
                transactions.GetAllRecurringSingleRules(),
                transactions.GetAllRecurringTransferRules());
            var expected = singles.Cast<BaseTransaction>().Concat(transfers).ToList();
            foreach (var row in forecast.Where(row => row.Date.Date <= through && !CoveredByExpected(row, expected)))
            {
                movements.Add(new Movement(
                    row.Date.Date,
                    string.IsNullOrWhiteSpace(row.Description) ? "Bill" : row.Description.Trim(),
                    row.Amount));
            }

            var statement = PayableStatements.Latest(statements);
            if (statement is not null
                && statement.DueDate.Year > 1
                && statement.DueDate.Date <= through
                && !PaymentAlreadyCreated(statement, transactions))
            {
                movements.Add(new Movement(
                    statement.DueDate.Date,
                    string.IsNullOrWhiteSpace(statement.Notes) ? "Statement due" : statement.Notes.Trim(),
                    PostedBalanceCalculator.ToLedgerBalance(account, -Math.Abs(statement.AmountDue))));
            }

            return movements;
        }

        private static bool PaymentAlreadyCreated(AccountStatement statement, ITransactionDataStore transactions) =>
            transactions.GetAllFutureTransferTransactions().Any(item =>
                item.StatementId == statement.Id
                || (item.Origin == ExpectedOrigin.StatementPay && item.OriginId == statement.Id));

        private static bool CoveredByExpected(UnifiedTransactionView row, IReadOnlyList<BaseTransaction> expected) =>
            expected.Any(item =>
            {
                var accountsMatch = item switch
                {
                    FutureSingleTransaction single => single.AccountId == row.AccountId,
                    FutureTransferTransaction transfer =>
                        transfer.FromAccountId == row.AccountId || transfer.ToAccountId == row.AccountId,
                    _ => false
                };
                return accountsMatch
                    && Math.Abs(Math.Abs(item.Amount) - Math.Abs(row.Amount)) <= RecurringRulePattern.AmountTolerance
                    && Math.Abs((item.Date.Date - row.Date.Date).TotalDays) <= BillsOrchestrator.MatchDayTolerance;
            });

        private static bool AffectsSingle(FutureSingleTransaction item, Guid accountId) =>
            SplitTransactionMath.AffectsAccount(item, accountId);

        private static bool AffectsTransfer(FutureTransferTransaction item, Guid accountId) =>
            item.FromAccountId == accountId || item.ToAccountId == accountId;

        private static decimal DisplayDelta(Account account, decimal ledgerAmount) =>
            PostedBalanceCalculator.ToDisplayBalance(account, ledgerAmount);

        private readonly record struct Movement(DateTime Date, string Description, decimal LedgerAmount);
    }
}
