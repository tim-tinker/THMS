using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class ExpenseBudgetRulesTable
    {
        private readonly ExpenseBudgetRuleFactory _factory = new();

        public void InitializeSchema(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ExpenseBudgetRules (
                        Id TEXT PRIMARY KEY,
                        AccountId TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        MonthsToAverage INTEGER NOT NULL,
                        SmoothingMode TEXT NOT NULL,
                        CurrentAverage REAL NOT NULL,
                        NextOccurrence TEXT NOT NULL
                    );";
                cmd.ExecuteNonQuery();
            }

            EnsureColumn(conn, "SmoothingMode", "TEXT NOT NULL DEFAULT 'Hybrid'");

            using (var index = conn.CreateCommand())
            {
                index.CommandText =
                    "CREATE UNIQUE INDEX IF NOT EXISTS IX_ExpenseBudgetRules_Account_Category ON ExpenseBudgetRules(AccountId, Category);";
                index.ExecuteNonQuery();
            }

            MigrateFromUtilityBudgetRules(conn);
        }

        public void Upsert(SqliteConnection conn, ExpenseBudgetRule? rule)
        {
            if (rule is null)
                return;

            var existing = GetById(conn, rule.Id)
                ?? GetByAccountAndCategory(conn, rule.AccountId, rule.Category);
            if (existing is null)
            {
                Add(conn, rule);
                return;
            }

            rule.Id = existing.Id;
            Update(conn, rule);
        }

        public void Add(SqliteConnection conn, ExpenseBudgetRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ExpenseBudgetRules
                (Id, AccountId, Category, MonthsToAverage, SmoothingMode, CurrentAverage, NextOccurrence)
                VALUES
                (@Id, @AccountId, @Category, @MonthsToAverage, @SmoothingMode, @CurrentAverage, @NextOccurrence);";
            Bind(cmd, rule);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, ExpenseBudgetRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE ExpenseBudgetRules SET
                    AccountId = @AccountId,
                    Category = @Category,
                    MonthsToAverage = @MonthsToAverage,
                    SmoothingMode = @SmoothingMode,
                    CurrentAverage = @CurrentAverage,
                    NextOccurrence = @NextOccurrence
                WHERE Id = @Id;";
            Bind(cmd, rule);
            cmd.ExecuteNonQuery();
        }

        public ExpenseBudgetRule? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseBudgetRules
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<ExpenseBudgetRule> GetByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseBudgetRules
                WHERE AccountId = @AccountId
                ORDER BY Category;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        private ExpenseBudgetRule? GetByAccountAndCategory(
            SqliteConnection conn,
            Guid accountId,
            string category)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseBudgetRules
                WHERE AccountId = @AccountId AND Category = @Category;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.Parameters.AddWithValue("@Category", category);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        private const string SelectColumns =
            @"SELECT Id, AccountId, Category, MonthsToAverage, SmoothingMode, CurrentAverage, NextOccurrence";

        private static void Bind(SqliteCommand cmd, ExpenseBudgetRule rule)
        {
            cmd.Parameters.AddWithValue("@Id", rule.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", rule.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Category", rule.Category);
            cmd.Parameters.AddWithValue("@MonthsToAverage", rule.MonthsToAverage);
            cmd.Parameters.AddWithValue("@SmoothingMode", rule.SmoothingMode.ToString());
            cmd.Parameters.AddWithValue("@CurrentAverage", rule.CurrentAverage);
            cmd.Parameters.AddWithValue("@NextOccurrence", rule.NextOccurrence);
        }

        private ExpenseBudgetRule Read(SqliteDataReader reader)
        {
            var rule = _factory.Create(reader.GetString(2));
            rule.Id = Guid.Parse(reader.GetString(0));
            rule.AccountId = Guid.Parse(reader.GetString(1));
            rule.Category = reader.GetString(2);
            rule.MonthsToAverage = reader.GetInt32(3);
            rule.SmoothingMode = Enum.Parse<ExpenseSmoothingMode>(reader.GetString(4));
            rule.CurrentAverage = (decimal)(double)reader.GetDouble(5);
            rule.NextOccurrence = reader.GetDateTime(6);
            return rule;
        }

        private IEnumerable<ExpenseBudgetRule> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<ExpenseBudgetRule>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE ExpenseBudgetRules ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static void MigrateFromUtilityBudgetRules(SqliteConnection conn)
        {
            using var check = conn.CreateCommand();
            check.CommandText =
                "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'UtilityBudgetRules';";
            if (check.ExecuteScalar() is null)
                return;

            using var migrate = conn.CreateCommand();
            migrate.CommandText = @"
                INSERT OR IGNORE INTO ExpenseBudgetRules
                (Id, AccountId, Category, MonthsToAverage, SmoothingMode, CurrentAverage, NextOccurrence)
                SELECT Id, AccountId, Category, MonthsToAverage, 'Hybrid', CurrentAverage, NextOccurrence
                FROM UtilityBudgetRules;";
            migrate.ExecuteNonQuery();
        }
    }
}
