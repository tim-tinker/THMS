using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteGasPurchaseStore
    {
        private readonly GasPurchasesTable _table = new();

        public void InitializeSchema(SqliteConnection conn) => _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, GasPurchase purchase) =>
            _table.Upsert(conn, purchase);

        public IEnumerable<GasPurchase> GetRange(
            SqliteConnection conn,
            Guid vehicleId,
            DateTime start,
            DateTime end) =>
            _table.GetRange(conn, vehicleId, start, end);

        public IEnumerable<GasPurchase> GetWithMissingCost(SqliteConnection conn) =>
            _table.GetWithMissingCost(conn);

        public void UpdateCost(SqliteConnection conn, Guid purchaseId, decimal cost) =>
            _table.UpdateCost(conn, purchaseId, cost);
    }
}
