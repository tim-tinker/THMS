using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.ViewModels.Finance
{
    public sealed class AccountRegisterRow
    {
        public Guid Id { get; init; }
        public required Account Account { get; init; }
        public string Name { get; init; } = "";
        public string Type { get; init; } = "";
        public string Institution { get; init; } = "";
        public string AccountNumber { get; init; } = "";
        public string StatementDate { get; init; } = AccountStatementListRow.NotApplicable;
        public string StatementBalance { get; init; } = AccountStatementListRow.NotApplicable;
        public string DueDate { get; init; } = AccountStatementListRow.NotApplicable;
        public string AmountDue { get; init; } = AccountStatementListRow.NotApplicable;

        public static AccountRegisterRow From(Account account, AccountStatement? latestStatement)
        {
            ArgumentNullException.ThrowIfNull(account);
            var statement = latestStatement is null ? null : AccountStatementListRow.From(latestStatement);
            return new AccountRegisterRow
            {
                Id = account.Id,
                Account = account,
                Name = account.Name,
                Type = AccountKinds.Of(account),
                Institution = account.Institution ?? "",
                AccountNumber = account.AccountNumber ?? "",
                StatementDate = statement?.StatementDate ?? AccountStatementListRow.NotApplicable,
                StatementBalance = statement?.StatementBalance ?? AccountStatementListRow.NotApplicable,
                DueDate = statement?.DueDate ?? AccountStatementListRow.NotApplicable,
                AmountDue = statement?.AmountDue ?? AccountStatementListRow.NotApplicable
            };
        }
    }
}
