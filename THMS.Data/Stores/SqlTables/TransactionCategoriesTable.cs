using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class TransactionCategoriesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TransactionCategories (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    ParentCategory TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public void Add(SqliteConnection conn, TransactionCategory category)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TransactionCategories (Id, Name, ParentCategory)
                VALUES (@Id, @Name, @ParentCategory);";
            Bind(cmd, category);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, TransactionCategory category)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE TransactionCategories SET
                    Name = @Name,
                    ParentCategory = @ParentCategory
                WHERE Id = @Id;";
            Bind(cmd, category);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM TransactionCategories WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public TransactionCategory? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, ParentCategory FROM TransactionCategories WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<TransactionCategory> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, ParentCategory FROM TransactionCategories ORDER BY Name;";
            using var reader = cmd.ExecuteReader();
            var list = new List<TransactionCategory>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static void Bind(SqliteCommand cmd, TransactionCategory category)
        {
            cmd.Parameters.AddWithValue("@Id", category.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", category.Name);
            cmd.Parameters.AddWithValue("@ParentCategory", (object?)category.ParentCategory ?? DBNull.Value);
        }

        private static TransactionCategory Read(SqliteDataReader reader)
        {
            return new TransactionCategory
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                ParentCategory = reader.IsDBNull(2) ? null : reader.GetString(2)
            };
        }
    }
}
