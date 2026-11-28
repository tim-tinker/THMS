using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteRetirement401kBirthYearStore : IRetirement401kBirthYearStore
    {
        private readonly string _connectionString;
        private readonly Retirement401kBirthYearTable _birthYear = new();

        public SQLiteRetirement401kBirthYearStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = Open();
            _birthYear.InitializeSchema(conn);
        }

        public void Save(Retirement401kBirthYear birthYear)
        {
            ArgumentNullException.ThrowIfNull(birthYear);
            using var conn = Open();
            _birthYear.Upsert(conn, birthYear);
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kBirthYear? Get()
        {
            using var conn = Open();
            return _birthYear.Get(conn);
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
