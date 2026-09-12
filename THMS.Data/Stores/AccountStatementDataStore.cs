using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public class AccountStatementDataStore : IAccountStatementDataStore
    {
        private readonly List<AccountStatement> _statements = [];

        public void Save(AccountStatement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);
            if (statement.Id == Guid.Empty)
                statement.Id = Guid.NewGuid();

            if (statement is CreditCardStatement card)
            {
                foreach (var promo in card.Promotions)
                {
                    if (promo.Id == Guid.Empty)
                        promo.Id = Guid.NewGuid();
                    if (promo.AccountId == Guid.Empty)
                        promo.AccountId = statement.AccountId;
                }
            }

            var index = _statements.FindIndex(s => s.Id == statement.Id);
            if (index >= 0)
                _statements[index] = statement;
            else
                _statements.Add(statement);
        }

        public AccountStatement? Get(Guid id) =>
            _statements.FirstOrDefault(s => s.Id == id);

        public List<AccountStatement> GetForAccount(Guid accountId) =>
            _statements.Where(s => s.AccountId == accountId).OrderBy(s => s.DueDate).ToList();

        public List<AccountStatement> GetUpcoming(DateTime asOf) =>
            _statements.Where(s => s.DueDate.Date >= asOf.Date).OrderBy(s => s.DueDate).ToList();

        public void Delete(Guid id) =>
            _statements.RemoveAll(s => s.Id == id);
    }
}
