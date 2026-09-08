using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class CategoryAssignmentHistoryTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CategoryAssignmentHistory (
                    NormalizedDescription TEXT PRIMARY KEY,
                    CategoryId TEXT NOT NULL,
                    UpdatedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, string normalizedDescription, Guid categoryId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO CategoryAssignmentHistory (NormalizedDescription, CategoryId, UpdatedOn)
                VALUES (@Key, @CategoryId, @UpdatedOn)
                ON CONFLICT(NormalizedDescription) DO UPDATE SET
                    CategoryId = @CategoryId,
                    UpdatedOn = @UpdatedOn;";
            cmd.Parameters.AddWithValue("@Key", normalizedDescription);
            cmd.Parameters.AddWithValue("@CategoryId", categoryId.ToString());
            cmd.Parameters.AddWithValue("@UpdatedOn", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public CategoryAssignment? Get(SqliteConnection conn, string normalizedDescription)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT NormalizedDescription, CategoryId, UpdatedOn
                FROM CategoryAssignmentHistory
                WHERE NormalizedDescription = @Key;";
            cmd.Parameters.AddWithValue("@Key", normalizedDescription);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new CategoryAssignment
            {
                NormalizedDescription = reader.GetString(0),
                CategoryId = Guid.Parse(reader.GetString(1)),
                UpdatedOn = reader.GetDateTime(2)
            };
        }

        public void Retarget(SqliteConnection conn, Guid fromId, Guid toId)
        {
            using (var update = conn.CreateCommand())
            {
                update.CommandText = @"
                    UPDATE CategoryAssignmentHistory
                    SET CategoryId = @ToId, UpdatedOn = @UpdatedOn
                    WHERE CategoryId = @FromId
                      AND NormalizedDescription NOT IN (
                          SELECT NormalizedDescription FROM CategoryAssignmentHistory WHERE CategoryId = @ToId
                      );";
                update.Parameters.AddWithValue("@ToId", toId.ToString());
                update.Parameters.AddWithValue("@FromId", fromId.ToString());
                update.Parameters.AddWithValue("@UpdatedOn", DateTime.UtcNow);
                update.ExecuteNonQuery();
            }

            using var delete = conn.CreateCommand();
            delete.CommandText = "DELETE FROM CategoryAssignmentHistory WHERE CategoryId = @FromId;";
            delete.Parameters.AddWithValue("@FromId", fromId.ToString());
            delete.ExecuteNonQuery();
        }

        public int CountByCategory(SqliteConnection conn, Guid categoryId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM CategoryAssignmentHistory WHERE CategoryId = @Id;";
            cmd.Parameters.AddWithValue("@Id", categoryId.ToString());
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
