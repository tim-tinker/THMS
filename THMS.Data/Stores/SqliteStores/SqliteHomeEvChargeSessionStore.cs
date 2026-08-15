using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteHomeEvChargeSessionStore
    {
        private readonly HomeEvChargeSessionTable _table = new();

        public void InitializeSchema(SqliteConnection conn) =>
            _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, HomeEvChargeSession session) =>
            _table.Upsert(conn, session);

        public HomeEvChargeSession? Get(SqliteConnection conn, Guid sessionId)
        {
            if (!_table.TryGet(conn, sessionId, out var kwhDrawn))
                return null;

            return new HomeEvChargeSession
            {
                Id = sessionId,
                KwhDrawn = kwhDrawn
            };
        }

        public void Delete(SqliteConnection conn, Guid sessionId) =>
            _table.Delete(conn, sessionId);
    }
}
