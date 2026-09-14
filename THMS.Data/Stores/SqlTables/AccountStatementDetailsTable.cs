using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class AccountStatementDetailsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AccountStatementDetails (
                    StatementId TEXT PRIMARY KEY,
                    EscrowBalance REAL NOT NULL DEFAULT 0,
                    StatementBalance REAL NOT NULL DEFAULT 0
                );";
            cmd.ExecuteNonQuery();
            DropColumnIfExists(conn, "PeriodStart");
            DropColumnIfExists(conn, "PrincipalBalance");
            DropColumnIfExists(conn, "InterestBalance");
            DropColumnIfExists(conn, "InterestEarned");
            DropColumnIfExists(conn, "InterestCharged");
            DropColumnIfExists(conn, "InterestAdded");
            DropColumnIfExists(conn, "Fees");
            DropColumnIfExists(conn, "BeginningBalance");
            DropColumnIfExists(conn, "Deposits");
            DropColumnIfExists(conn, "Withdrawals");
            DropColumnIfExists(conn, "Premium");
        }

        public void Upsert(SqliteConnection conn, Guid statementId, AccountStatement statement)
        {
            var details = FromStatement(statement);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AccountStatementDetails
                (StatementId, EscrowBalance, StatementBalance)
                VALUES
                (@StatementId, @EscrowBalance, @StatementBalance)
                ON CONFLICT(StatementId) DO UPDATE SET
                    EscrowBalance = excluded.EscrowBalance,
                    StatementBalance = excluded.StatementBalance;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.Parameters.AddWithValue("@EscrowBalance", details.EscrowBalance);
            cmd.Parameters.AddWithValue("@StatementBalance", details.StatementBalance);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AccountStatementDetails WHERE StatementId = @StatementId;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.ExecuteNonQuery();
        }

        public AccountStatementDetails Get(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT EscrowBalance, StatementBalance
                FROM AccountStatementDetails
                WHERE StatementId = @StatementId;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return AccountStatementDetails.Empty;

            return new AccountStatementDetails(
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1));
        }

        private static AccountStatementDetails FromStatement(AccountStatement statement) =>
            statement switch
            {
                BankStatement bank => new(0, bank.StatementBalance),
                LoanStatement loan => new(0, loan.StatementBalance),
                MortgageStatement mortgage => new(mortgage.EscrowBalance, mortgage.StatementBalance),
                CreditCardStatement card => new(0, card.StatementBalance),
                _ => AccountStatementDetails.Empty
            };

        private static void DropColumnIfExists(SqliteConnection conn, string columnName)
        {
            if (!ColumnExists(conn, columnName))
                return;

            try
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE AccountStatementDetails DROP COLUMN {columnName};";
                alter.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Older SQLite builds may not support DROP COLUMN.
            }
        }

        private static bool ColumnExists(SqliteConnection conn, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(AccountStatementDetails);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    public sealed record AccountStatementDetails(
        decimal EscrowBalance,
        decimal StatementBalance)
    {
        public static AccountStatementDetails Empty { get; } = new(0, 0);
    }
}
