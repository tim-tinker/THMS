using Microsoft.Data.Sqlite;

namespace THMS.Data.Stores
{
    /// <summary>
    /// One-time conversion of loan and mortgage principal, and of the signed activity on those
    /// accounts, into the credit-card ledger. Amount owed is stored negative. Payments are positive.
    /// Interest and loan funding are negative. Statement balances, escrow, and transfer magnitudes
    /// are left alone: a transfer's sign already comes from its direction.
    /// </summary>
    public static class LiabilityLedgerMigration
    {
        public const string PrincipalId = "LiabilityLedgerPrincipal";
        public const string ActivityId = "LiabilityLedgerActivity";

        private static readonly string[] PrincipalTables = ["LoanAccounts", "MortgageAccounts"];

        private static readonly string[] ActivityTables =
        [
            "LoanAccounts",
            "MortgageAccounts",
            "PostedTransactions",
            "FutureSingleTransactions",
            "RecurringSingleTransactionRules",
            "SplitTransactionRows"
        ];

        public static decimal ToLedgerAmount(decimal amount, string? category, string? description)
        {
            var kind = Classify(category, description);
            return kind switch
            {
                LiabilityActivityKind.Charge => -Math.Abs(amount),
                LiabilityActivityKind.Payment => Math.Abs(amount),
                _ => -amount
            };
        }

        public static void Apply(SqliteConnection conn)
        {
            ArgumentNullException.ThrowIfNull(conn);
            var flipPrincipal = TablesExist(conn, PrincipalTables) && !AlreadyApplied(conn, PrincipalId);
            var flipActivity = TablesExist(conn, ActivityTables) && !AlreadyApplied(conn, ActivityId);
            if (!flipPrincipal && !flipActivity)
                return;

            using var tx = conn.BeginTransaction();
            if (flipPrincipal)
            {
                NegatePrincipal(conn, "LoanAccounts");
                NegatePrincipal(conn, "MortgageAccounts");
                MarkApplied(conn, PrincipalId);
            }

            if (flipActivity)
            {
                NormalizeActivity(conn, "PostedTransactions", hasFinalPayment: false);
                NormalizeActivity(conn, "FutureSingleTransactions", hasFinalPayment: false);
                NormalizeActivity(conn, "RecurringSingleTransactionRules", hasFinalPayment: true);
                MarkApplied(conn, ActivityId);
            }

            tx.Commit();
        }

        private static LiabilityActivityKind Classify(string? category, string? description)
        {
            if (!string.IsNullOrWhiteSpace(category))
                return ClassifyText(category) ?? LiabilityActivityKind.PreserveEffect;

            return ClassifyText(description) ?? LiabilityActivityKind.PreserveEffect;
        }

        private static LiabilityActivityKind? ClassifyText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (text.Contains("interest", StringComparison.OrdinalIgnoreCase))
                return LiabilityActivityKind.Charge;
            if (text.Contains("funding", StringComparison.OrdinalIgnoreCase)
                || text.Contains("disbursement", StringComparison.OrdinalIgnoreCase))
                return LiabilityActivityKind.Charge;
            if (text.Contains("payment", StringComparison.OrdinalIgnoreCase)
                || text.Contains("principal", StringComparison.OrdinalIgnoreCase))
                return LiabilityActivityKind.Payment;
            return null;
        }

        private static void NegatePrincipal(SqliteConnection conn, string table)
        {
            var rows = new List<(string Id, decimal Principal)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT AccountId, Principal FROM {table};";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    rows.Add((reader.GetString(0), ReadDecimal(reader, 1)));
            }

