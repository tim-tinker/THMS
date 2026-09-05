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
                    TermMonths INTEGER NOT NULL
                );";
            cmd.ExecuteNonQuery();

            EnsureColumn(conn, "TermMonths", "INTEGER NOT NULL DEFAULT 0");
        }

        public void Upsert(SqliteConnection conn, LoanAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO LoanAccounts (AccountId, Principal, InterestRate, TermMonths)
                VALUES (@AccountId, @Principal, @InterestRate, @TermMonths)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Principal = excluded.Principal,
                    InterestRate = excluded.InterestRate,
                    TermMonths = excluded.TermMonths;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Principal", account.Principal);
            cmd.Parameters.AddWithValue("@InterestRate", account.InterestRate);
            cmd.Parameters.AddWithValue("@TermMonths", account.TermMonths);
            cmd.ExecuteNonQuery();
        }

        public (decimal Principal, decimal InterestRate, int TermMonths)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Principal, InterestRate, TermMonths
                FROM LoanAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                reader.GetInt32(2)
            );
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM LoanAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE LoanAccounts ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }
    }
}
