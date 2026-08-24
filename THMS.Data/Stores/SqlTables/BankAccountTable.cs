using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class BankAccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS BankAccounts (
                    AccountId TEXT PRIMARY KEY,
                    PostedBalance REAL NOT NULL,
                    OverdraftLimit REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, BankAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO BankAccounts (AccountId, PostedBalance, OverdraftLimit)
                VALUES (@AccountId, @PostedBalance, @OverdraftLimit)
                ON CONFLICT(AccountId) DO UPDATE SET
                    PostedBalance = excluded.PostedBalance,
                    OverdraftLimit = excluded.OverdraftLimit;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@PostedBalance", account.PostedBalance);
            cmd.Parameters.AddWithValue("@OverdraftLimit", account.OverdraftLimit);
            cmd.ExecuteNonQuery();
        }

        public (decimal PostedBalance, decimal OverdraftLimit)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT PostedBalance, OverdraftLimit
                FROM BankAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

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
            cmd.CommandText = "DELETE FROM BankAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
