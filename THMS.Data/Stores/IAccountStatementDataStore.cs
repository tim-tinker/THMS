using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores
{
    public interface IAccountStatementDataStore
    {
        void Save(AccountStatement statement);
        AccountStatement? Get(Guid id);
        List<AccountStatement> GetForAccount(Guid accountId);
        List<AccountStatement> GetUpcoming(DateTime asOf);
        void Delete(Guid id);
    }
}
