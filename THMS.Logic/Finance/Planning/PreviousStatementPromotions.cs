using THMS.Data.Stores;
using THMS.Domain.Finance.Planning;

namespace THMS.Logic.Finance.Planning
{
    public static class PreviousStatementPromotions
    {
        public static List<PromotionalBalance> CopyForNewStatement(
            IAccountStatementDataStore statements,
            Guid accountId)
        {
            ArgumentNullException.ThrowIfNull(statements);

            var previous = statements.GetForAccount(accountId)
                .OfType<CreditCardStatement>()
                .OrderByDescending(statement => statement.StatementDate.Date)
                .ThenByDescending(statement => statement.DueDate.Date)
                .FirstOrDefault();
            if (previous is null)
                return [];

            return previous.Promotions
                .Select(promo => new PromotionalBalance
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    DateAcquired = promo.DateAcquired,
                    InitialAmount = promo.InitialAmount,
                    CurrentBalance = promo.CurrentBalance,
                    Deadline = promo.Deadline,
                    Type = promo.Type
                })
                .ToList();
        }
    }
}
