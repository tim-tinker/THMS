using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteHomeEvChargeAttributionStore
    {
        private readonly HomeEvChargeAttributionTable _table = new();

        public void InitializeSchema(SqliteConnection conn) =>
            _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, Guid sessionId, HomeEvChargeAttribution attribution) =>
            _table.Upsert(conn, sessionId, attribution);

        public HomeEvChargeAttribution? Get(SqliteConnection conn, Guid sessionId) =>
            _table.GetBySessionId(conn, sessionId);

        public void Delete(SqliteConnection conn, Guid sessionId) =>
            _table.Delete(conn, sessionId);
    }
}
