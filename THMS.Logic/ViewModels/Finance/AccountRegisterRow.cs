using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Planning;

namespace THMS.Logic.ViewModels.Finance
{
    public sealed class AccountRegisterRow
    {
        public const string AutoPaid = "Auto";
        public const string NotAutoPaid = "-";

        public Guid Id { get; init; }
        public required Account Account { get; init; }
        public string Name { get; init; } = "";
        public string WebsiteUrl { get; init; } = "";
        public string StatementDate { get; init; } = AccountStatementListRow.NotApplicable;
        public DateTime? StatementDateValue { get; init; }
        public string StatementBalance { get; init; } = AccountStatementListRow.NotApplicable;
        public decimal? StatementBalanceValue { get; init; }
        public string InterestPaid { get; init; } = "";
        public decimal? InterestPaidValue { get; init; }
        public string AmountDue { get; init; } = AccountStatementListRow.NotApplicable;
        public decimal? AmountDueValue { get; init; }
        public string Paid { get; init; } = "";
        public string DueDate { get; init; } = AccountStatementListRow.NotApplicable;
        public DateTime? DueDateValue { get; init; }
        public string Apr { get; init; } = "";
        public decimal? AprValue { get; init; }
        public string CreditLimit { get; init; } = "";
        public decimal? CreditLimitValue { get; init; }

        public static AccountRegisterRow From(Account account, AccountStatement? latestStatement) =>
            From(account, latestStatement is null ? [] : [latestStatement], posted: []);

        public static AccountRegisterRow From(
            Account account,
            IReadOnlyList<AccountStatement> statements,
            IEnumerable<PostedTransaction> posted)
        {
            ArgumentNullException.ThrowIfNull(account);
            ArgumentNullException.ThrowIfNull(statements);
            ArgumentNullException.ThrowIfNull(posted);

            var latest = statements
                .OrderByDescending(s => s.StatementDate)
                .ThenByDescending(s => s.DueDate)
                .FirstOrDefault();
            var statement = latest is null ? null : AccountStatementListRow.From(latest);
            var interest = latest is null
                ? 0m
                : StatementPeriodInterest.Compute(latest, statements, posted);
            var apr = AprNumber(account);
            var creditLimit = account is CreditAccount credit && credit.CreditLimit > 0
                ? credit.CreditLimit
                : (decimal?)null;
            var hasDue = latest is not null and not BankStatement;

            return new AccountRegisterRow
            {
                Id = account.Id,
                Account = account,
                Name = account.Name,
                WebsiteUrl = account.WebsiteUrl ?? "",
                StatementDate = statement?.StatementDate ?? AccountStatementListRow.NotApplicable,
                StatementDateValue = latest?.StatementDate,
                StatementBalance = statement?.StatementBalance ?? AccountStatementListRow.NotApplicable,
                StatementBalanceValue = BalanceValue(latest),
                InterestPaid = interest == 0 ? "" : interest.ToString("c2"),
                InterestPaidValue = interest == 0 ? null : interest,
                AmountDue = statement?.AmountDue ?? AccountStatementListRow.NotApplicable,
                AmountDueValue = hasDue ? latest!.AmountDue : null,
                Paid = PaidLabel(account),
                DueDate = statement?.DueDate ?? AccountStatementListRow.NotApplicable,
                DueDateValue = hasDue ? latest!.DueDate : null,
                Apr = FormatApr(apr),
                AprValue = apr,
                CreditLimit = creditLimit is decimal limit ? limit.ToString("c2") : "",
                CreditLimitValue = creditLimit
            };
        }

        public static int Compare(AccountRegisterRow x, AccountRegisterRow y, string propertyName, bool descending)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            var result = propertyName switch
            {
                nameof(Name) => CompareText(x.Name, y.Name, descending),
                nameof(StatementDate) => CompareNullable(x.StatementDateValue, y.StatementDateValue, descending),
                nameof(StatementBalance) => CompareNullable(x.StatementBalanceValue, y.StatementBalanceValue, descending),
                nameof(InterestPaid) => CompareNullable(x.InterestPaidValue, y.InterestPaidValue, descending),
                nameof(AmountDue) => CompareNullable(x.AmountDueValue, y.AmountDueValue, descending),
                nameof(Paid) => CompareText(x.Paid, y.Paid, descending),
                nameof(DueDate) => CompareNullable(x.DueDateValue, y.DueDateValue, descending),
                nameof(Apr) => CompareNullable(x.AprValue, y.AprValue, descending),
                nameof(CreditLimit) => CompareNullable(x.CreditLimitValue, y.CreditLimitValue, descending),
                _ => CompareText(x.Name, y.Name, descending)
            };

            if (result != 0)
                return result;

            result = CompareText(x.Name, y.Name, descending: false);
            return result != 0 ? result : x.Id.CompareTo(y.Id);
        }

        public static string FormatApr(Account account) => FormatApr(AprNumber(account));

        private static string FormatApr(decimal? percent) =>
            percent is decimal value ? value.ToString("0.###") + "%" : "";

        private static decimal? AprNumber(Account account)
        {
            decimal? rate = account switch
            {
                CreditAccount credit => credit.APR,
                LoanAccount loan => loan.InterestRate,
                MortgageAccount mortgage => mortgage.InterestRate,
                _ => null
            };
            if (rate is not decimal value || value <= 0)
                return null;

            return value <= 1m ? value * 100m : value;
        }

        private static decimal? BalanceValue(AccountStatement? latest) => latest switch
        {
            BankStatement bank => bank.StatementBalance,
            LoanStatement loan => loan.StatementBalance,
            MortgageStatement mortgage => mortgage.StatementBalance,
            CreditCardStatement card => card.StatementBalance,
            _ => null
        };

        private static int CompareText(string? left, string? right, bool descending)
        {
            var leftEmpty = string.IsNullOrWhiteSpace(left);
            var rightEmpty = string.IsNullOrWhiteSpace(right);
            if (leftEmpty && rightEmpty)
                return 0;
            if (leftEmpty)
                return 1;
            if (rightEmpty)
                return -1;
            var result = string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase);
            return descending ? -result : result;
        }

        private static int CompareNullable<T>(T? left, T? right, bool descending)
            where T : struct, IComparable<T>
        {
            if (left is null && right is null)
                return 0;
            if (left is null)
                return 1;
            if (right is null)
                return -1;
            var result = left.Value.CompareTo(right.Value);
            return descending ? -result : result;
        }

        private static string PaidLabel(Account account)
        {
            if (account.AutoPay)
                return AutoPaid;
            return account.SupportsAutoPay ? NotAutoPaid : "";
        }
    }
}
