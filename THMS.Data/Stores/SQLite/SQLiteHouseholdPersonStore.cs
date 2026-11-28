using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Household;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteHouseholdPersonStore : IHouseholdPersonStore
    {
        private readonly string _connectionString;
        private readonly HouseholdPersonTable _people = new();

        public SQLiteHouseholdPersonStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = Open();
            _people.InitializeSchema(conn);
        }

        public void Save(HouseholdPerson person)
        {
            ArgumentNullException.ThrowIfNull(person);
            if (person.Id == Guid.Empty)
                person.Id = Guid.NewGuid();
            person.ModifiedOn = DateTime.UtcNow;
            using var conn = Open();
            _people.Upsert(conn, person);
            FinanceDataRevision.NoteChanged();
        }

        public HouseholdPerson? Get(Guid id)
        {
            using var conn = Open();
            return _people.Get(conn, id);
        }

        public IReadOnlyList<HouseholdPerson> GetAll()
        {
            using var conn = Open();
            return _people.GetAll(conn);
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
