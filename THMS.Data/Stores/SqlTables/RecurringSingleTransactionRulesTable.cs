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
                    EndDate TEXT,
                    IsActive INTEGER NOT NULL,
                    IsFinalPaymentDifferent INTEGER NOT NULL,
                    FinalPaymentAmount REAL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Add(SqliteConnection conn, RecurringSingleTransactionRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO RecurringSingleTransactionRules
                (Id, AccountId, Date, Description, Amount, Category, Frequency,
                 EndDate, IsActive, IsFinalPaymentDifferent, FinalPaymentAmount)
                VALUES
                (@Id, @AccountId, @Date, @Description, @Amount, @Category, @Frequency,
                 @EndDate, @IsActive, @IsFinalPaymentDifferent, @FinalPaymentAmount);";
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
                    Frequency = @Frequency,
                    EndDate = @EndDate,
                    IsActive = @IsActive,
                    IsFinalPaymentDifferent = @IsFinalPaymentDifferent,
                    FinalPaymentAmount = @FinalPaymentAmount
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
                     EndDate, IsActive, IsFinalPaymentDifferent, FinalPaymentAmount";

        private static void Bind(SqliteCommand cmd, RecurringSingleTransactionRule rule)
        {
            cmd.Parameters.AddWithValue("@Id", rule.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", rule.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Date", rule.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)rule.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", rule.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)rule.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Frequency", rule.Frequency.ToString());
            cmd.Parameters.AddWithValue("@EndDate", (object?)rule.EndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", rule.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsFinalPaymentDifferent", rule.IsFinalPaymentDifferent ? 1 : 0);
            cmd.Parameters.AddWithValue("@FinalPaymentAmount", (object?)rule.FinalPaymentAmount ?? DBNull.Value);
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
                EndDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                IsActive = reader.GetInt32(8) == 1,
                IsFinalPaymentDifferent = reader.GetInt32(9) == 1,
                FinalPaymentAmount = reader.IsDBNull(10) ? null : (decimal)(double)reader.GetDouble(10)
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
    }
}
