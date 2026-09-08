using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class ExpenseBudgetRulesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            MigrateLegacySchema(conn);

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ExpenseBudgetRules (
                        Id TEXT PRIMARY KEY,
                        AccountId TEXT NOT NULL,
                        BudgetName TEXT NOT NULL,
                        IncludedCategories TEXT NOT NULL,
                        BudgetFrequency TEXT NOT NULL,
                        DefaultBudgetAmount REAL NOT NULL,
                        IsActive INTEGER NOT NULL
                    );";
                cmd.ExecuteNonQuery();
            }
        }

        public void Add(SqliteConnection conn, ExpenseBudgetRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ExpenseBudgetRules
                (Id, AccountId, BudgetName, IncludedCategories, BudgetFrequency, DefaultBudgetAmount, IsActive)
                VALUES
                (@Id, @AccountId, @BudgetName, @IncludedCategories, @BudgetFrequency, @DefaultBudgetAmount, @IsActive);";
            Bind(cmd, rule);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, ExpenseBudgetRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE ExpenseBudgetRules SET
                    AccountId = @AccountId,
                    BudgetName = @BudgetName,
                    IncludedCategories = @IncludedCategories,
                    BudgetFrequency = @BudgetFrequency,
                    DefaultBudgetAmount = @DefaultBudgetAmount,
                    IsActive = @IsActive
                WHERE Id = @Id;";
            Bind(cmd, rule);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ExpenseBudgetRules WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public ExpenseBudgetRule? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM ExpenseBudgetRules WHERE Id = @Id;";
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
                ORDER BY BudgetName;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<ExpenseBudgetRule> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseBudgetRules
                ORDER BY BudgetName;";
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            @"SELECT Id, AccountId, BudgetName, IncludedCategories, BudgetFrequency, DefaultBudgetAmount, IsActive";

        private static void Bind(SqliteCommand cmd, ExpenseBudgetRule rule)
        {
            cmd.Parameters.AddWithValue("@Id", rule.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", rule.AccountId.ToString());
            cmd.Parameters.AddWithValue("@BudgetName", rule.BudgetName);
            cmd.Parameters.AddWithValue("@IncludedCategories", string.Join("|", rule.IncludedCategoryIds));
            cmd.Parameters.AddWithValue("@BudgetFrequency", rule.BudgetFrequency.ToString());
            cmd.Parameters.AddWithValue("@DefaultBudgetAmount", rule.DefaultBudgetAmount);
            cmd.Parameters.AddWithValue("@IsActive", rule.IsActive ? 1 : 0);
        }

        private static ExpenseBudgetRule Read(SqliteDataReader reader)
        {
            return new ExpenseBudgetRule
            {
                Id = Guid.Parse(reader.GetString(0)),
                AccountId = Guid.Parse(reader.GetString(1)),
                BudgetName = reader.GetString(2),
                IncludedCategoryIds = SplitCategoryIds(reader.IsDBNull(3) ? "" : reader.GetString(3)),
                BudgetFrequency = Enum.Parse<BudgetFrequency>(reader.GetString(4)),
                DefaultBudgetAmount = (decimal)(double)reader.GetDouble(5),
                IsActive = reader.GetInt32(6) == 1
            };
        }

        private static List<Guid> SplitCategoryIds(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return [];

            var ids = new List<Guid>();
            foreach (var part in raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var id))
                    ids.Add(id);
            }

            return ids;
        }

        private static IEnumerable<ExpenseBudgetRule> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<ExpenseBudgetRule>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static void MigrateLegacySchema(SqliteConnection conn)
        {
            if (!TableExists(conn, "ExpenseBudgetRules"))
            {
                MigrateFromUtilityBudgetRules(conn, createTargetFirst: true);
                return;
            }

            if (!ColumnExists(conn, "ExpenseBudgetRules", "Category"))
                return;

            using (var rename = conn.CreateCommand())
            {
                rename.CommandText = "ALTER TABLE ExpenseBudgetRules RENAME TO ExpenseBudgetRules_Legacy;";
                rename.ExecuteNonQuery();
            }

            using (var create = conn.CreateCommand())
            {
                create.CommandText = @"
                    CREATE TABLE ExpenseBudgetRules (
                        Id TEXT PRIMARY KEY,
                        AccountId TEXT NOT NULL,
                        BudgetName TEXT NOT NULL,
                        IncludedCategories TEXT NOT NULL,
                        BudgetFrequency TEXT NOT NULL,
                        DefaultBudgetAmount REAL NOT NULL,
                        IsActive INTEGER NOT NULL
                    );";
                create.ExecuteNonQuery();
            }

            using (var copy = conn.CreateCommand())
            {
                copy.CommandText = @"
                    INSERT INTO ExpenseBudgetRules
                    (Id, AccountId, BudgetName, IncludedCategories, BudgetFrequency, DefaultBudgetAmount, IsActive)
                    SELECT
                        Id,
                        AccountId,
                        Category,
                        CASE WHEN Category = 'Utilities' THEN 'Electric|Water|Gas|Utilities' ELSE Category END,
                        'Monthly',
                        CurrentAverage,
                        1
                    FROM ExpenseBudgetRules_Legacy;";
                copy.ExecuteNonQuery();
            }

            MigrateFromUtilityBudgetRules(conn, createTargetFirst: false);
        }

        private static void MigrateFromUtilityBudgetRules(SqliteConnection conn, bool createTargetFirst)
        {
            if (!TableExists(conn, "UtilityBudgetRules"))
                return;

            if (createTargetFirst && !TableExists(conn, "ExpenseBudgetRules"))
            {
                using var create = conn.CreateCommand();
                create.CommandText = @"
                    CREATE TABLE ExpenseBudgetRules (
                        Id TEXT PRIMARY KEY,
                        AccountId TEXT NOT NULL,
                        BudgetName TEXT NOT NULL,
                        IncludedCategories TEXT NOT NULL,
                        BudgetFrequency TEXT NOT NULL,
                        DefaultBudgetAmount REAL NOT NULL,
                        IsActive INTEGER NOT NULL
                    );";
                create.ExecuteNonQuery();
            }

            using var migrate = conn.CreateCommand();
            migrate.CommandText = @"
                INSERT OR IGNORE INTO ExpenseBudgetRules
                (Id, AccountId, BudgetName, IncludedCategories, BudgetFrequency, DefaultBudgetAmount, IsActive)
                SELECT Id, AccountId, Category, 'Electric|Water|Gas|Utilities', 'Monthly', CurrentAverage, 1
                FROM UtilityBudgetRules;";
            migrate.ExecuteNonQuery();
        }

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Name;";
            cmd.Parameters.AddWithValue("@Name", tableName);
            return cmd.ExecuteScalar() is not null;
        }

        private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
