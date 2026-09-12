using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryAccountStore
    {
        private readonly List<Account> _items = new();

        public void Upsert(Account account)
        {
            var index = _items.FindIndex(a => a.Id == account.Id);
            if (index < 0)
                _items.Add(account);
            else
                _items[index] = account;
        }

        public Account? Get(string name) =>
            _items.FirstOrDefault(a => a.Name == name);

        public IEnumerable<Account> GetAll() =>
            _items.OrderBy(a => a.Name).ToList();

        public void Delete(Guid id) =>
            _items.RemoveAll(a => a.Id == id);
    }
}