            foreach (var (id, principal) in rows)
            {
                using var update = conn.CreateCommand();
                update.CommandText = $"UPDATE {table} SET Principal = @Principal WHERE AccountId = @Id;";
                update.Parameters.AddWithValue("@Id", id);
                update.Parameters.AddWithValue("@Principal", -principal);
                update.ExecuteNonQuery();
            }
        }

        private static void NormalizeActivity(SqliteConnection conn, string table, bool hasFinalPayment)
        {
            var rows = new List<ActivityRow>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT Id, Amount, Category, Description{(hasFinalPayment ? ", FinalPaymentAmount" : "")}
                    FROM {table}
                    WHERE AccountId IN (
                        SELECT AccountId FROM LoanAccounts
                        UNION
                        SELECT AccountId FROM MortgageAccounts);";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(new ActivityRow(
                        reader.GetString(0),
                        ReadDecimal(reader, 1),
                        reader.IsDBNull(2) ? null : reader.GetString(2),
                        reader.IsDBNull(3) ? null : reader.GetString(3),
                        hasFinalPayment && !reader.IsDBNull(4) ? ReadDecimal(reader, 4) : null));
                }
            }

            foreach (var row in rows)
            {
                var amount = ToLedgerAmount(row.Amount, row.Category, row.Description);
                decimal? finalPayment = row.FinalPayment is decimal final
                    ? ToLedgerAmount(final, row.Category, row.Description)
                    : null;
                if (amount == row.Amount && finalPayment == row.FinalPayment)
                    continue;

                using var update = conn.CreateCommand();
                update.CommandText = hasFinalPayment
                    ? $"UPDATE {table} SET Amount = @Amount, FinalPaymentAmount = @FinalPayment WHERE Id = @Id;"
                    : $"UPDATE {table} SET Amount = @Amount WHERE Id = @Id;";
                update.Parameters.AddWithValue("@Id", row.Id);
                update.Parameters.AddWithValue("@Amount", amount);
                if (hasFinalPayment)
                    update.Parameters.AddWithValue("@FinalPayment", (object?)finalPayment ?? DBNull.Value);
                update.ExecuteNonQuery();

                if (Math.Sign(row.Amount) != Math.Sign(amount))
                    NegateSplits(conn, row.Id);
            }
        }

        private static void NegateSplits(SqliteConnection conn, string parentId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE SplitTransactionRows SET Amount = -Amount WHERE ParentTransactionId = @Id;";
            cmd.Parameters.AddWithValue("@Id", parentId);
            cmd.ExecuteNonQuery();
        }

        private static decimal ReadDecimal(SqliteDataReader reader, int ordinal) =>
            Math.Round(Convert.ToDecimal(reader.GetValue(ordinal)), 2, MidpointRounding.AwayFromZero);

        private static bool TablesExist(SqliteConnection conn, IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Name;";
                cmd.Parameters.AddWithValue("@Name", name);
                if (cmd.ExecuteScalar() is null)
                    return false;
            }

            return true;
        }

        private static bool AlreadyApplied(SqliteConnection conn, string id)
        {
            if (!TablesExist(conn, ["SchemaMigrations"]))
                return false;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM SchemaMigrations WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);
            return cmd.ExecuteScalar() is not null;
        }

        private static void MarkApplied(SqliteConnection conn, string id)
        {
            using var create = conn.CreateCommand();
            create.CommandText = @"
                CREATE TABLE IF NOT EXISTS SchemaMigrations (
                    Id TEXT PRIMARY KEY,
                    AppliedAt TEXT NOT NULL
                );";
            create.ExecuteNonQuery();

            using var insert = conn.CreateCommand();
            insert.CommandText = "INSERT INTO SchemaMigrations (Id, AppliedAt) VALUES (@Id, @AppliedAt);";
            insert.Parameters.AddWithValue("@Id", id);
            insert.Parameters.AddWithValue("@AppliedAt", DateTime.UtcNow.ToString("o"));
            insert.ExecuteNonQuery();
        }

        private enum LiabilityActivityKind
        {
            Charge,
            Payment,
            PreserveEffect
        }

        private readonly record struct ActivityRow(
            string Id,
            decimal Amount,
            string? Category,
            string? Description,
            decimal? FinalPayment);
    }
}
