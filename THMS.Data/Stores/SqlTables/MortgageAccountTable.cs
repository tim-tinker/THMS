using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class MortgageAccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MortgageAccounts (
                    AccountId TEXT PRIMARY KEY,
                    Principal REAL NOT NULL,
                    InterestRate REAL NOT NULL,
                    TermMonths INTEGER NOT NULL,
                    NextPaymentDate TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, MortgageAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO MortgageAccounts
                (AccountId, Principal, InterestRate, TermMonths, NextPaymentDate)
                VALUES
                (@AccountId, @Principal, @InterestRate, @TermMonths, @NextPaymentDate)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Principal = excluded.Principal,
                    InterestRate = excluded.InterestRate,
                    TermMonths = excluded.TermMonths,
                    NextPaymentDate = excluded.NextPaymentDate;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Principal", account.Principal);
            cmd.Parameters.AddWithValue("@InterestRate", account.InterestRate);
            cmd.Parameters.AddWithValue("@TermMonths", account.TermMonths);
            cmd.Parameters.AddWithValue("@NextPaymentDate", account.NextPaymentDate);
            cmd.ExecuteNonQuery();
        }

        public (decimal Principal, decimal InterestRate, int TermMonths, DateTime NextPaymentDate)?
            Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Principal, InterestRate, TermMonths, NextPaymentDate
                FROM MortgageAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                reader.GetInt32(2),
                reader.GetDateTime(3)
            );
        }
    }
}
