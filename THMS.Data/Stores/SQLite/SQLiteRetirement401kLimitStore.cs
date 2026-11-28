using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteRetirement401kLimitStore : IRetirement401kLimitStore
    {
        private readonly string _connectionString;
        private readonly Retirement401kLimitsTable _limits = new();

        public SQLiteRetirement401kLimitStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = Open();
            _limits.InitializeSchema(conn);
        }

        public void Save(Retirement401kLimits limits)
        {
            ArgumentNullException.ThrowIfNull(limits);
            using var conn = Open();
            _limits.Upsert(conn, limits);
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kLimits? Get(int taxYear)
        {
            using var conn = Open();
            return _limits.Get(conn, taxYear);
        }

        public Retirement401kLimits? GetLatest()
        {
            using var conn = Open();
            return _limits.GetLatest(conn);
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
