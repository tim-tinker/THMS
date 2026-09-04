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
        }

        public void Add(SqliteConnection conn, PostedTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PostedTransactions
                (Id, AccountId, Date, Description, Amount, Category)
                VALUES
                (@Id, @AccountId, @Date, @Description, @Amount, @Category);";
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
                    Category = @Category
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
            "SELECT Id, AccountId, Date, Description, Amount, Category";

        private static void Bind(SqliteCommand cmd, PostedTransaction transaction)
        {
            cmd.Parameters.AddWithValue("@Id", transaction.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", transaction.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Date", transaction.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)transaction.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)transaction.Category ?? DBNull.Value);
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
                Category = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
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
