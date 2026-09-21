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
                    PostedTransactionId TEXT,
                    IsUserCreated INTEGER NOT NULL DEFAULT 0
                );";
            cmd.ExecuteNonQuery();
            EnsureColumn(conn, "IsUserCreated", "INTEGER NOT NULL DEFAULT 0");
            SqliteCategoryColumns.EnsureCategoryId(conn, "FutureSingleTransactions");
            EnsureColumn(conn, "IsPlannedPayment", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "StatementId", "TEXT");
            EnsureColumn(conn, "PromotionalBalanceId", "TEXT");
            EnsureColumn(conn, "PlanningNote", "TEXT");
            EnsureColumn(conn, "Origin", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "OriginId", "TEXT");
            EnsureColumn(conn, "Status", "INTEGER NOT NULL DEFAULT 0");
        }

        public void Add(SqliteConnection conn, FutureSingleTransaction transaction)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO FutureSingleTransactions
                (Id, AccountId, Date, Description, Amount, Category, CategoryId, IsRealized, PostedTransactionId, IsUserCreated,
                 IsPlannedPayment, StatementId, PromotionalBalanceId, PlanningNote, Origin, OriginId, Status)
                VALUES
                (@Id, @AccountId, @Date, @Description, @Amount, @Category, @CategoryId, @IsRealized, @PostedTransactionId, @IsUserCreated,
                 @IsPlannedPayment, @StatementId, @PromotionalBalanceId, @PlanningNote, @Origin, @OriginId, @Status);";
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
                    CategoryId = @CategoryId,
                    IsRealized = @IsRealized,
                    PostedTransactionId = @PostedTransactionId,
                    IsUserCreated = @IsUserCreated,
                    IsPlannedPayment = @IsPlannedPayment,
                    StatementId = @StatementId,
                    PromotionalBalanceId = @PromotionalBalanceId,
                    PlanningNote = @PlanningNote,
                    Origin = @Origin,
                    OriginId = @OriginId,
                    Status = @Status
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

        public IEnumerable<FutureSingleTransaction> GetPlanned(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureSingleTransactions
                WHERE AccountId = @AccountId AND IsPlannedPayment = 1
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<FutureSingleTransaction> GetAllPlanned(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM FutureSingleTransactions
                WHERE IsPlannedPayment = 1
                ORDER BY Date;";
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            @"SELECT Id, AccountId, Date, Description, Amount, Category, IsRealized, PostedTransactionId, IsUserCreated, CategoryId,
                     IsPlannedPayment, StatementId, PromotionalBalanceId, PlanningNote, Origin, OriginId, Status";

        private static void Bind(SqliteCommand cmd, FutureSingleTransaction transaction)
        {
            cmd.Parameters.AddWithValue("@Id", transaction.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", transaction.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Date", transaction.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)transaction.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", transaction.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)transaction.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", SqliteCategoryColumns.BindId(transaction.CategoryId));
            cmd.Parameters.AddWithValue("@IsRealized", transaction.IsRealized ? 1 : 0);
            cmd.Parameters.AddWithValue(
                "@PostedTransactionId",
                transaction.PostedTransactionId.HasValue
                    ? transaction.PostedTransactionId.Value.ToString()
                    : DBNull.Value);
            cmd.Parameters.AddWithValue("@IsUserCreated", transaction.IsUserCreated ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsPlannedPayment", transaction.IsPlannedPayment ? 1 : 0);
            cmd.Parameters.AddWithValue("@StatementId", BindGuid(transaction.StatementId));
            cmd.Parameters.AddWithValue("@PromotionalBalanceId", BindGuid(transaction.PromotionalBalanceId));
            cmd.Parameters.AddWithValue("@PlanningNote", (object?)transaction.PlanningNote ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Origin", (int)transaction.Origin);
            cmd.Parameters.AddWithValue("@OriginId", BindGuid(transaction.OriginId));
            cmd.Parameters.AddWithValue("@Status", (int)transaction.Status);
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
                PostedTransactionId = reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7)),
                IsUserCreated = reader.FieldCount > 8 && !reader.IsDBNull(8) && reader.GetInt32(8) == 1,
                CategoryId = SqliteCategoryColumns.ReadId(reader, 9),
                IsPlannedPayment = ReadBool(reader, 10),
                StatementId = SqliteCategoryColumns.ReadId(reader, 11),
                PromotionalBalanceId = SqliteCategoryColumns.ReadId(reader, 12),
                PlanningNote = reader.FieldCount > 13 && !reader.IsDBNull(13) ? reader.GetString(13) : null,
                Origin = ReadEnum(reader, 14, ExpectedOrigin.Manual),
                OriginId = SqliteCategoryColumns.ReadId(reader, 15),
                Status = ReadEnum(reader, 16, ExpectedStatus.Planned)
            };
        }

        private static T ReadEnum<T>(SqliteDataReader reader, int index, T fallback) where T : struct, Enum =>
            reader.FieldCount > index && !reader.IsDBNull(index)
                ? (T)Enum.ToObject(typeof(T), reader.GetInt32(index))
                : fallback;

        private static bool ReadBool(SqliteDataReader reader, int index) =>
            reader.FieldCount > index && !reader.IsDBNull(index) && reader.GetInt32(index) == 1;

        private static object BindGuid(Guid? id) =>
            id is Guid value && value != Guid.Empty ? value.ToString() : DBNull.Value;

        private static IEnumerable<FutureSingleTransaction> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<FutureSingleTransaction>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE FutureSingleTransactions ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }
    }
}
