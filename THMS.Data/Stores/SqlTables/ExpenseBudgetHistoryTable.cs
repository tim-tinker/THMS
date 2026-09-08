using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class ExpenseBudgetHistoryTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ExpenseBudgetHistory (
                        Id TEXT PRIMARY KEY,
                        BudgetRuleId TEXT NOT NULL,
                        PeriodStart TEXT NOT NULL,
                        PeriodEnd TEXT NOT NULL,
                        StartingBalance REAL NOT NULL DEFAULT 0,
                        BudgetAmount REAL NOT NULL,
                        ActualExpenses REAL NOT NULL,
                        Remaining REAL NOT NULL,
                        EndingBalance REAL NOT NULL DEFAULT 0,
                        RecommendedAmount REAL NOT NULL,
                        IsClosed INTEGER NOT NULL
                    );";
                cmd.ExecuteNonQuery();
            }

            EnsureColumn(conn, "StartingBalance", "REAL NOT NULL DEFAULT 0");
            EnsureColumn(conn, "EndingBalance", "REAL NOT NULL DEFAULT 0");
            SeedHistoryFromLegacyRules(conn);
            SeedMissingOpenPeriods(conn);
            DropLegacyTables(conn);
        }

        public void Add(SqliteConnection conn, ExpenseBudgetHistory history)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ExpenseBudgetHistory
                (Id, BudgetRuleId, PeriodStart, PeriodEnd, StartingBalance, BudgetAmount, ActualExpenses, Remaining, EndingBalance, RecommendedAmount, IsClosed)
                VALUES
                (@Id, @BudgetRuleId, @PeriodStart, @PeriodEnd, @StartingBalance, @BudgetAmount, @ActualExpenses, @Remaining, @EndingBalance, @RecommendedAmount, @IsClosed);";
            Bind(cmd, history);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, ExpenseBudgetHistory history)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE ExpenseBudgetHistory SET
                    BudgetRuleId = @BudgetRuleId,
                    PeriodStart = @PeriodStart,
                    PeriodEnd = @PeriodEnd,
                    StartingBalance = @StartingBalance,
                    BudgetAmount = @BudgetAmount,
                    ActualExpenses = @ActualExpenses,
                    Remaining = @Remaining,
                    EndingBalance = @EndingBalance,
                    RecommendedAmount = @RecommendedAmount,
                    IsClosed = @IsClosed
                WHERE Id = @Id;";
            Bind(cmd, history);
            cmd.ExecuteNonQuery();
        }

        public void DeleteByRule(SqliteConnection conn, Guid budgetRuleId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ExpenseBudgetHistory WHERE BudgetRuleId = @BudgetRuleId;";
            cmd.Parameters.AddWithValue("@BudgetRuleId", budgetRuleId.ToString());
            cmd.ExecuteNonQuery();
        }

        public ExpenseBudgetHistory? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM ExpenseBudgetHistory WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<ExpenseBudgetHistory> GetByRule(SqliteConnection conn, Guid budgetRuleId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseBudgetHistory
                WHERE BudgetRuleId = @BudgetRuleId
                ORDER BY PeriodStart DESC;";
            cmd.Parameters.AddWithValue("@BudgetRuleId", budgetRuleId.ToString());
            return ReadAll(cmd);
        }

        public ExpenseBudgetHistory? GetActive(SqliteConnection conn, Guid budgetRuleId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseBudgetHistory
                WHERE BudgetRuleId = @BudgetRuleId AND IsClosed = 0
                ORDER BY PeriodStart DESC
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@BudgetRuleId", budgetRuleId.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<ExpenseBudgetHistory> GetForPeriod(
            SqliteConnection conn,
            Guid budgetRuleId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            return GetByRule(conn, budgetRuleId)
                .Where(h => h.PeriodStart <= periodEnd && h.PeriodEnd >= periodStart);
        }

        private const string SelectColumns =
            @"SELECT Id, BudgetRuleId, PeriodStart, PeriodEnd, StartingBalance, BudgetAmount, ActualExpenses, Remaining, EndingBalance, RecommendedAmount, IsClosed";

        private static void Bind(SqliteCommand cmd, ExpenseBudgetHistory history)
        {
            cmd.Parameters.AddWithValue("@Id", history.Id.ToString());
            cmd.Parameters.AddWithValue("@BudgetRuleId", history.BudgetRuleId.ToString());
            cmd.Parameters.AddWithValue("@PeriodStart", history.PeriodStart);
            cmd.Parameters.AddWithValue("@PeriodEnd", history.PeriodEnd);
            cmd.Parameters.AddWithValue("@StartingBalance", history.StartingBalance);
            cmd.Parameters.AddWithValue("@BudgetAmount", history.BudgetAmount);
            cmd.Parameters.AddWithValue("@ActualExpenses", history.ActualExpenses);
            cmd.Parameters.AddWithValue("@Remaining", history.Remaining);
            cmd.Parameters.AddWithValue("@EndingBalance", history.EndingBalance);
            cmd.Parameters.AddWithValue("@RecommendedAmount", history.RecommendedAmount);
            cmd.Parameters.AddWithValue("@IsClosed", history.IsClosed ? 1 : 0);
        }

        private static ExpenseBudgetHistory Read(SqliteDataReader reader)
        {
            return new ExpenseBudgetHistory
            {
                Id = Guid.Parse(reader.GetString(0)),
                BudgetRuleId = Guid.Parse(reader.GetString(1)),
                PeriodStart = reader.GetDateTime(2),
                PeriodEnd = reader.GetDateTime(3),
                StartingBalance = (decimal)(double)reader.GetDouble(4),
                BudgetAmount = (decimal)(double)reader.GetDouble(5),
                ActualExpenses = (decimal)(double)reader.GetDouble(6),
                Remaining = (decimal)(double)reader.GetDouble(7),
                EndingBalance = (decimal)(double)reader.GetDouble(8),
                RecommendedAmount = (decimal)(double)reader.GetDouble(9),
                IsClosed = reader.GetInt32(10) == 1
            };
        }

        private static IEnumerable<ExpenseBudgetHistory> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<ExpenseBudgetHistory>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static void SeedHistoryFromLegacyRules(SqliteConnection conn)
        {
            if (!TableExists(conn, "ExpenseBudgetRules_Legacy"))
                return;

            using var insert = conn.CreateCommand();
            insert.CommandText = @"
                INSERT INTO ExpenseBudgetHistory
                (Id, BudgetRuleId, PeriodStart, PeriodEnd, StartingBalance, BudgetAmount, ActualExpenses, Remaining, EndingBalance, RecommendedAmount, IsClosed)
                SELECT
                    lower(hex(randomblob(16))),
                    Id,
                    date('now', 'start of month'),
                    date('now', 'start of month', '+1 month', '-1 day'),
                    0,
                    abs(CurrentAverage),
                    0,
                    abs(CurrentAverage),
                    abs(CurrentAverage),
                    abs(CurrentAverage),
                    0
                FROM ExpenseBudgetRules_Legacy
                WHERE NOT EXISTS (
                    SELECT 1 FROM ExpenseBudgetHistory h WHERE h.BudgetRuleId = ExpenseBudgetRules_Legacy.Id
                );";
            insert.ExecuteNonQuery();
        }

        private static void SeedMissingOpenPeriods(SqliteConnection conn)
        {
            if (!TableExists(conn, "ExpenseBudgetRules"))
                return;

            using var insert = conn.CreateCommand();
            insert.CommandText = @"
                INSERT INTO ExpenseBudgetHistory
                (Id, BudgetRuleId, PeriodStart, PeriodEnd, StartingBalance, BudgetAmount, ActualExpenses, Remaining, EndingBalance, RecommendedAmount, IsClosed)
                SELECT
                    lower(hex(randomblob(16))),
                    Id,
                    date('now', 'start of month'),
                    date('now', 'start of month', '+1 month', '-1 day'),
                    0,
                    abs(DefaultBudgetAmount),
                    0,
                    abs(DefaultBudgetAmount),
                    abs(DefaultBudgetAmount),
                    0,
                    0
                FROM ExpenseBudgetRules r
                WHERE NOT EXISTS (
                    SELECT 1 FROM ExpenseBudgetHistory h WHERE h.BudgetRuleId = r.Id
                );";
            insert.ExecuteNonQuery();
        }

        private static void DropLegacyTables(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                DROP TABLE IF EXISTS ExpenseBudgetRules_Legacy;
                DROP TABLE IF EXISTS UtilityBudgetRules;";
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE ExpenseBudgetHistory ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Name;";
            cmd.Parameters.AddWithValue("@Name", tableName);
            return cmd.ExecuteScalar() is not null;
        }
    }
}
