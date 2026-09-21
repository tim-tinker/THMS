using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class TransactionReconciliationsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TransactionReconciliations (
                    Id TEXT PRIMARY KEY,
                    ImportedTransactionId TEXT NOT NULL,
                    ExpectedTransactionId TEXT,
                    RecommendedExpectedId TEXT,
                    AcceptedOn TEXT NOT NULL,
                    RuleLastOccurrenceBefore TEXT,
                    RuleNextOccurrenceBefore TEXT,
                    CreatedOn TEXT NOT NULL,
                    ModifiedOn TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Add(SqliteConnection conn, TransactionReconciliation reconciliation)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TransactionReconciliations
                (Id, ImportedTransactionId, ExpectedTransactionId, RecommendedExpectedId, AcceptedOn,
                 RuleLastOccurrenceBefore, RuleNextOccurrenceBefore, CreatedOn, ModifiedOn)
                VALUES
                (@Id, @ImportedTransactionId, @ExpectedTransactionId, @RecommendedExpectedId, @AcceptedOn,
                 @RuleLastOccurrenceBefore, @RuleNextOccurrenceBefore, @CreatedOn, @ModifiedOn);";
            Bind(cmd, reconciliation);
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM TransactionReconciliations WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public TransactionReconciliation? GetById(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM TransactionReconciliations WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public TransactionReconciliation? GetByImported(SqliteConnection conn, Guid importedId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns +
                " FROM TransactionReconciliations WHERE ImportedTransactionId = @ImportedId ORDER BY AcceptedOn DESC LIMIT 1;";
            cmd.Parameters.AddWithValue("@ImportedId", importedId.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public IEnumerable<TransactionReconciliation> GetAll(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM TransactionReconciliations ORDER BY AcceptedOn;";
            using var reader = cmd.ExecuteReader();
            var list = new List<TransactionReconciliation>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private const string SelectColumns =
            @"SELECT Id, ImportedTransactionId, ExpectedTransactionId, RecommendedExpectedId, AcceptedOn,
                     RuleLastOccurrenceBefore, RuleNextOccurrenceBefore, CreatedOn, ModifiedOn";

        private static void Bind(SqliteCommand cmd, TransactionReconciliation reconciliation)
        {
            cmd.Parameters.AddWithValue("@Id", reconciliation.Id.ToString());
            cmd.Parameters.AddWithValue("@ImportedTransactionId", reconciliation.ImportedTransactionId.ToString());
            cmd.Parameters.AddWithValue("@ExpectedTransactionId", BindGuid(reconciliation.ExpectedTransactionId));
            cmd.Parameters.AddWithValue("@RecommendedExpectedId", BindGuid(reconciliation.RecommendedExpectedId));
            cmd.Parameters.AddWithValue("@AcceptedOn", reconciliation.AcceptedOn);
            cmd.Parameters.AddWithValue("@RuleLastOccurrenceBefore", BindDate(reconciliation.RuleLastOccurrenceBefore));
            cmd.Parameters.AddWithValue("@RuleNextOccurrenceBefore", BindDate(reconciliation.RuleNextOccurrenceBefore));
            cmd.Parameters.AddWithValue("@CreatedOn", reconciliation.CreatedOn);
            cmd.Parameters.AddWithValue("@ModifiedOn", reconciliation.ModifiedOn);
        }

        private static TransactionReconciliation Read(SqliteDataReader reader) =>
            new()
            {
                Id = Guid.Parse(reader.GetString(0)),
                ImportedTransactionId = Guid.Parse(reader.GetString(1)),
                ExpectedTransactionId = ReadGuid(reader, 2),
                RecommendedExpectedId = ReadGuid(reader, 3),
                AcceptedOn = reader.GetDateTime(4),
                RuleLastOccurrenceBefore = ReadDate(reader, 5),
                RuleNextOccurrenceBefore = ReadDate(reader, 6),
                CreatedOn = reader.GetDateTime(7),
                ModifiedOn = reader.GetDateTime(8)
            };

        private static object BindGuid(Guid? id) =>
            id is Guid value && value != Guid.Empty ? value.ToString() : DBNull.Value;

        private static Guid? ReadGuid(SqliteDataReader reader, int index) =>
            reader.IsDBNull(index) ? null : Guid.Parse(reader.GetString(index));

        private static object BindDate(DateTime? value) =>
            value is DateTime date ? date : DBNull.Value;

        private static DateTime? ReadDate(SqliteDataReader reader, int index) =>
            reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    }
}
