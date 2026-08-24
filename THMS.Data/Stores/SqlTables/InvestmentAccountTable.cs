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
                    CashBalance REAL NOT NULL,
                    MarketValue REAL NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, InvestmentAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO InvestmentAccounts (AccountId, CashBalance, MarketValue)
                VALUES (@AccountId, @CashBalance, @MarketValue)
                ON CONFLICT(AccountId) DO UPDATE SET
                    CashBalance = excluded.CashBalance,
                    MarketValue = excluded.MarketValue;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@CashBalance", account.CashBalance);
            cmd.Parameters.AddWithValue("@MarketValue", account.MarketValue);
            cmd.ExecuteNonQuery();
        }

        public (decimal CashBalance, decimal MarketValue)? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT CashBalance, MarketValue
                FROM InvestmentAccounts
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
            cmd.CommandText = "DELETE FROM InvestmentAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
