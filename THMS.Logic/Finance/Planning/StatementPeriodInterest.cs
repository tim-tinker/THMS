using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;

namespace THMS.Logic.Finance.Planning
{
    public static class StatementPeriodInterest
    {
        public static bool Applies(AccountStatement statement) =>
            statement is BankStatement or CreditCardStatement or LoanStatement or MortgageStatement;

        public static decimal Compute(
            AccountStatement statement,
            IEnumerable<AccountStatement> accountStatements,
            IEnumerable<PostedTransaction> posted)
        {
            ArgumentNullException.ThrowIfNull(statement);
            if (!Applies(statement))
                return 0;

            var previousDate = PreviousStatementDate(statement, accountStatements);
            return posted
                .Where(transaction =>
                    transaction.AccountId == statement.AccountId &&
                    InPeriod(transaction.Date, statement.StatementDate, previousDate))
                .Sum(InterestAmount);
        }

        private static DateTime? PreviousStatementDate(
            AccountStatement statement,
            IEnumerable<AccountStatement> accountStatements)
        {
            DateTime? previous = null;
            foreach (var other in accountStatements)
            {
                if (other.AccountId != statement.AccountId)
                    continue;
                if (other.StatementDate.Date >= statement.StatementDate.Date)
                    continue;
                if (previous is null || other.StatementDate.Date > previous.Value)
                    previous = other.StatementDate.Date;
            }

            return previous;
        }

        private static bool InPeriod(DateTime date, DateTime statementDate, DateTime? previousStatementDate)
        {
            if (date.Date > statementDate.Date)
                return false;
            if (previousStatementDate is DateTime previous && date.Date <= previous.Date)
                return false;
            return true;
        }

        private static decimal InterestAmount(PostedTransaction transaction)
        {
            if (transaction.HasSplits)
                return transaction.Splits.Where(IsInterest).Sum(split => split.Amount);

            return IsInterest(transaction.CategoryId, transaction.Category) ? transaction.Amount : 0;
        }

        private static bool IsInterest(SplitTransactionRow split) =>
            IsInterest(split.CategoryId, split.Category);

        private static bool IsInterest(Guid? categoryId, string? category)
        {
            if (categoryId == DefaultExpenseCategories.InterestId)
                return true;

            return !string.IsNullOrWhiteSpace(category) &&
                   DefaultExpenseCategories.CanonicalName(category) == DefaultExpenseCategories.Interest;
        }
    }
}
