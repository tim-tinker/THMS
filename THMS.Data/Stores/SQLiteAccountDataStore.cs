using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqliteStores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteAccountDataStore : IAccountDataStore
    {
        private readonly string _connectionString;
        private readonly SqliteAccountStore _accountStore = new();

        public SQLiteAccountDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = OpenConnection();
            _accountStore.InitializeSchema(conn);
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public void UpsertAccount(Account account)
        {
            using var conn = OpenConnection();
            _accountStore.Upsert(conn, account);
            FinanceDataRevision.NoteChanged();
        }

        public Account? GetAccount(string name)
        {
            using var conn = OpenConnection();
            return _accountStore.Get(conn, name);
        }

        public IEnumerable<Account> GetAllAccounts()
        {
            using var conn = OpenConnection();
            return _accountStore.GetAll(conn).ToList();
        }

        public void DeleteAccount(Guid id)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            _accountStore.Delete(conn, id);
            tx.Commit();
            FinanceDataRevision.NoteChanged();
        }

        public IReadOnlyList<PlaidItemSyncState> GetPlaidItemSyncStates()
        {
            using var conn = OpenConnection();
            return _accountStore.GetPlaidItemSyncStates(conn);
        }

        public PlaidItemSyncState? GetPlaidItemSyncState(string itemId)
        {
            using var conn = OpenConnection();
            return _accountStore.GetPlaidItemSyncState(conn, itemId);
        }

        public void UpsertPlaidItemSyncState(PlaidItemSyncState state)
        {
            using var conn = OpenConnection();
            _accountStore.UpsertPlaidItemSyncState(conn, state);
            FinanceDataRevision.NoteChanged();
        }

        public TransactionFileColumnMap? GetTransactionFileColumnMap(Guid accountId)
        {
            using var conn = OpenConnection();
            return _accountStore.GetTransactionFileColumnMap(conn, accountId);
        }

        public void UpsertTransactionFileColumnMap(TransactionFileColumnMap map)
        {
            using var conn = OpenConnection();
            _accountStore.UpsertTransactionFileColumnMap(conn, map);
        }
    }
}
