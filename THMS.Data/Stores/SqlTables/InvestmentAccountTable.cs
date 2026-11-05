using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;

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
            AddColumnIfMissing(conn, "PositionValue", "REAL NOT NULL DEFAULT 0");
            AddColumnIfMissing(conn, "TracksPositions", "INTEGER NOT NULL DEFAULT 0");
            AddColumnIfMissing(conn, "BookOpenedOn", "TEXT");
            AddColumnIfMissing(conn, "QuoteAccess", "INTEGER NOT NULL DEFAULT 0");
        }

        public void Upsert(SqliteConnection conn, InvestmentAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO InvestmentAccounts (AccountId, CashBalance, PositionValue, TracksPositions, BookOpenedOn, QuoteAccess)
                VALUES (@AccountId, @CashBalance, @PositionValue, @TracksPositions, @BookOpenedOn, @QuoteAccess)
                ON CONFLICT(AccountId) DO UPDATE SET
                    CashBalance = excluded.CashBalance,
                    PositionValue = excluded.PositionValue,
                    TracksPositions = excluded.TracksPositions,
                    BookOpenedOn = excluded.BookOpenedOn,
                    QuoteAccess = excluded.QuoteAccess;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@CashBalance", account.CashBalance);
            cmd.Parameters.AddWithValue("@PositionValue", account.PositionValue);
            cmd.Parameters.AddWithValue("@TracksPositions", account.TracksPositions ? 1 : 0);
            cmd.Parameters.AddWithValue(
                "@BookOpenedOn",
                account.BookOpenedOn is DateTime opened
                    ? opened.ToString("yyyy-MM-dd")
                    : DBNull.Value);
            cmd.Parameters.AddWithValue("@QuoteAccess", (int)account.QuoteAccess);
            cmd.ExecuteNonQuery();
        }

        public InvestmentAccount? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT CashBalance, PositionValue, TracksPositions, BookOpenedOn, QuoteAccess
                FROM InvestmentAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new InvestmentAccount
            {
                CashBalance = (decimal)reader.GetDouble(0),
                PositionValue = (decimal)reader.GetDouble(1),
                TracksPositions = reader.GetInt32(2) != 0,
                BookOpenedOn = reader.IsDBNull(3)
                    ? null
                    : DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                QuoteAccess = reader.IsDBNull(4) ? QuoteAccess.Service : (QuoteAccess)reader.GetInt32(4)
            };
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM InvestmentAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void AddColumnIfMissing(SqliteConnection conn, string columnName, string definition)
        {
            if (ColumnExists(conn, columnName))
                return;

            using var add = conn.CreateCommand();
            add.CommandText = $"ALTER TABLE InvestmentAccounts ADD COLUMN {columnName} {definition};";
            add.ExecuteNonQuery();
        }

        private static void DropColumnIfExists(SqliteConnection conn, string columnName)
        {
            if (!ColumnExists(conn, columnName))
                return;

            using var drop = conn.CreateCommand();
            drop.CommandText = $"ALTER TABLE InvestmentAccounts DROP COLUMN {columnName};";
            drop.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection conn, string columnName)
        {
            using var pragma = conn.CreateCommand();
            pragma.CommandText = "PRAGMA table_info(InvestmentAccounts);";
            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
