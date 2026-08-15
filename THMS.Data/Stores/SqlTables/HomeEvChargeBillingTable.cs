using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class HomeEvChargeBillingTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS HomeEvChargeBillings (
                SessionId TEXT PRIMARY KEY,
                SessionCost REAL NOT NULL,
                GridRate REAL NOT NULL,
                BillingCycleId TEXT NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Guid sessionId, HomeEvChargeBilling billing)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO HomeEvChargeBillings (SessionId, SessionCost, GridRate, BillingCycleId)
            VALUES (@SessionId, @SessionCost, @GridRate, @BillingCycleId)
            ON CONFLICT(SessionId) DO UPDATE SET
                SessionCost = excluded.SessionCost,
                GridRate = excluded.GridRate,
                BillingCycleId = excluded.BillingCycleId;
            ";

            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.Parameters.AddWithValue("@SessionCost", billing.SessionCost);
            cmd.Parameters.AddWithValue("@GridRate", billing.GridRate);
            cmd.Parameters.AddWithValue("@BillingCycleId", billing.BillingCycleId.ToString());
            cmd.ExecuteNonQuery();
        }

        public HomeEvChargeBilling? GetBySessionId(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT SessionCost, GridRate, BillingCycleId
            FROM HomeEvChargeBillings
            WHERE SessionId = @SessionId;
            ";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new HomeEvChargeBilling
            {
                SessionCost = (decimal)(double)reader.GetDouble(0),
                GridRate = (decimal)(double)reader.GetDouble(1),
                BillingCycleId = Guid.Parse(reader.GetString(2))
            };
        }

        public void Delete(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM HomeEvChargeBillings WHERE SessionId = @SessionId;";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
