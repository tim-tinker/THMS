using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteHomeEvChargeBillingStore
    {
        private readonly HomeEvChargeBillingTable _table = new();

        public void InitializeSchema(SqliteConnection conn) =>
            _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, Guid sessionId, HomeEvChargeBilling billing) =>
            _table.Upsert(conn, sessionId, billing);

        public HomeEvChargeBilling? Get(SqliteConnection conn, Guid sessionId) =>
            _table.GetBySessionId(conn, sessionId);

        public void Delete(SqliteConnection conn, Guid sessionId) =>
            _table.Delete(conn, sessionId);
    }
}
