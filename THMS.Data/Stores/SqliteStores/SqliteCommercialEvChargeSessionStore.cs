using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SqlTables;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    public class SqliteCommercialEvChargeSessionStore
    {
        private readonly CommercialEvChargeSessionTable _table = new();

        public void InitializeSchema(SqliteConnection conn) =>
            _table.InitializeSchema(conn);

        public void Upsert(SqliteConnection conn, CommercialEvChargeSession session) =>
            _table.Upsert(conn, session);

        public CommercialEvChargeSession? Get(SqliteConnection conn, Guid sessionId)
        {
            var row = _table.GetById(conn, sessionId);
            if (row == null)
                return null;

            return new CommercialEvChargeSession
            {
                Id = sessionId,
                KwhDrawn = row.Value.KwhDrawn,
                SessionCost = row.Value.SessionCost
            };
        }

        public void Delete(SqliteConnection conn, Guid sessionId) =>
            _table.Delete(conn, sessionId);
    }
}
