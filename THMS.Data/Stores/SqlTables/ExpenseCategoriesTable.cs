using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class ExpenseCategoriesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ExpenseCategories (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        ParentCategoryId TEXT,
                        IsActive INTEGER NOT NULL,
                        DisplayOrder INTEGER NOT NULL
                    );";
                cmd.ExecuteNonQuery();
            }

            MigrateFromTransactionCategories(conn);
            EnsureDefaults(conn);
        }

        public void Add(SqliteConnection conn, ExpenseCategory category)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ExpenseCategories (Id, Name, ParentCategoryId, IsActive, DisplayOrder)
                VALUES (@Id, @Name, @ParentCategoryId, @IsActive, @DisplayOrder);";
            Bind(cmd, category);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, ExpenseCategory category)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE ExpenseCategories SET
                    Name = @Name,
                    ParentCategoryId = @ParentCategoryId,
                    IsActive = @IsActive,
                    DisplayOrder = @DisplayOrder
                WHERE Id = @Id;";
            Bind(cmd, category);
            cmd.ExecuteNonQuery();
        }

        public void SoftDelete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE ExpenseCategories SET IsActive = 0 WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public ExpenseCategory? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM ExpenseCategories WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<ExpenseCategory> GetAll(SqliteConnection conn, bool includeInactive)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseCategories
                " + (includeInactive ? "" : "WHERE IsActive = 1 ") + @"
                ORDER BY DisplayOrder, Name;";
            return ReadAll(cmd);
        }

        public IEnumerable<ExpenseCategory> GetChildren(SqliteConnection conn, Guid parentCategoryId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM ExpenseCategories
                WHERE ParentCategoryId = @ParentCategoryId AND IsActive = 1
                ORDER BY DisplayOrder, Name;";
            cmd.Parameters.AddWithValue("@ParentCategoryId", parentCategoryId.ToString());
            return ReadAll(cmd);
        }

        public void EnsureDefaults(SqliteConnection conn)
        {
            foreach (var category in DefaultExpenseCategories.All)
            {
                if (GetById(conn, category.Id) is not null)
                    continue;

                Add(conn, category);
            }
        }

        public void MigrateLegacyCategoryStrings(SqliteConnection conn)
        {
            EnsureDefaults(conn);
            var known = GetAll(conn, includeInactive: true).ToList();
            var byName = known.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var name in DistinctCategoryNames(conn))
            {
                var canonical = DefaultExpenseCategories.CanonicalName(name);
                if (byName.ContainsKey(canonical))
                    continue;

                var created = new ExpenseCategory
                {
                    Name = canonical,
                    IsActive = true,
                    DisplayOrder = 200
                };
                Add(conn, created);
                byName[created.Name] = created;
            }

            known = GetAll(conn, includeInactive: true).ToList();
            byName = known.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

            string IdFor(string? raw)
            {
                var canonical = DefaultExpenseCategories.CanonicalName(raw);
                return byName.TryGetValue(canonical, out var category)
                    ? category.Id.ToString()
                    : DefaultExpenseCategories.UncategorizedId.ToString();
            }

            foreach (var table in new[]
                     {
                         "PostedTransactions",
                         "PostedTransferTransactions",
                         "FutureSingleTransactions",
                         "FutureTransferTransactions",
                         "RecurringSingleTransactionRules",
                         "RecurringTransferRules"
                     })
            {
                BackfillTable(conn, table, IdFor);
            }

            BackfillBudgetRules(conn, byName);
        }

        private static void BackfillTable(SqliteConnection conn, string table, Func<string?, string> idFor)
        {
            using var select = conn.CreateCommand();
            select.CommandText = $"SELECT DISTINCT Category FROM {table} WHERE Category IS NOT NULL AND TRIM(Category) != '' AND (CategoryId IS NULL OR CategoryId = '');";
            var names = new List<string>();
            using (var reader = select.ExecuteReader())
            {
                while (reader.Read())
                    names.Add(reader.GetString(0));
            }

            foreach (var name in names)
            {
                using var update = conn.CreateCommand();
                update.CommandText = $"UPDATE {table} SET CategoryId = @Id WHERE Category = @Name AND (CategoryId IS NULL OR CategoryId = '');";
                update.Parameters.AddWithValue("@Id", idFor(name));
                update.Parameters.AddWithValue("@Name", name);
                update.ExecuteNonQuery();
            }
        }

        private static void BackfillBudgetRules(SqliteConnection conn, Dictionary<string, ExpenseCategory> byName)
        {
            using var select = conn.CreateCommand();
            select.CommandText = "SELECT Id, IncludedCategories FROM ExpenseBudgetRules;";
            var rows = new List<(string Id, string Raw)>();
            using (var reader = select.ExecuteReader())
            {
                while (reader.Read())
                    rows.Add((reader.GetString(0), reader.IsDBNull(1) ? "" : reader.GetString(1)));
            }

            foreach (var (id, raw) in rows)
            {
                var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0 || parts.All(p => Guid.TryParse(p, out _)))
                    continue;

                var ids = new List<string>();
                foreach (var part in parts)
                {
                    var canonical = DefaultExpenseCategories.CanonicalName(part);
                    if (byName.TryGetValue(canonical, out var category))
                        ids.Add(category.Id.ToString());
                }

                if (ids.Count == 0)
                    continue;

                using var update = conn.CreateCommand();
                update.CommandText = "UPDATE ExpenseBudgetRules SET IncludedCategories = @Value WHERE Id = @Id;";
                update.Parameters.AddWithValue("@Value", string.Join("|", ids.Distinct()));
                update.Parameters.AddWithValue("@Id", id);
                update.ExecuteNonQuery();
            }
        }

        private static IEnumerable<string> DistinctCategoryNames(SqliteConnection conn)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in new[]
                     {
                         "PostedTransactions",
                         "PostedTransferTransactions",
                         "FutureSingleTransactions",
                         "FutureTransferTransactions",
                         "RecurringSingleTransactionRules",
                         "RecurringTransferRules"
                     })
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT DISTINCT Category FROM {table} WHERE Category IS NOT NULL AND TRIM(Category) != '';";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    names.Add(reader.GetString(0));
            }

            return names;
        }

        private static void MigrateFromTransactionCategories(SqliteConnection conn)
        {
            if (!TableExists(conn, "TransactionCategories"))
                return;

            using var select = conn.CreateCommand();
            select.CommandText = "SELECT Id, Name, ParentCategory FROM TransactionCategories;";
            var rows = new List<(string Id, string Name, string? Parent)>();
            using (var reader = select.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add((
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2)));
                }
            }

            foreach (var row in rows)
            {
                using var insert = conn.CreateCommand();
                insert.CommandText = @"
                    INSERT OR IGNORE INTO ExpenseCategories (Id, Name, ParentCategoryId, IsActive, DisplayOrder)
                    VALUES (@Id, @Name, NULL, 1, 100);";
                insert.Parameters.AddWithValue("@Id", row.Id);
                insert.Parameters.AddWithValue("@Name", row.Name);
                insert.ExecuteNonQuery();
            }

            var byName = GetAllStatic(conn).ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r.Parent)))
            {
                if (!byName.TryGetValue(row.Parent!, out var parent))
                    continue;

                using var update = conn.CreateCommand();
                update.CommandText = "UPDATE ExpenseCategories SET ParentCategoryId = @ParentId WHERE Id = @Id AND (ParentCategoryId IS NULL OR ParentCategoryId = '');";
                update.Parameters.AddWithValue("@ParentId", parent.Id.ToString());
                update.Parameters.AddWithValue("@Id", row.Id);
                update.ExecuteNonQuery();
            }
        }

        private static IEnumerable<ExpenseCategory> GetAllStatic(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM ExpenseCategories;";
            return ReadAll(cmd);
        }

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Name;";
            cmd.Parameters.AddWithValue("@Name", tableName);
            return cmd.ExecuteScalar() is not null;
        }

        private const string SelectColumns =
            "SELECT Id, Name, ParentCategoryId, IsActive, DisplayOrder";

        private static void Bind(SqliteCommand cmd, ExpenseCategory category)
        {
            cmd.Parameters.AddWithValue("@Id", category.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", category.Name);
            cmd.Parameters.AddWithValue("@ParentCategoryId", SqliteCategoryColumns.BindId(category.ParentCategoryId));
            cmd.Parameters.AddWithValue("@IsActive", category.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
        }

        private static ExpenseCategory Read(SqliteDataReader reader)
        {
            return new ExpenseCategory
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                ParentCategoryId = SqliteCategoryColumns.ReadId(reader, 2),
                IsActive = reader.GetInt32(3) == 1,
                DisplayOrder = reader.GetInt32(4)
            };
        }

        private static IEnumerable<ExpenseCategory> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<ExpenseCategory>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }
    }
}
