using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class LoanAccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS LoanAccounts (
                    AccountId TEXT PRIMARY KEY,
                    Principal REAL NOT NULL,
                    InterestRate REAL NOT NULL,
                    NextPaymentDate TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, LoanAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO LoanAccounts (AccountId, Principal, InterestRate, NextPaymentDate)
                VALUES (@AccountId, @Principal, @InterestRate, @NextPaymentDate)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Principal = excluded.Principal,
                    InterestRate = excluded.InterestRate,
                    NextPaymentDate = excluded.NextPaymentDate;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Principal", account.Principal);
            cmd.Parameters.AddWithValue("@InterestRate", account.InterestRate);
            cmd.Parameters.AddWithValue("@NextPaymentDate", account.NextPaymentDate);
            cmd.ExecuteNonQuery();
        }

        public (decimal Principal, decimal InterestRate, DateTime NextPaymentDate)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Principal, InterestRate, NextPaymentDate
                FROM LoanAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                reader.GetDateTime(2)
            );
        }
    }
}
