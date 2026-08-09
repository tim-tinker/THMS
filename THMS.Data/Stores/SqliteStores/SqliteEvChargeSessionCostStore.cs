using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    /// <summary>
    /// Cost-focused access to EvChargeSessions (owned by vehicle schema).
    /// Uses SessionCost to match <see cref="SqlTables.EvChargeSessionTable"/>.
    /// </summary>
    public class SqliteEvChargeSessionCostStore
    {
        public void UpdateCost(SqliteConnection conn, Guid sessionId, decimal cost)
        {
            using var cmd = new SqliteCommand(@"
                UPDATE EvChargeSessions
                SET SessionCost = @Cost
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());
            cmd.Parameters.AddWithValue("@Cost", cost);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<EvChargeSession> GetWithMissingCost(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(@"
                SELECT Id, StartTime, EndTime, KwhAdded, IsHomeCharge, SessionCost
                FROM EvChargeSessions
                WHERE SessionCost = 0
                ORDER BY StartTime;", conn);

            using var reader = cmd.ExecuteReader();
            var list = new List<EvChargeSession>();

            while (reader.Read())
            {
                list.Add(new EvChargeSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    StartTime = reader.GetDateTime(1),
                    EndTime = reader.GetDateTime(2),
                    KwhAdded = reader.GetDecimal(3),
                    IsHomeCharge = reader.GetInt32(4) != 0,
                    SessionCost = reader.GetDecimal(5)
                });
            }

            return list;
        }
    }
}
