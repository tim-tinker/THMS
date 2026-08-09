using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Finance;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteCommercialChargeCostStore
    {
        private readonly CommercialChargeCostRecordsTable _table = new();

        public void InitializeSchema(SqliteConnection conn) => _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, CommercialChargeCostRecord record) =>
            _table.Upsert(conn, record);

        public IEnumerable<CommercialChargeCostRecord> GetRange(
            SqliteConnection conn,
            DateTime start,
            DateTime end) =>
            _table.GetRange(conn, start, end);

        public IEnumerable<CommercialChargeCostRecord> GetRangeByVendor(
            SqliteConnection conn,
            string vendor,
            DateTime start,
            DateTime end) =>
            _table.GetRangeByVendor(conn, vendor, start, end);
    }
}
