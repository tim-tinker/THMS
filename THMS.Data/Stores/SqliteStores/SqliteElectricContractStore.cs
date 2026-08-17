using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteElectricContractStore
    {
        private readonly ElectricContractsTable _table = new();

        public void InitializeSchema(SqliteConnection conn) => _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, ElectricContract contract) =>
            _table.Upsert(conn, contract);

        public ElectricContract? Get(SqliteConnection conn, Guid contractId) =>
            _table.GetById(conn, contractId);

        public ElectricContract? GetForDate(SqliteConnection conn, DateTime date) =>
            _table.GetForDate(conn, date);

        public IEnumerable<ElectricContract> GetRange(
            SqliteConnection conn,
            DateTime start,
            DateTime end) =>
            _table.GetRange(conn, start, end);

        public ElectricContract? GetLatest(SqliteConnection conn) =>
            _table.GetLatest(conn);
    }
}
