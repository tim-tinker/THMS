using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    internal static class SqliteCategoryColumns
    {
        public static void EnsureCategoryId(SqliteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN IF NOT EXISTS CategoryId TEXT;";
            cmd.ExecuteNonQuery();
        }

        public static object BindId(Guid? categoryId) =>
            categoryId is Guid id && id != Guid.Empty ? id.ToString() : DBNull.Value;

        public static Guid? ReadId(SqliteDataReader reader, int index)
        {
            if (reader.FieldCount <= index || reader.IsDBNull(index))
                return null;

            return Guid.TryParse(reader.GetString(index), out var id) ? id : null;
        }

        public static void Retarget(SqliteConnection conn, string tableName, Guid fromId, Guid toId, string categoryName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE {tableName} SET CategoryId = @ToId, Category = @Name WHERE CategoryId = @FromId;";
            cmd.Parameters.AddWithValue("@ToId", toId.ToString());
            cmd.Parameters.AddWithValue("@Name", categoryName);
            cmd.Parameters.AddWithValue("@FromId", fromId.ToString());
            cmd.ExecuteNonQuery();
        }

        public static int Count(SqliteConnection conn, string tableName, Guid categoryId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE CategoryId = @Id;";
            cmd.Parameters.AddWithValue("@Id", categoryId.ToString());
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
