using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class SplitTransactionRowsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS SplitTransactionRows (
                    Id TEXT PRIMARY KEY,
                    ParentTransactionId TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Category TEXT,
                    CategoryId TEXT,
                    Type TEXT NOT NULL,
                    TransferAccountId TEXT,
                    Notes TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_SplitTransactionRows_Parent
                    ON SplitTransactionRows (ParentTransactionId);";
            cmd.ExecuteNonQuery();
        }

        public void ReplaceAll(SqliteConnection conn, Guid parentId, IEnumerable<SplitTransactionRow> splits)
        {
            DeleteByParent(conn, parentId);
            foreach (var split in splits)
            {
                if (split.Id == Guid.Empty)
                    split.Id = Guid.NewGuid();
                split.ParentTransactionId = parentId;
                Insert(conn, split);
            }
        }

        public void DeleteByParent(SqliteConnection conn, Guid parentId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM SplitTransactionRows WHERE ParentTransactionId = @ParentId;";
            cmd.Parameters.AddWithValue("@ParentId", parentId.ToString());
            cmd.ExecuteNonQuery();
        }

        public List<SplitTransactionRow> GetByParent(SqliteConnection conn, Guid parentId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM SplitTransactionRows WHERE ParentTransactionId = @ParentId ORDER BY Id;";
            cmd.Parameters.AddWithValue("@ParentId", parentId.ToString());
            return ReadAll(cmd);
        }

        public Dictionary<Guid, List<SplitTransactionRow>> GetByParents(
            SqliteConnection conn,
            IReadOnlyCollection<Guid> parentIds)
        {
            var lookup = parentIds.Distinct().ToDictionary(id => id, _ => new List<SplitTransactionRow>());
            if (lookup.Count == 0)
                return lookup;

            const int chunkSize = 400;
            foreach (var chunk in lookup.Keys.Chunk(chunkSize))
            {
                using var cmd = conn.CreateCommand();
                var names = new List<string>(chunk.Length);
                for (var i = 0; i < chunk.Length; i++)
                {
                    var name = $"@p{i}";
                    names.Add(name);
                    cmd.Parameters.AddWithValue(name, chunk[i].ToString());
                }

                cmd.CommandText = SelectColumns +
                    $" FROM SplitTransactionRows WHERE ParentTransactionId IN ({string.Join(",", names)}) ORDER BY Id;";
                foreach (var split in ReadAll(cmd))
                {
                    if (lookup.TryGetValue(split.ParentTransactionId, out var list))
                        list.Add(split);
                }
            }

            return lookup;
        }

        private void Insert(SqliteConnection conn, SplitTransactionRow split)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO SplitTransactionRows
                (Id, ParentTransactionId, Amount, Category, CategoryId, Type, TransferAccountId, Notes)
                VALUES
                (@Id, @ParentTransactionId, @Amount, @Category, @CategoryId, @Type, @TransferAccountId, @Notes);";
            cmd.Parameters.AddWithValue("@Id", split.Id.ToString());
            cmd.Parameters.AddWithValue("@ParentTransactionId", split.ParentTransactionId.ToString());
            cmd.Parameters.AddWithValue("@Amount", split.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)split.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", SqliteCategoryColumns.BindId(split.CategoryId));
            cmd.Parameters.AddWithValue("@Type", split.Type.ToString());
            cmd.Parameters.AddWithValue(
                "@TransferAccountId",
                split.TransferAccountId is Guid accountId && accountId != Guid.Empty
                    ? accountId.ToString()
                    : DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", (object?)split.Notes ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private const string SelectColumns =
            "SELECT Id, ParentTransactionId, Amount, Category, CategoryId, Type, TransferAccountId, Notes";

        private static List<SplitTransactionRow> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<SplitTransactionRow>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static SplitTransactionRow Read(SqliteDataReader reader)
        {
            var typeName = reader.IsDBNull(5) ? nameof(SplitType.Expense) : reader.GetString(5);
            if (!Enum.TryParse<SplitType>(typeName, out var type))
                type = SplitType.Expense;

            Guid? transferAccountId = null;
            if (!reader.IsDBNull(6) && Guid.TryParse(reader.GetString(6), out var accountId) && accountId != Guid.Empty)
                transferAccountId = accountId;

            return new SplitTransactionRow
            {
                Id = Guid.Parse(reader.GetString(0)),
                ParentTransactionId = Guid.Parse(reader.GetString(1)),
                Amount = (decimal)(double)reader.GetDouble(2),
                Category = reader.IsDBNull(3) ? null : reader.GetString(3),
                CategoryId = SqliteCategoryColumns.ReadId(reader, 4),
                Type = type,
                TransferAccountId = transferAccountId,
                Notes = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
        }
    }
}
