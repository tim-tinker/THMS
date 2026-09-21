using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class PostedTransactionsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PostedTransactions (
                    Id TEXT PRIMARY KEY,
                    AccountId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    Amount REAL NOT NULL,
                    Category TEXT
                );";
            cmd.ExecuteNonQuery();
            SqliteCategoryColumns.EnsureCategoryId(conn, "PostedTransactions");
            EnsureColumn(conn, "ImportedStatus", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "RecommendedExpectedId", "TEXT");
        }

        public void Add(SqliteConnection conn, PostedTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PostedTransactions
                (Id, AccountId, Date, Description, Amount, Category, CategoryId, ImportedStatus, RecommendedExpectedId)
                VALUES
                (@Id, @AccountId, @Date, @Description, @Amount, @Category, @CategoryId, @ImportedStatus, @RecommendedExpectedId);";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, PostedTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE PostedTransactions SET
                    AccountId = @AccountId,
                    Date = @Date,
                    Description = @Description,
                    Amount = @Amount,
                    Category = @Category,
                    CategoryId = @CategoryId,
                    ImportedStatus = @ImportedStatus,
                    RecommendedExpectedId = @RecommendedExpectedId
                WHERE Id = @Id;";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM PostedTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public PostedTransaction? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM PostedTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<PostedTransaction> GetByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransactions
                WHERE AccountId = @AccountId
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<PostedTransaction> GetByDateRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransactions
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);
            return ReadAll(cmd);
        }

        public IEnumerable<PostedTransaction> GetByAccountAndDateRange(
            SqliteConnection conn,
            Guid accountId,
            DateTime start,
            DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransactions
                WHERE AccountId = @AccountId
                  AND Date >= @Start AND Date <= @End
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);
            return ReadAll(cmd);
        }

        public decimal SumAmountBefore(SqliteConnection conn, Guid accountId, DateTime before)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(Amount), 0)
                FROM PostedTransactions
                WHERE AccountId = @AccountId AND Date < @Before;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.Parameters.AddWithValue("@Before", before);
            var value = cmd.ExecuteScalar();
            return value is null or DBNull ? 0 : Convert.ToDecimal(value);
        }

        public decimal SumAmountAfter(SqliteConnection conn, Guid accountId, DateTime after)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(Amount), 0)
                FROM PostedTransactions
                WHERE AccountId = @AccountId AND Date > @After;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.Parameters.AddWithValue("@After", after);
            var value = cmd.ExecuteScalar();
            return value is null or DBNull ? 0 : Convert.ToDecimal(value);
        }

        public PostedTransaction? GetLatest(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransactions
                WHERE AccountId = @AccountId
                ORDER BY Date DESC
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<PostedTransaction> GetUnmatchedByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransactions p
                WHERE p.AccountId = @AccountId
                  AND p.Id NOT IN (
                      SELECT RelatedPostedTransactionId
                      FROM PostedTransferTransactions
                      WHERE RelatedPostedTransactionId IS NOT NULL
                        AND RelatedPostedTransactionId != ''
                  )
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            "SELECT Id, AccountId, Date, Description, Amount, Category, CategoryId, ImportedStatus, RecommendedExpectedId";

        private static void Bind(SqliteCommand cmd, PostedTransaction transaction)
        {
            cmd.Parameters.AddWithValue("@Id", transaction.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", transaction.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Date", transaction.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)transaction.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)transaction.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", SqliteCategoryColumns.BindId(transaction.CategoryId));
            cmd.Parameters.AddWithValue("@ImportedStatus", (int)transaction.ImportedStatus);
            cmd.Parameters.AddWithValue(
                "@RecommendedExpectedId",
                transaction.RecommendedExpectedId is Guid expected && expected != Guid.Empty
                    ? expected.ToString()
                    : DBNull.Value);
        }

        private static PostedTransaction Read(SqliteDataReader reader)
        {
            return new PostedTransaction
            {
                Id = Guid.Parse(reader.GetString(0)),
                AccountId = Guid.Parse(reader.GetString(1)),
                Date = reader.GetDateTime(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                Amount = (decimal)(double)reader.GetDouble(4),
                Category = reader.IsDBNull(5) ? null : reader.GetString(5),
                CategoryId = SqliteCategoryColumns.ReadId(reader, 6),
                ImportedStatus = ReadImportedStatus(reader, 7),
                RecommendedExpectedId = SqliteCategoryColumns.ReadId(reader, 8)
            };
        }

        private static ImportedStatus ReadImportedStatus(SqliteDataReader reader, int index) =>
            reader.FieldCount > index && !reader.IsDBNull(index)
                ? (ImportedStatus)reader.GetInt32(index)
                : ImportedStatus.Unreconciled;

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE PostedTransactions ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static IEnumerable<PostedTransaction> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<PostedTransaction>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }
    }
}
