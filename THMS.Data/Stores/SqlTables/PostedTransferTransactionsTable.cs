using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class PostedTransferTransactionsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PostedTransferTransactions (
                    Id TEXT PRIMARY KEY,
                    AccountId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    Amount REAL NOT NULL,
                    Category TEXT,
                    RelatedPostedTransactionId TEXT,
                    Direction TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
            SqliteCategoryColumns.EnsureCategoryId(conn, "PostedTransferTransactions");
            EnsureColumn(conn, "ImportedStatus", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "RecommendedExpectedId", "TEXT");
        }

        public void Add(SqliteConnection conn, PostedTransferTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PostedTransferTransactions
                (Id, AccountId, Date, Description, Amount, Category, CategoryId, RelatedPostedTransactionId, Direction,
                 ImportedStatus, RecommendedExpectedId)
                VALUES
                (@Id, @AccountId, @Date, @Description, @Amount, @Category, @CategoryId, @RelatedPostedTransactionId, @Direction,
                 @ImportedStatus, @RecommendedExpectedId);";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, PostedTransferTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE PostedTransferTransactions SET
                    AccountId = @AccountId,
                    Date = @Date,
                    Description = @Description,
                    Amount = @Amount,
                    Category = @Category,
                    CategoryId = @CategoryId,
                    RelatedPostedTransactionId = @RelatedPostedTransactionId,
                    Direction = @Direction,
                    ImportedStatus = @ImportedStatus,
                    RecommendedExpectedId = @RecommendedExpectedId
                WHERE Id = @Id;";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM PostedTransferTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public PostedTransferTransaction? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM PostedTransferTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<PostedTransferTransaction> GetByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransferTransactions
                WHERE AccountId = @AccountId
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<PostedTransferTransaction> GetByDateRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransferTransactions
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);
            return ReadAll(cmd);
        }

        public IEnumerable<PostedTransferTransaction> GetByAccountAndDateRange(
            SqliteConnection conn,
            Guid accountId,
            DateTime start,
            DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransferTransactions
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
                FROM PostedTransferTransactions
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
                FROM PostedTransferTransactions
                WHERE AccountId = @AccountId AND Date > @After;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.Parameters.AddWithValue("@After", after);
            var value = cmd.ExecuteScalar();
            return value is null or DBNull ? 0 : Convert.ToDecimal(value);
        }

        public PostedTransferTransaction? GetLatest(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransferTransactions
                WHERE AccountId = @AccountId
                ORDER BY Date DESC
                LIMIT 1;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<PostedTransferTransaction> GetUnmatchedByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM PostedTransferTransactions
                WHERE AccountId = @AccountId
                  AND (RelatedPostedTransactionId IS NULL OR RelatedPostedTransactionId = '')
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            "SELECT Id, AccountId, Date, Description, Amount, Category, RelatedPostedTransactionId, Direction, CategoryId, ImportedStatus, RecommendedExpectedId";

        private static void Bind(SqliteCommand cmd, PostedTransferTransaction transaction)
        {
            cmd.Parameters.AddWithValue("@Id", transaction.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", transaction.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Date", transaction.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)transaction.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)transaction.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", SqliteCategoryColumns.BindId(transaction.CategoryId));
            cmd.Parameters.AddWithValue(
                "@RelatedPostedTransactionId",
                transaction.RelatedPostedTransactionId == Guid.Empty
                    ? DBNull.Value
                    : transaction.RelatedPostedTransactionId.ToString());
            cmd.Parameters.AddWithValue("@Direction", transaction.Direction.ToString());
            cmd.Parameters.AddWithValue("@ImportedStatus", (int)transaction.ImportedStatus);
            cmd.Parameters.AddWithValue(
                "@RecommendedExpectedId",
                transaction.RecommendedExpectedId is Guid expected && expected != Guid.Empty
                    ? expected.ToString()
                    : DBNull.Value);
        }

        private static PostedTransferTransaction Read(SqliteDataReader reader)
        {
            return new PostedTransferTransaction
            {
                Id = Guid.Parse(reader.GetString(0)),
                AccountId = Guid.Parse(reader.GetString(1)),
                Date = reader.GetDateTime(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                Amount = (decimal)(double)reader.GetDouble(4),
                Category = reader.IsDBNull(5) ? null : reader.GetString(5),
                RelatedPostedTransactionId = reader.IsDBNull(6) ? Guid.Empty : Guid.Parse(reader.GetString(6)),
                Direction = Enum.Parse<TransferDirection>(reader.GetString(7)),
                CategoryId = SqliteCategoryColumns.ReadId(reader, 8),
                ImportedStatus = ReadImportedStatus(reader, 9),
                RecommendedExpectedId = SqliteCategoryColumns.ReadId(reader, 10)
            };
        }

        private static ImportedStatus ReadImportedStatus(SqliteDataReader reader, int index) =>
            reader.FieldCount > index && !reader.IsDBNull(index)
                ? (ImportedStatus)reader.GetInt32(index)
                : ImportedStatus.Unreconciled;

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE PostedTransferTransactions ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static IEnumerable<PostedTransferTransaction> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<PostedTransferTransaction>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }
    }
}
