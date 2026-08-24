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
                    CreditLimit REAL NOT NULL,
                    APR REAL NOT NULL,
                    StatementDate TEXT NOT NULL,
                    DueDate TEXT NOT NULL,
                    PostedBalance REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, CreditAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CreditAccounts
                (AccountId, CreditLimit, APR, StatementDate, DueDate, PostedBalance)
                VALUES
                (@AccountId, @CreditLimit, @APR, @StatementDate, @DueDate, @PostedBalance)
                ON CONFLICT(AccountId) DO UPDATE SET
                    CreditLimit = excluded.CreditLimit,
                    APR = excluded.APR,
                    StatementDate = excluded.StatementDate,
                    DueDate = excluded.DueDate,
                    PostedBalance = excluded.PostedBalance;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@CreditLimit", account.CreditLimit);
            cmd.Parameters.AddWithValue("@APR", account.APR);
            cmd.Parameters.AddWithValue("@StatementDate", account.StatementDate);
            cmd.Parameters.AddWithValue("@DueDate", account.DueDate);
            cmd.Parameters.AddWithValue("@PostedBalance", account.PostedBalance);
            cmd.ExecuteNonQuery();
        }

        public (decimal CreditLimit, decimal APR, DateTime StatementDate, DateTime DueDate, decimal PostedBalance)?
            Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT CreditLimit, APR, StatementDate, DueDate, PostedBalance
                FROM CreditAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                reader.GetDateTime(2),
                reader.GetDateTime(3),
                (decimal)(double)reader.GetDouble(4)
            );
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CreditAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
