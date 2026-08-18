using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqliteStores;
using THMS.Domain.Finance.Accounts;

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
        }

        public Account? GetAccount(Guid id)
        {
            using var conn = OpenConnection();
            return _accountStore.Get(conn, id);
        }

        public IEnumerable<Account> GetAllAccounts()
        {
            using var conn = OpenConnection();
            return _accountStore.GetAll(conn).ToList();
        }
    }
}
