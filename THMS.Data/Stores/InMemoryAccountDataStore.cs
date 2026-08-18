using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores
{
    public class InMemoryAccountDataStore : IAccountDataStore
    {
        private readonly InMemoryAccountStore _accountStore = new();

        public void UpsertAccount(Account account) =>
            _accountStore.Upsert(account);

        public Account? GetAccount(Guid id) =>
            _accountStore.Get(id);

        public IEnumerable<Account> GetAllAccounts() =>
            _accountStore.GetAll();
    }
}
