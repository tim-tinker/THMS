using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class CreditAccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CreditAccounts (
                    AccountId TEXT PRIMARY KEY,
                    StartingBalance REAL NOT NULL DEFAULT 0,
                    CreditLimit REAL NOT NULL,
                    APR REAL NOT NULL,
                    StatementDate TEXT NOT NULL,
                    DueDate TEXT NOT NULL,
                    PostedBalance REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();

            EnsureColumn(conn, "StartingBalance", "REAL NOT NULL DEFAULT 0");
        }

        public void Upsert(SqliteConnection conn, CreditAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CreditAccounts
                (AccountId, StartingBalance, CreditLimit, APR, StatementDate, DueDate, PostedBalance)
                VALUES
                (@AccountId, @StartingBalance, @CreditLimit, @APR, @StatementDate, @DueDate, @PostedBalance)
                ON CONFLICT(AccountId) DO UPDATE SET
                    StartingBalance = excluded.StartingBalance,
                    CreditLimit = excluded.CreditLimit,
                    APR = excluded.APR,
                    StatementDate = excluded.StatementDate,
                    DueDate = excluded.DueDate,
                    PostedBalance = excluded.PostedBalance;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@StartingBalance", account.StartingBalance);
            cmd.Parameters.AddWithValue("@CreditLimit", account.CreditLimit);
            cmd.Parameters.AddWithValue("@APR", account.APR);
            cmd.Parameters.AddWithValue("@StatementDate", account.StatementDate);
            cmd.Parameters.AddWithValue("@DueDate", account.DueDate);
            cmd.Parameters.AddWithValue("@PostedBalance", account.PostedBalance);
            cmd.ExecuteNonQuery();
        }

        public (decimal StartingBalance, decimal CreditLimit, decimal APR, DateTime StatementDate, DateTime DueDate, decimal PostedBalance)?
            Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT StartingBalance, CreditLimit, APR, StatementDate, DueDate, PostedBalance
                FROM CreditAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                (decimal)(double)reader.GetDouble(2),
                reader.GetDateTime(3),
                reader.GetDateTime(4),
                (decimal)(double)reader.GetDouble(5)
            );
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CreditAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            if (ColumnExists(conn, columnName))
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE CreditAccounts ADD COLUMN {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection conn, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(CreditAccounts);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
