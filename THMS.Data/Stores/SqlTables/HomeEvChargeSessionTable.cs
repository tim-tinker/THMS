using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class HomeEvChargeSessionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS HomeEvChargeSessions (
                Id TEXT PRIMARY KEY,
                KwhDrawn REAL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, HomeEvChargeSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO HomeEvChargeSessions (Id, KwhDrawn)
            VALUES (@Id, @KwhDrawn)
            ON CONFLICT(Id) DO UPDATE SET
                KwhDrawn = excluded.KwhDrawn;
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@KwhDrawn", (object?)session.KwhDrawn ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Returns false if no row; otherwise sets kwhDrawn (which may itself be null).</summary>
        public bool TryGet(SqliteConnection conn, Guid sessionId, out decimal? kwhDrawn)
        {
            kwhDrawn = null;

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT KwhDrawn
            FROM HomeEvChargeSessions
            WHERE Id = @Id;
            ";
            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return false;

            kwhDrawn = reader.IsDBNull(0)
                ? null
                : (decimal)(double)reader.GetDouble(0);

            return true;
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM HomeEvChargeSessions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
