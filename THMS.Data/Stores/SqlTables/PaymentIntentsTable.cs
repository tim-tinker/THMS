using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class PaymentIntentsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PaymentIntents (
                    Id TEXT PRIMARY KEY,
                    Source TEXT NOT NULL,
                    SourceId TEXT,
                    DestinationAccountId TEXT NOT NULL,
                    FundingAccountId TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    PayDate TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    MatchedPostedTransactionId TEXT,
                    MatchedCounterpartTransactionId TEXT,
                    Description TEXT,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Add(SqliteConnection conn, PaymentIntent intent)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PaymentIntents
                (Id, Source, SourceId, DestinationAccountId, FundingAccountId, Amount, PayDate, Status,
                 MatchedPostedTransactionId, MatchedCounterpartTransactionId, Description, CreatedOn, ModifiedOn)
                VALUES
                (@Id, @Source, @SourceId, @DestinationAccountId, @FundingAccountId, @Amount, @PayDate, @Status,
                 @MatchedPostedTransactionId, @MatchedCounterpartTransactionId, @Description, @CreatedOn, @ModifiedOn);";
            Bind(cmd, intent);
            cmd.ExecuteNonQuery();
        }

        public void Update(SqliteConnection conn, PaymentIntent intent)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE PaymentIntents SET
                    Source = @Source,
                    SourceId = @SourceId,
                    DestinationAccountId = @DestinationAccountId,
                    FundingAccountId = @FundingAccountId,
                    Amount = @Amount,
                    PayDate = @PayDate,
                    Status = @Status,
                    MatchedPostedTransactionId = @MatchedPostedTransactionId,
                    MatchedCounterpartTransactionId = @MatchedCounterpartTransactionId,
                    Description = @Description,
                    ModifiedOn = @ModifiedOn
                WHERE Id = @Id;";
            Bind(cmd, intent);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM PaymentIntents WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public PaymentIntent? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM PaymentIntents WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<PaymentIntent> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM PaymentIntents ORDER BY PayDate;";
            return ReadAll(cmd);
        }

        public IEnumerable<PaymentIntent> GetByStatus(SqliteConnection conn, PaymentIntentStatus status)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM PaymentIntents WHERE Status = @Status ORDER BY PayDate;";
            cmd.Parameters.AddWithValue("@Status", status.ToString());
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            @"SELECT Id, Source, SourceId, DestinationAccountId, FundingAccountId, Amount, PayDate, Status,
                     MatchedPostedTransactionId, MatchedCounterpartTransactionId, Description, CreatedOn, ModifiedOn";

        private static void Bind(SqliteCommand cmd, PaymentIntent intent)
        {
            cmd.Parameters.AddWithValue("@Id", intent.Id.ToString());
            cmd.Parameters.AddWithValue("@Source", intent.Source.ToString());
            cmd.Parameters.AddWithValue("@SourceId", BindGuid(intent.SourceId));
            cmd.Parameters.AddWithValue("@DestinationAccountId", intent.DestinationAccountId.ToString());
            cmd.Parameters.AddWithValue("@FundingAccountId", intent.FundingAccountId.ToString());
            cmd.Parameters.AddWithValue("@Amount", intent.Amount);
            cmd.Parameters.AddWithValue("@PayDate", intent.PayDate);
            cmd.Parameters.AddWithValue("@Status", intent.Status.ToString());
            cmd.Parameters.AddWithValue("@MatchedPostedTransactionId", BindGuid(intent.MatchedPostedTransactionId));
            cmd.Parameters.AddWithValue("@MatchedCounterpartTransactionId", BindGuid(intent.MatchedCounterpartTransactionId));
            cmd.Parameters.AddWithValue("@Description", (object?)intent.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedOn", intent.CreatedOn);
            cmd.Parameters.AddWithValue("@ModifiedOn", intent.ModifiedOn);
        }

        private static PaymentIntent Read(SqliteDataReader reader)
        {
            return new PaymentIntent
            {
                Id = Guid.Parse(reader.GetString(0)),
                Source = Enum.Parse<PaymentIntentSource>(reader.GetString(1)),
                SourceId = ReadGuid(reader, 2),
                DestinationAccountId = Guid.Parse(reader.GetString(3)),
                FundingAccountId = Guid.Parse(reader.GetString(4)),
                Amount = (decimal)(double)reader.GetDouble(5),
                PayDate = reader.GetDateTime(6),
                Status = Enum.Parse<PaymentIntentStatus>(reader.GetString(7)),
                MatchedPostedTransactionId = ReadGuid(reader, 8),
                MatchedCounterpartTransactionId = ReadGuid(reader, 9),
                Description = reader.IsDBNull(10) ? null : reader.GetString(10),
                CreatedOn = reader.GetDateTime(11),
                ModifiedOn = reader.GetDateTime(12)
            };
        }

        private static IEnumerable<PaymentIntent> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<PaymentIntent>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static object BindGuid(Guid? id) =>
            id is Guid value && value != Guid.Empty ? value.ToString() : DBNull.Value;

        private static Guid? ReadGuid(SqliteDataReader reader, int index) =>
            reader.IsDBNull(index) ? null : Guid.Parse(reader.GetString(index));
    }
}
