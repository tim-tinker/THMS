using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Finance.Planning
{
    public static class NextStatementDates
    {
        public static (DateTime StatementDate, DateTime DueDate) Suggest(
            IEnumerable<AccountStatement> existing,
            DateTime today)
        {
            var previous = existing
                .OrderByDescending(statement => statement.StatementDate.Date)
                .ThenByDescending(statement => statement.DueDate.Date)
                .FirstOrDefault();
            if (previous is null)
                return (today.Date.AddDays(-15), today.Date.AddDays(15));

            return (
                previous.StatementDate.Date.AddMonths(1),
                previous.DueDate.Date.AddMonths(1));
        }
    }
}
