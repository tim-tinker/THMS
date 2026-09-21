using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Finance.Planning
{
    public static class PayableStatements
    {
        public static AccountStatement? Latest(IEnumerable<AccountStatement> statements)
        {
            var latest = statements
                .Where(statement => statement is not BankStatement)
                .OrderByDescending(statement => statement.StatementDate)
                .ThenByDescending(statement => statement.DueDate)
                .FirstOrDefault();
            return latest is { AmountDue: > 0 } ? latest : null;
        }
    }
}
