using THMS.Data.Stores.InMemoryStores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores
{
    public class InMemoryAccountDataStore : IAccountDataStore
    {
        private readonly InMemoryAccountStore _accountStore = new();
        private readonly Dictionary<string, PlaidItemSyncState> _itemSync = new(StringComparer.Ordinal);
        private readonly Dictionary<Guid, TransactionFileColumnMap> _transactionFileMaps = new();

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
            _transactionFileMaps.Remove(id);
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

        public TransactionFileColumnMap? GetTransactionFileColumnMap(Guid accountId) =>
            _transactionFileMaps.TryGetValue(accountId, out var map) ? map : null;

        public void UpsertTransactionFileColumnMap(TransactionFileColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(map);
            _transactionFileMaps[map.AccountId] = map;
        }
    }
}
