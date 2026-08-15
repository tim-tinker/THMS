using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqliteStores
{
    /// <summary>
    /// Cost-focused access for commercial EV sessions missing a vendor cost.
    /// </summary>
    public class SqliteEvChargeSessionCostStore
    {
        public void UpdateCost(SqliteConnection conn, Guid sessionId, decimal cost)
        {
            using var cmd = new SqliteCommand(@"
                UPDATE CommercialEvChargeSessions
                SET SessionCost = @Cost
                WHERE Id = @Id;", conn);

            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());
            cmd.Parameters.AddWithValue("@Cost", cost);
            cmd.ExecuteNonQuery();
        }

        public IEnumerable<CommercialEvChargeSession> GetWithMissingCost(SqliteConnection conn)
        {
            using var cmd = new SqliteCommand(@"
                SELECT c.Id, c.KwhDrawn, c.SessionCost,
                       b.StartTime, m.EndTime
                FROM CommercialEvChargeSessions c
                LEFT JOIN BaseEvChargeSessions b ON b.Id = c.Id
                LEFT JOIN MileageRecords m ON m.Id = c.Id
                WHERE c.SessionCost = 0
                ORDER BY b.StartTime;", conn);

            using var reader = cmd.ExecuteReader();
            var list = new List<CommercialEvChargeSession>();

            while (reader.Read())
            {
                list.Add(new CommercialEvChargeSession
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    KwhDrawn = (decimal)(double)reader.GetDouble(1),
                    SessionCost = (decimal)(double)reader.GetDouble(2),
                    StartTime = reader.IsDBNull(3) ? default : reader.GetDateTime(3),
                    EndTime = reader.IsDBNull(4) ? default : reader.GetDateTime(4)
                });
            }

            return list;
        }
    }
}
