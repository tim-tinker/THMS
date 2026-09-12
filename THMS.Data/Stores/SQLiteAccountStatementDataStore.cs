using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqliteStores;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteAccountStatementDataStore : IAccountStatementDataStore
    {
        private readonly string _connectionString;
        private readonly SqliteAccountStatementStore _store = new();

        public SQLiteAccountStatementDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = OpenConnection();
            _store.InitializeSchema(conn);
        }

        public void Save(AccountStatement statement)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            _store.Save(conn, statement);
            tx.Commit();
        }

        public AccountStatement? Get(Guid id)
        {
            using var conn = OpenConnection();
            return _store.Get(conn, id);
        }

        public List<AccountStatement> GetForAccount(Guid accountId)
        {
            using var conn = OpenConnection();
            return _store.GetForAccount(conn, accountId);
        }

        public List<AccountStatement> GetUpcoming(DateTime asOf)
        {
            using var conn = OpenConnection();
            return _store.GetUpcoming(conn, asOf);
        }

        public void Delete(Guid id)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            _store.Delete(conn, id);
            tx.Commit();
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
