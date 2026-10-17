using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class UtilityUsageRecordsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS UtilityUsageRecords (
                    Id TEXT PRIMARY KEY,
                    StatementId TEXT NOT NULL,
                    Description TEXT NOT NULL DEFAULT '',
                    Type TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Rate REAL NOT NULL,
                    SortOrder INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS IX_UtilityUsageRecords_StatementId
                    ON UtilityUsageRecords (StatementId);";
            cmd.ExecuteNonQuery();
            SqliteSchema.EnsureColumn(conn, "UtilityUsageRecords", "Description", "TEXT NOT NULL DEFAULT ''");
        }

        public void ReplaceAll(SqliteConnection conn, Guid statementId, IEnumerable<UtilityUsageRecord> usage)
        {
            DeleteByStatement(conn, statementId);
            var order = 0;
            foreach (var record in usage)
                Insert(conn, statementId, record, order++);
        }

        public void DeleteByStatement(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM UtilityUsageRecords WHERE StatementId = @StatementId;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.ExecuteNonQuery();
        }

        public List<UtilityUsageRecord> GetByStatement(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Description, Type, Amount, Rate
                FROM UtilityUsageRecords
                WHERE StatementId = @StatementId
                ORDER BY SortOrder, Id;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            using var reader = cmd.ExecuteReader();
            var list = new List<UtilityUsageRecord>();
            while (reader.Read())
            {
                list.Add(new UtilityUsageRecord
                {
                    Description = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Type = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Amount = (decimal)(double)reader.GetDouble(2),
                    Rate = decimal.Round((decimal)(double)reader.GetDouble(3), 6, MidpointRounding.AwayFromZero)
                });
            }

            return list;
        }

        private static void Insert(SqliteConnection conn, Guid statementId, UtilityUsageRecord record, int sortOrder)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO UtilityUsageRecords
                (Id, StatementId, Description, Type, Amount, Rate, SortOrder)
                VALUES
                (@Id, @StatementId, @Description, @Type, @Amount, @Rate, @SortOrder);";
            cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.Parameters.AddWithValue("@Description", record.Description ?? "");
            cmd.Parameters.AddWithValue("@Type", record.Type);
            cmd.Parameters.AddWithValue("@Amount", record.Amount);
            cmd.Parameters.AddWithValue("@Rate", record.Rate);
            cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            cmd.ExecuteNonQuery();
        }
    }
}
