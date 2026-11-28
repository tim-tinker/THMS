using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Income;

namespace THMS.Data.Stores.SQLite
{
    public class SQLiteIncomeDataStore : IIncomeDataStore
    {
        private readonly string _connectionString;
        private readonly PayslipsTable _payslips = new();
        private readonly FormW2sTable _forms = new();

        public SQLiteIncomeDataStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = Open();
            _payslips.InitializeSchema(conn);
            _forms.InitializeSchema(conn);
        }

        public void Save(Payslip payslip)
        {
            ArgumentNullException.ThrowIfNull(payslip);
            if (payslip.Id == Guid.Empty)
                payslip.Id = Guid.NewGuid();
            payslip.ModifiedOn = DateTime.UtcNow;
            using var conn = Open();
            _payslips.Upsert(conn, payslip);
            FinanceDataRevision.NoteChanged();
        }

        public Payslip? Get(Guid id)
        {
            using var conn = Open();
            return _payslips.Get(conn, id);
        }

        public IReadOnlyList<Payslip> GetAll()
        {
            using var conn = Open();
            return _payslips.GetAll(conn);
        }

        public void Delete(Guid id)
        {
            using var conn = Open();
            _payslips.Delete(conn, id);
            FinanceDataRevision.NoteChanged();
        }

        public void Save(FormW2 form)
        {
            ArgumentNullException.ThrowIfNull(form);
            if (form.Id == Guid.Empty)
                form.Id = Guid.NewGuid();
            form.ModifiedOn = DateTime.UtcNow;
            using var conn = Open();
            _forms.Upsert(conn, form);
            FinanceDataRevision.NoteChanged();
        }

        public FormW2? GetFormW2(Guid id)
        {
            using var conn = Open();
            return _forms.Get(conn, id);
        }

        public IReadOnlyList<FormW2> GetFormW2s()
        {
            using var conn = Open();
            return _forms.GetAll(conn);
        }

        public void DeleteFormW2(Guid id)
        {
            using var conn = Open();
            _forms.Delete(conn, id);
            FinanceDataRevision.NoteChanged();
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
