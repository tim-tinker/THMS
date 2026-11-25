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
                    TermMonths INTEGER NOT NULL,
                    SecuredAssetId TEXT,
                    LeaseAmount REAL NOT NULL DEFAULT 0,
                    Residual REAL NOT NULL DEFAULT 0,
                    MoneyFactor REAL NOT NULL DEFAULT 0,
                    GrossCapitalizedCost REAL NOT NULL DEFAULT 0,
                    CapitalizedCostReduction REAL NOT NULL DEFAULT 0,
                    AdjustedCapitalizedCost REAL NOT NULL DEFAULT 0,
                    RentCharge REAL NOT NULL DEFAULT 0
                );";
            cmd.ExecuteNonQuery();

            EnsureColumn(conn, "TermMonths", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "SecuredAssetId", "TEXT");
            EnsureColumn(conn, "LeaseAmount", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "Residual", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "MoneyFactor", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "GrossCapitalizedCost", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "CapitalizedCostReduction", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "AdjustedCapitalizedCost", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "RentCharge", "REAL NOT NULL DEFAULT 0");
        }

        public void Upsert(SqliteConnection conn, LoanAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO LoanAccounts (
                    AccountId, Principal, InterestRate, TermMonths, SecuredAssetId, LeaseAmount, Residual, MoneyFactor,
                    GrossCapitalizedCost, CapitalizedCostReduction, AdjustedCapitalizedCost, RentCharge)
                VALUES (
                    @AccountId, @Principal, @InterestRate, @TermMonths, @SecuredAssetId, @LeaseAmount, @Residual, @MoneyFactor,
                    @GrossCapitalizedCost, @CapitalizedCostReduction, @AdjustedCapitalizedCost, @RentCharge)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Principal = excluded.Principal,
                    InterestRate = excluded.InterestRate,
                    TermMonths = excluded.TermMonths,
                    SecuredAssetId = excluded.SecuredAssetId,
                    LeaseAmount = excluded.LeaseAmount,
                    Residual = excluded.Residual,
                    MoneyFactor = excluded.MoneyFactor,
                    GrossCapitalizedCost = excluded.GrossCapitalizedCost,
                    CapitalizedCostReduction = excluded.CapitalizedCostReduction,
                    AdjustedCapitalizedCost = excluded.AdjustedCapitalizedCost,
                    RentCharge = excluded.RentCharge;";

            var lease = account as LeaseAccount;
            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Principal", account.Principal);
            cmd.Parameters.AddWithValue("@InterestRate", account.InterestRate);
            cmd.Parameters.AddWithValue("@TermMonths", account.TermMonths);
            cmd.Parameters.AddWithValue("@SecuredAssetId", GuidText(account.SecuredAssetId));
            cmd.Parameters.AddWithValue("@LeaseAmount", lease?.LeaseAmount ?? 0m);
            cmd.Parameters.AddWithValue("@Residual", lease?.Residual ?? 0m);
            cmd.Parameters.AddWithValue("@MoneyFactor", lease?.MoneyFactor ?? 0m);
            cmd.Parameters.AddWithValue("@GrossCapitalizedCost", lease?.GrossCapitalizedCost ?? 0m);
            cmd.Parameters.AddWithValue("@CapitalizedCostReduction", lease?.CapitalizedCostReduction ?? 0m);
            cmd.Parameters.AddWithValue("@AdjustedCapitalizedCost", lease?.AdjustedCapitalizedCost ?? 0m);
            cmd.Parameters.AddWithValue("@RentCharge", lease?.RentCharge ?? 0m);
            cmd.ExecuteNonQuery();
        }

        public (decimal Principal, decimal InterestRate, int TermMonths, Guid? SecuredAssetId, decimal LeaseAmount, decimal Residual, decimal MoneyFactor, decimal GrossCapitalizedCost, decimal CapitalizedCostReduction, decimal AdjustedCapitalizedCost, decimal RentCharge)? Get(
            SqliteConnection conn,
            Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Principal, InterestRate, TermMonths, SecuredAssetId, LeaseAmount, Residual, MoneyFactor,
                       GrossCapitalizedCost, CapitalizedCostReduction, AdjustedCapitalizedCost, RentCharge
                FROM LoanAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                reader.GetInt32(2),
                ReadGuid(reader, 3),
                reader.IsDBNull(4) ? 0m : (decimal)(double)reader.GetDouble(4),
                reader.IsDBNull(5) ? 0m : (decimal)(double)reader.GetDouble(5),
                reader.IsDBNull(6) ? 0m : (decimal)(double)reader.GetDouble(6),
                reader.IsDBNull(7) ? 0m : (decimal)(double)reader.GetDouble(7),
                reader.IsDBNull(8) ? 0m : (decimal)(double)reader.GetDouble(8),
                reader.IsDBNull(9) ? 0m : (decimal)(double)reader.GetDouble(9),
                reader.IsDBNull(10) ? 0m : (decimal)(double)reader.GetDouble(10)
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
            if (ColumnExists(conn, columnName))
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE LoanAccounts ADD COLUMN {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection conn, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(LoanAccounts);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static object GuidText(Guid? id) =>
            id is Guid value && value != Guid.Empty ? value.ToString() : DBNull.Value;

        private static Guid? ReadGuid(SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));
    }
}
