using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores
{
    public class InMemoryAccountDataStore : IAccountDataStore
    {
        private readonly InMemoryAccountStore _accountStore = new();

        public void UpsertAccount(Account account) =>
            _accountStore.Upsert(account);

        public Account? GetAccount(string name) =>
            _accountStore.Get(name);

        public IEnumerable<Account> GetAllAccounts() =>
            _accountStore.GetAll();

        public void DeleteAccount(Guid id) =>
            _accountStore.Delete(id);
    }
}
