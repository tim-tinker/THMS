using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance.Billing;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteElectricUtilityBillStore
    {
        private readonly ElectricUtilityBillsTable _table = new();

        public void InitializeSchema(SqliteConnection conn) => _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, ElectricUtilityBill bill) =>
            _table.Upsert(conn, bill);

        public ElectricUtilityBill? Get(SqliteConnection conn, Guid billId) =>
            _table.GetById(conn, billId);

        public ElectricUtilityBill? GetForDate(SqliteConnection conn, DateTime date) =>
            _table.GetForDate(conn, date);

        public IEnumerable<ElectricUtilityBill> GetRange(
            SqliteConnection conn,
            DateTime start,
            DateTime end) =>
            _table.GetRange(conn, start, end);

        public ElectricUtilityBill? GetLatest(SqliteConnection conn) =>
            _table.GetLatest(conn);
    }
}
