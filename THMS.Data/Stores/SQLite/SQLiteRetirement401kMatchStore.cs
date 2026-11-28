using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteRetirement401kMatchStore : IRetirement401kMatchStore
    {
        private readonly string _connectionString;
        private readonly Retirement401kMatchTable _match = new();
        private readonly Retirement401kEmploymentMatchTable _employment = new();

        public SQLiteRetirement401kMatchStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = Open();
            _match.InitializeSchema(conn);
            _employment.InitializeSchema(conn);
        }

        public void Save(Retirement401kMatchFormula formula)
        {
            ArgumentNullException.ThrowIfNull(formula);
            using var conn = Open();
            _match.Upsert(conn, formula);
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kMatchFormula? Get()
        {
            using var conn = Open();
            return _match.Get(conn);
        }

        public void SaveFor(Guid personId, string employer, Retirement401kMatchFormula formula)
        {
            ArgumentNullException.ThrowIfNull(formula);
            using var conn = Open();
            _employment.Upsert(conn, personId, employer, formula);
            FinanceDataRevision.NoteChanged();
        }

        public Retirement401kMatchFormula? GetFor(Guid personId, string employer)
        {
            using var conn = Open();
            return _employment.Get(conn, personId, employer);
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
