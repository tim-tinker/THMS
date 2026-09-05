using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class InvestmentAccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS InvestmentAccounts (
                    AccountId TEXT PRIMARY KEY,
                    CashBalance REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();

            DropColumnIfExists(conn, "MarketValue");
        }

        public void Upsert(SqliteConnection conn, InvestmentAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO InvestmentAccounts (AccountId, CashBalance)
                VALUES (@AccountId, @CashBalance)
                ON CONFLICT(AccountId) DO UPDATE SET
                    CashBalance = excluded.CashBalance;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@CashBalance", account.CashBalance);
            cmd.ExecuteNonQuery();
        }

        public decimal? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT CashBalance
                FROM InvestmentAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (decimal)(double)reader.GetDouble(0);
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM InvestmentAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void DropColumnIfExists(SqliteConnection conn, string columnName)
        {
            var exists = false;
            using (var pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA table_info(InvestmentAccounts);";
                using var reader = pragma.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
                return;

            using var drop = conn.CreateCommand();
            drop.CommandText = $"ALTER TABLE InvestmentAccounts DROP COLUMN {columnName};";
            drop.ExecuteNonQuery();
        }
    }
}
