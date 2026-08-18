using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class FutureTransferTransactionsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS FutureTransferTransactions (
                    Id TEXT PRIMARY KEY,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    Amount REAL NOT NULL,
                    Category TEXT,
                    FromAccountId TEXT NOT NULL,
                    ToAccountId TEXT NOT NULL,
                    IsRealized INTEGER NOT NULL,
                    PostedFromTransactionId TEXT,
                    PostedToTransactionId TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public void Add(SqliteConnection conn, FutureTransferTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO FutureTransferTransactions
                (Id, Date, Description, Amount, Category, FromAccountId, ToAccountId,
                 IsRealized, PostedFromTransactionId, PostedToTransactionId)
                VALUES
                (@Id, @Date, @Description, @Amount, @Category, @FromAccountId, @ToAccountId,
                 @IsRealized, @PostedFromTransactionId, @PostedToTransactionId);";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, FutureTransferTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE FutureTransferTransactions SET
                    Date = @Date,
                    Description = @Description,
                    Amount = @Amount,
                    Category = @Category,
                    FromAccountId = @FromAccountId,
                    ToAccountId = @ToAccountId,
                    IsRealized = @IsRealized,
                    PostedFromTransactionId = @PostedFromTransactionId,
                    PostedToTransactionId = @PostedToTransactionId
                WHERE Id = @Id;";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM FutureTransferTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public FutureTransferTransaction? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM FutureTransferTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<FutureTransferTransaction> GetByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureTransferTransactions
                WHERE FromAccountId = @AccountId OR ToAccountId = @AccountId
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<FutureTransferTransaction> GetByDateRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureTransferTransactions
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);
            return ReadAll(cmd);
        }

        public IEnumerable<FutureTransferTransaction> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM FutureTransferTransactions ORDER BY Date;";
            return ReadAll(cmd);
        }

        public IEnumerable<FutureTransferTransaction> GetRealized(SqliteConnection conn, DateTime cutoff)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureTransferTransactions
                WHERE IsRealized = 1 AND Date <= @Cutoff
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Cutoff", cutoff);
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            @"SELECT Id, Date, Description, Amount, Category, FromAccountId, ToAccountId,
                     IsRealized, PostedFromTransactionId, PostedToTransactionId";

        private static void Bind(SqliteCommand cmd, FutureTransferTransaction transaction)
        {
            cmd.Parameters.AddWithValue("@Id", transaction.Id.ToString());
            cmd.Parameters.AddWithValue("@Date", transaction.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)transaction.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)transaction.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromAccountId", transaction.FromAccountId.ToString());
            cmd.Parameters.AddWithValue("@ToAccountId", transaction.ToAccountId.ToString());
            cmd.Parameters.AddWithValue("@IsRealized", transaction.IsRealized ? 1 : 0);
            cmd.Parameters.AddWithValue(
                "@PostedFromTransactionId",
                transaction.PostedFromTransactionId.HasValue
                    ? transaction.PostedFromTransactionId.Value.ToString()
                    : DBNull.Value);
            cmd.Parameters.AddWithValue(
                "@PostedToTransactionId",
                transaction.PostedToTransactionId.HasValue
                    ? transaction.PostedToTransactionId.Value.ToString()
                    : DBNull.Value);
        }

        private static FutureTransferTransaction Read(SqliteDataReader reader)
        {
            return new FutureTransferTransaction
            {
                Id = Guid.Parse(reader.GetString(0)),
                Date = reader.GetDateTime(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Amount = (decimal)(double)reader.GetDouble(3),
                Category = reader.IsDBNull(4) ? null : reader.GetString(4),
                FromAccountId = Guid.Parse(reader.GetString(5)),
                ToAccountId = Guid.Parse(reader.GetString(6)),
                IsRealized = reader.GetInt32(7) == 1,
                PostedFromTransactionId = reader.IsDBNull(8) ? null : Guid.Parse(reader.GetString(8)),
                PostedToTransactionId = reader.IsDBNull(9) ? null : Guid.Parse(reader.GetString(9))
            };
        }

        private static IEnumerable<FutureTransferTransaction> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<FutureTransferTransaction>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }
    }
}
