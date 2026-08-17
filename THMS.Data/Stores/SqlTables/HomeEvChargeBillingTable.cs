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
                SessionCost REAL NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();

            DropColumnIfExists(conn, "GridRate");
            DropColumnIfExists(conn, "BillingCycleId");
        }

        public void Upsert(SqliteConnection conn, Guid sessionId, HomeEvChargeBilling billing)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO HomeEvChargeBillings (SessionId, SessionCost)
            VALUES (@SessionId, @SessionCost)
            ON CONFLICT(SessionId) DO UPDATE SET
                SessionCost = excluded.SessionCost;
            ";

            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.Parameters.AddWithValue("@SessionCost", billing.SessionCost);
            cmd.ExecuteNonQuery();
        }

        public HomeEvChargeBilling? GetBySessionId(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT SessionCost
            FROM HomeEvChargeBillings
            WHERE SessionId = @SessionId;
            ";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new HomeEvChargeBilling
            {
                SessionCost = (decimal)(double)reader.GetDouble(0)
            };
        }

        public void Delete(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM HomeEvChargeBillings WHERE SessionId = @SessionId;";
            cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void DropColumnIfExists(SqliteConnection conn, string columnName)
        {
            try
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE HomeEvChargeBillings DROP COLUMN {columnName};";
                alter.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Column already absent on new databases.
            }
        }
    }
}
