using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores
{
    public class InMemoryAccountDataStore : IAccountDataStore
    {
        private readonly InMemoryAccountStore _accountStore = new();
        private readonly Dictionary<string, PlaidItemSyncState> _itemSync = new(StringComparer.Ordinal);

        public void UpsertAccount(Account account)
        {
            _accountStore.Upsert(account);
            FinanceDataRevision.NoteChanged();
        }

        public Account? GetAccount(string name) =>
            _accountStore.Get(name);

        public IEnumerable<Account> GetAllAccounts() =>
            _accountStore.GetAll();

        public void DeleteAccount(Guid id)
        {
            _accountStore.Delete(id);
            FinanceDataRevision.NoteChanged();
        }

        public IReadOnlyList<PlaidItemSyncState> GetPlaidItemSyncStates() => _itemSync.Values.ToList();

        public PlaidItemSyncState? GetPlaidItemSyncState(string itemId) =>
            _itemSync.TryGetValue(itemId, out var state) ? state : null;

        public void UpsertPlaidItemSyncState(PlaidItemSyncState state)
        {
            _itemSync[state.ItemId] = state;
            FinanceDataRevision.NoteChanged();
        }
    }
}
