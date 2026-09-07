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
                    StartingBalance REAL NOT NULL DEFAULT 0,
                    PostedBalance REAL NOT NULL,
                    OverdraftLimit REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();

            EnsureColumn(conn, "StartingBalance", "REAL NOT NULL DEFAULT 0");
        }

        public void Upsert(SqliteConnection conn, BankAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO BankAccounts (AccountId, StartingBalance, PostedBalance, OverdraftLimit)
                VALUES (@AccountId, @StartingBalance, @PostedBalance, @OverdraftLimit)
                ON CONFLICT(AccountId) DO UPDATE SET
                    StartingBalance = excluded.StartingBalance,
                    PostedBalance = excluded.PostedBalance,
                    OverdraftLimit = excluded.OverdraftLimit;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@StartingBalance", account.StartingBalance);
            cmd.Parameters.AddWithValue("@PostedBalance", account.PostedBalance);
            cmd.Parameters.AddWithValue("@OverdraftLimit", account.OverdraftLimit);
            cmd.ExecuteNonQuery();
        }

        public (decimal StartingBalance, decimal PostedBalance, decimal OverdraftLimit)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT StartingBalance, PostedBalance, OverdraftLimit
                FROM BankAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                (decimal)(double)reader.GetDouble(2)
            );
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM BankAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE BankAccounts ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }
    }
}
