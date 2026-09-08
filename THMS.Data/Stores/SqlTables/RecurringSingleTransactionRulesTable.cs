using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class RecurringSingleTransactionRulesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS RecurringSingleTransactionRules (
                    Id TEXT PRIMARY KEY,
                    AccountId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    Amount REAL NOT NULL,
                    Category TEXT,
                    Frequency TEXT NOT NULL,
                    LastOccurrence TEXT,
                    EndDate TEXT,
                    IsActive INTEGER NOT NULL,
                    IsFinalPaymentDifferent INTEGER NOT NULL,
                    FinalPaymentAmount REAL,
                    IsUserCreated INTEGER NOT NULL DEFAULT 0
                );";
            cmd.ExecuteNonQuery();
            EnsureColumn(conn, "LastOccurrence", "TEXT");
            EnsureColumn(conn, "IsUserCreated", "INTEGER NOT NULL DEFAULT 0");
            SqliteCategoryColumns.EnsureCategoryId(conn, "RecurringSingleTransactionRules");
        }

        public void Add(SqliteConnection conn, RecurringSingleTransactionRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO RecurringSingleTransactionRules
                (Id, AccountId, Date, Description, Amount, Category, CategoryId, Frequency,
                 LastOccurrence, EndDate, IsActive, IsFinalPaymentDifferent, FinalPaymentAmount, IsUserCreated)
                VALUES
                (@Id, @AccountId, @Date, @Description, @Amount, @Category, @CategoryId, @Frequency,
                 @LastOccurrence, @EndDate, @IsActive, @IsFinalPaymentDifferent, @FinalPaymentAmount, @IsUserCreated);";
            Bind(cmd, rule);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, RecurringSingleTransactionRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE RecurringSingleTransactionRules SET
                    AccountId = @AccountId,
                    Date = @Date,
                    Description = @Description,
                    Amount = @Amount,
                    Category = @Category,
                    CategoryId = @CategoryId,
                    Frequency = @Frequency,
                    LastOccurrence = @LastOccurrence,
                    EndDate = @EndDate,
                    IsActive = @IsActive,
                    IsFinalPaymentDifferent = @IsFinalPaymentDifferent,
                    FinalPaymentAmount = @FinalPaymentAmount,
                    IsUserCreated = @IsUserCreated
                WHERE Id = @Id;";
            Bind(cmd, rule);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM RecurringSingleTransactionRules WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public RecurringSingleTransactionRule? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM RecurringSingleTransactionRules WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<RecurringSingleTransactionRule> GetByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM RecurringSingleTransactionRules
                WHERE AccountId = @AccountId
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<RecurringSingleTransactionRule> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM RecurringSingleTransactionRules ORDER BY Date;";
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            @"SELECT Id, AccountId, Date, Description, Amount, Category, Frequency,
                     LastOccurrence, EndDate, IsActive, IsFinalPaymentDifferent, FinalPaymentAmount, IsUserCreated, CategoryId";

        private static void Bind(SqliteCommand cmd, RecurringSingleTransactionRule rule)
        {
            cmd.Parameters.AddWithValue("@Id", rule.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", rule.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Date", rule.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)rule.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", rule.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)rule.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", SqliteCategoryColumns.BindId(rule.CategoryId));
            cmd.Parameters.AddWithValue("@Frequency", rule.Frequency.ToString());
            cmd.Parameters.AddWithValue("@LastOccurrence", (object?)rule.LastOccurrence ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EndDate", (object?)rule.EndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", rule.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsFinalPaymentDifferent", rule.IsFinalPaymentDifferent ? 1 : 0);
            cmd.Parameters.AddWithValue("@FinalPaymentAmount", (object?)rule.FinalPaymentAmount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsUserCreated", rule.IsUserCreated ? 1 : 0);
        }

        private static RecurringSingleTransactionRule Read(SqliteDataReader reader)
        {
            return new RecurringSingleTransactionRule
            {
                Id = Guid.Parse(reader.GetString(0)),
                AccountId = Guid.Parse(reader.GetString(1)),
                Date = reader.GetDateTime(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                Amount = (decimal)(double)reader.GetDouble(4),
                Category = reader.IsDBNull(5) ? null : reader.GetString(5),
                Frequency = Enum.Parse<RecurrenceFrequency>(reader.GetString(6)),
                LastOccurrence = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                EndDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                IsActive = reader.GetInt32(9) == 1,
                IsFinalPaymentDifferent = reader.GetInt32(10) == 1,
                FinalPaymentAmount = reader.IsDBNull(11) ? null : (decimal)(double)reader.GetDouble(11),
                IsUserCreated = reader.FieldCount > 12 && !reader.IsDBNull(12) && reader.GetInt32(12) == 1,
                CategoryId = SqliteCategoryColumns.ReadId(reader, 13)
            };
        }

        private static IEnumerable<RecurringSingleTransactionRule> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<RecurringSingleTransactionRule>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE RecurringSingleTransactionRules ADD COLUMN IF NOT EXISTS {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }
    }
}
