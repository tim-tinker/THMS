using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class FutureSingleTransactionsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS FutureSingleTransactions (
                    Id TEXT PRIMARY KEY,
                    AccountId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    Amount REAL NOT NULL,
                    Category TEXT,
                    IsRealized INTEGER NOT NULL,
                    PostedTransactionId TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        public void Add(SqliteConnection conn, FutureSingleTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO FutureSingleTransactions
                (Id, AccountId, Date, Description, Amount, Category, IsRealized, PostedTransactionId)
                VALUES
                (@Id, @AccountId, @Date, @Description, @Amount, @Category, @IsRealized, @PostedTransactionId);";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, FutureSingleTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE FutureSingleTransactions SET
                    AccountId = @AccountId,
                    Date = @Date,
                    Description = @Description,
                    Amount = @Amount,
                    Category = @Category,
                    IsRealized = @IsRealized,
                    PostedTransactionId = @PostedTransactionId
                WHERE Id = @Id;";
            Bind(cmd, transaction);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM FutureSingleTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public FutureSingleTransaction? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM FutureSingleTransactions WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<FutureSingleTransaction> GetByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureSingleTransactions
                WHERE AccountId = @AccountId
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<FutureSingleTransaction> GetByDateRange(SqliteConnection conn, DateTime start, DateTime end)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureSingleTransactions
                WHERE Date >= @Start AND Date <= @End
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Start", start);
            cmd.Parameters.AddWithValue("@End", end);
            return ReadAll(cmd);
        }

        public IEnumerable<FutureSingleTransaction> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM FutureSingleTransactions ORDER BY Date;";
            return ReadAll(cmd);
        }

        public IEnumerable<FutureSingleTransaction> GetRealized(SqliteConnection conn, DateTime cutoff)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureSingleTransactions
                WHERE IsRealized = 1 AND Date <= @Cutoff
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@Cutoff", cutoff);
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            "SELECT Id, AccountId, Date, Description, Amount, Category, IsRealized, PostedTransactionId";

        private static void Bind(SqliteCommand cmd, FutureSingleTransaction transaction)
        {
            cmd.Parameters.AddWithValue("@Id", transaction.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", transaction.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Date", transaction.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)transaction.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)transaction.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsRealized", transaction.IsRealized ? 1 : 0);
            cmd.Parameters.AddWithValue(
                "@PostedTransactionId",
                transaction.PostedTransactionId.HasValue
                    ? transaction.PostedTransactionId.Value.ToString()
                    : DBNull.Value);
        }

        private static FutureSingleTransaction Read(SqliteDataReader reader)
        {
            return new FutureSingleTransaction
            {
                Id = Guid.Parse(reader.GetString(0)),
                AccountId = Guid.Parse(reader.GetString(1)),
                Date = reader.GetDateTime(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                Amount = (decimal)(double)reader.GetDouble(4),
                Category = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsRealized = reader.GetInt32(6) == 1,
                PostedTransactionId = reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7))
            };
        }

        private static IEnumerable<FutureSingleTransaction> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<FutureSingleTransaction>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }
    }
}
