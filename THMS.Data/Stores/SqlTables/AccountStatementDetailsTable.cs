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
                    PrincipalBalance REAL NOT NULL DEFAULT 0,
                    InterestBalance REAL NOT NULL DEFAULT 0,
                    EscrowBalance REAL NOT NULL DEFAULT 0,
                    InterestCharged REAL NOT NULL DEFAULT 0,
                    Fees REAL NOT NULL DEFAULT 0,
                    StatementBalance REAL NOT NULL DEFAULT 0,
                    Premium REAL NOT NULL DEFAULT 0,
                    BeginningBalance REAL NOT NULL DEFAULT 0,
                    Deposits REAL NOT NULL DEFAULT 0,
                    Withdrawals REAL NOT NULL DEFAULT 0,
                    InterestEarned REAL NOT NULL DEFAULT 0,
                    PeriodStart TEXT
                );";
            cmd.ExecuteNonQuery();
            EnsureColumn(conn, "BeginningBalance", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "Deposits", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "Withdrawals", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "InterestEarned", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "PeriodStart", "TEXT");
        }

        public void Upsert(SqliteConnection conn, Guid statementId, AccountStatement statement)
        {
            var details = FromStatement(statement);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AccountStatementDetails
                (StatementId, PrincipalBalance, InterestBalance, EscrowBalance, InterestCharged, Fees, StatementBalance, Premium,
                 BeginningBalance, Deposits, Withdrawals, InterestEarned, PeriodStart)
                VALUES
                (@StatementId, @PrincipalBalance, @InterestBalance, @EscrowBalance, @InterestCharged, @Fees, @StatementBalance, @Premium,
                 @BeginningBalance, @Deposits, @Withdrawals, @InterestEarned, @PeriodStart)
                ON CONFLICT(StatementId) DO UPDATE SET
                    PrincipalBalance = excluded.PrincipalBalance,
                    InterestBalance = excluded.InterestBalance,
                    EscrowBalance = excluded.EscrowBalance,
                    InterestCharged = excluded.InterestCharged,
                    Fees = excluded.Fees,
                    StatementBalance = excluded.StatementBalance,
                    Premium = excluded.Premium,
                    BeginningBalance = excluded.BeginningBalance,
                    Deposits = excluded.Deposits,
                    Withdrawals = excluded.Withdrawals,
                    InterestEarned = excluded.InterestEarned,
                    PeriodStart = excluded.PeriodStart;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.Parameters.AddWithValue("@PrincipalBalance", details.PrincipalBalance);
            cmd.Parameters.AddWithValue("@InterestBalance", details.InterestBalance);
            cmd.Parameters.AddWithValue("@EscrowBalance", details.EscrowBalance);
            cmd.Parameters.AddWithValue("@InterestCharged", details.InterestCharged);
            cmd.Parameters.AddWithValue("@Fees", details.Fees);
            cmd.Parameters.AddWithValue("@StatementBalance", details.StatementBalance);
            cmd.Parameters.AddWithValue("@Premium", details.Premium);
            cmd.Parameters.AddWithValue("@BeginningBalance", details.BeginningBalance);
            cmd.Parameters.AddWithValue("@Deposits", details.Deposits);
            cmd.Parameters.AddWithValue("@Withdrawals", details.Withdrawals);
            cmd.Parameters.AddWithValue("@InterestEarned", details.InterestEarned);
            cmd.Parameters.AddWithValue("@PeriodStart", (object?)details.PeriodStart ?? DBNull.Value);
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
                SELECT PrincipalBalance, InterestBalance, EscrowBalance, InterestCharged, Fees, StatementBalance, Premium,
                       BeginningBalance, Deposits, Withdrawals, InterestEarned, PeriodStart
                FROM AccountStatementDetails
                WHERE StatementId = @StatementId;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return AccountStatementDetails.Empty;

            return new AccountStatementDetails(
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                (decimal)(double)reader.GetDouble(2),
                (decimal)(double)reader.GetDouble(3),
                (decimal)(double)reader.GetDouble(4),
                (decimal)(double)reader.GetDouble(5),
                (decimal)(double)reader.GetDouble(6),
                (decimal)(double)reader.GetDouble(7),
                (decimal)(double)reader.GetDouble(8),
                (decimal)(double)reader.GetDouble(9),
                (decimal)(double)reader.GetDouble(10),
                reader.IsDBNull(11) ? null : reader.GetDateTime(11));
        }

        private static AccountStatementDetails FromStatement(AccountStatement statement) =>
            statement switch
            {
                BankStatement bank => new(
                    0,
                    0,
                    0,
                    0,
                    bank.Fees,
                    bank.EndingBalance,
                    0,
                    bank.BeginningBalance,
                    bank.Deposits,
                    bank.Withdrawals,
                    bank.InterestEarned,
                    bank.PeriodStart == default ? null : bank.PeriodStart),
                LoanStatement loan => new(
                    loan.PrincipalBalance,
                    loan.InterestBalance,
                    0,
                    loan.InterestCharged,
                    loan.Fees,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    null),
                MortgageStatement mortgage => new(
                    mortgage.PrincipalBalance,
                    mortgage.InterestBalance,
                    mortgage.EscrowBalance,
                    mortgage.InterestCharged,
                    mortgage.Fees,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    null),
                CreditCardStatement card => new(
                    0,
                    0,
                    0,
                    card.InterestCharged,
                    card.Fees,
                    card.StatementBalance,
                    0,
                    0,
                    0,
                    0,
                    0,
                    null),
                InsuranceStatement insurance => new(
                    0,
                    0,
                    0,
                    0,
                    insurance.Fees,
                    0,
                    insurance.Premium,
                    0,
                    0,
                    0,
                    0,
                    null),
                _ => AccountStatementDetails.Empty
            };

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            if (ColumnExists(conn, columnName))
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE AccountStatementDetails ADD COLUMN {columnName} {columnDef};";
            alter.ExecuteNonQuery();
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
        decimal PrincipalBalance,
        decimal InterestBalance,
        decimal EscrowBalance,
        decimal InterestCharged,
        decimal Fees,
        decimal StatementBalance,
        decimal Premium,
        decimal BeginningBalance,
        decimal Deposits,
        decimal Withdrawals,
        decimal InterestEarned,
        DateTime? PeriodStart)
    {
        public static AccountStatementDetails Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null);
    }
}
