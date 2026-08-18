using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class InternalAccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS InternalAccounts (
                    AccountId TEXT PRIMARY KEY,
                    Purpose TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, InternalAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO InternalAccounts (AccountId, Purpose)
                VALUES (@AccountId, @Purpose)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Purpose = excluded.Purpose;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Purpose", account.Purpose);
            cmd.ExecuteNonQuery();
        }

        public string? GetPurpose(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Purpose
                FROM InternalAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? reader.GetString(0) : null;
        }
    }
}
