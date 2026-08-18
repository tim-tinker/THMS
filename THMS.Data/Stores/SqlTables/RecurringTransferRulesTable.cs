using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class RecurringTransferRulesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS RecurringTransferRules (
                    Id TEXT PRIMARY KEY,
                    Date TEXT NOT NULL,
                    Description TEXT,
                    Amount REAL NOT NULL,
                    Category TEXT,
                    FromAccountId TEXT NOT NULL,
                    ToAccountId TEXT NOT NULL,
                    Frequency TEXT NOT NULL,
                    EndDate TEXT,
                    IsActive INTEGER NOT NULL,
                    IsFinalPaymentDifferent INTEGER NOT NULL,
                    FinalPaymentAmount REAL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Add(SqliteConnection conn, RecurringTransferRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO RecurringTransferRules
                (Id, Date, Description, Amount, Category, FromAccountId, ToAccountId,
                 Frequency, EndDate, IsActive, IsFinalPaymentDifferent, FinalPaymentAmount)
                VALUES
                (@Id, @Date, @Description, @Amount, @Category, @FromAccountId, @ToAccountId,
                 @Frequency, @EndDate, @IsActive, @IsFinalPaymentDifferent, @FinalPaymentAmount);";
            Bind(cmd, rule);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, RecurringTransferRule rule)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE RecurringTransferRules SET
                    Date = @Date,
                    Description = @Description,
                    Amount = @Amount,
                    Category = @Category,
                    FromAccountId = @FromAccountId,
                    ToAccountId = @ToAccountId,
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
            cmd.CommandText = "DELETE FROM RecurringTransferRules WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public RecurringTransferRule? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM RecurringTransferRules WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<RecurringTransferRule> GetByAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM RecurringTransferRules
                WHERE FromAccountId = @AccountId OR ToAccountId = @AccountId
                ORDER BY Date;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public IEnumerable<RecurringTransferRule> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM RecurringTransferRules ORDER BY Date;";
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            @"SELECT Id, Date, Description, Amount, Category, FromAccountId, ToAccountId,
                     Frequency, EndDate, IsActive, IsFinalPaymentDifferent, FinalPaymentAmount";

        private static void Bind(SqliteCommand cmd, RecurringTransferRule rule)
        {
            cmd.Parameters.AddWithValue("@Id", rule.Id.ToString());
            cmd.Parameters.AddWithValue("@Date", rule.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)rule.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", rule.Amount);
            cmd.Parameters.AddWithValue("@Category", (object?)rule.Category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FromAccountId", rule.FromAccountId.ToString());
            cmd.Parameters.AddWithValue("@ToAccountId", rule.ToAccountId.ToString());
            cmd.Parameters.AddWithValue("@Frequency", rule.Frequency.ToString());
            cmd.Parameters.AddWithValue("@EndDate", (object?)rule.EndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", rule.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsFinalPaymentDifferent", rule.IsFinalPaymentDifferent ? 1 : 0);
            cmd.Parameters.AddWithValue("@FinalPaymentAmount", (object?)rule.FinalPaymentAmount ?? DBNull.Value);
        }

        private static RecurringTransferRule Read(SqliteDataReader reader)
        {
            return new RecurringTransferRule
            {
                Id = Guid.Parse(reader.GetString(0)),
                Date = reader.GetDateTime(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Amount = (decimal)(double)reader.GetDouble(3),
                Category = reader.IsDBNull(4) ? null : reader.GetString(4),
                FromAccountId = Guid.Parse(reader.GetString(5)),
                ToAccountId = Guid.Parse(reader.GetString(6)),
                Frequency = Enum.Parse<RecurrenceFrequency>(reader.GetString(7)),
                EndDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                IsActive = reader.GetInt32(9) == 1,
                IsFinalPaymentDifferent = reader.GetInt32(10) == 1,
                FinalPaymentAmount = reader.IsDBNull(11) ? null : (decimal)(double)reader.GetDouble(11)
            };
        }

        private static IEnumerable<RecurringTransferRule> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<RecurringTransferRule>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }
    }
}
