using Microsoft.Data.Sqlite;
using THMS.Domain.Transportation;

namespace THMS.Data.Stores.SqlTables
{
    public class CommercialEvChargeSessionTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS CommercialEvChargeSessions (
                Id TEXT PRIMARY KEY,
                KwhDrawn REAL NOT NULL,
                SessionCost REAL NOT NULL
            );
            ";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, CommercialEvChargeSession session)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            INSERT INTO CommercialEvChargeSessions (Id, KwhDrawn, SessionCost)
            VALUES (@Id, @KwhDrawn, @SessionCost)
            ON CONFLICT(Id) DO UPDATE SET
                KwhDrawn = excluded.KwhDrawn,
                SessionCost = excluded.SessionCost;
            ";

            cmd.Parameters.AddWithValue("@Id", session.Id.ToString());
            cmd.Parameters.AddWithValue("@KwhDrawn", session.KwhDrawn);
            cmd.Parameters.AddWithValue("@SessionCost", session.SessionCost);
            cmd.ExecuteNonQuery();
        }

        public (decimal KwhDrawn, decimal SessionCost)? GetById(SqliteConnection conn, Guid sessionId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
            @"
            SELECT KwhDrawn, SessionCost
            FROM CommercialEvChargeSessions
            WHERE Id = @Id;
            ";
            cmd.Parameters.AddWithValue("@Id", sessionId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1)
            );
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CommercialEvChargeSessions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
