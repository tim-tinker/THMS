using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteMaintenanceInvoiceStore
    {
        private readonly MaintenanceInvoiceTable _table = new();

        public void InitializeSchema(SqliteConnection conn) => _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, MaintenanceInvoiceRecord invoice) =>
            _table.Upsert(conn, invoice);

        public IEnumerable<MaintenanceInvoiceRecord> GetRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end) =>
            _table.GetRange(conn, vehicleId, start, end);

        public decimal GetTotalCost(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end) =>
            _table.GetTotalCost(conn, vehicleId, start, end);
    }
}
