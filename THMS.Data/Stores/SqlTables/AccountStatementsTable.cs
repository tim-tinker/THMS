using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class AccountStatementsTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AccountStatements (
                    Id TEXT PRIMARY KEY,
                    AccountId TEXT NOT NULL,
                    StatementDate TEXT NOT NULL,
                    DueDate TEXT NOT NULL,
                    AmountDue REAL NOT NULL,
                    MinimumPayment REAL NOT NULL,
                    Notes TEXT,
                    StatementType TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_AccountStatements_AccountId
                    ON AccountStatements (AccountId);
                CREATE INDEX IF NOT EXISTS IX_AccountStatements_DueDate
                    ON AccountStatements (DueDate);";
            cmd.ExecuteNonQuery();
            EnsureColumn(conn, "StatementType", "TEXT NOT NULL DEFAULT 'CreditCard'");
        }

        public void Upsert(SqliteConnection conn, AccountStatement statement)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AccountStatements
                (Id, AccountId, StatementDate, DueDate, AmountDue, MinimumPayment, Notes, StatementType)
                VALUES
                (@Id, @AccountId, @StatementDate, @DueDate, @AmountDue, @MinimumPayment, @Notes, @StatementType)
                ON CONFLICT(Id) DO UPDATE SET
                    AccountId = excluded.AccountId,
                    StatementDate = excluded.StatementDate,
                    DueDate = excluded.DueDate,
                    AmountDue = excluded.AmountDue,
                    MinimumPayment = excluded.MinimumPayment,
                    Notes = excluded.Notes,
                    StatementType = excluded.StatementType;";
            cmd.Parameters.AddWithValue("@Id", statement.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", statement.AccountId.ToString());
            cmd.Parameters.AddWithValue("@StatementDate", statement.StatementDate);
            cmd.Parameters.AddWithValue("@DueDate", statement.DueDate);
            cmd.Parameters.AddWithValue("@AmountDue", statement.AmountDue);
            cmd.Parameters.AddWithValue("@MinimumPayment", statement.MinimumPayment);
            cmd.Parameters.AddWithValue("@Notes", (object?)statement.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatementType", statement.Type.ToString());
            cmd.ExecuteNonQuery();
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AccountStatements WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        public AccountStatementRow? Get(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + " FROM AccountStatements WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public List<AccountStatementRow> GetForAccount(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM AccountStatements
                WHERE AccountId = @AccountId
                ORDER BY DueDate;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            return ReadAll(cmd);
        }

        public List<AccountStatementRow> GetUpcoming(SqliteConnection conn, DateTime asOf)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectColumns + @"
                FROM AccountStatements
                WHERE DueDate >= @AsOf
                ORDER BY DueDate;";
            cmd.Parameters.AddWithValue("@AsOf", asOf.Date);
            return ReadAll(cmd);
        }

        private const string SelectColumns =
            "SELECT Id, AccountId, StatementDate, DueDate, AmountDue, MinimumPayment, Notes, StatementType";

        private static List<AccountStatementRow> ReadAll(SqliteCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            var list = new List<AccountStatementRow>();
            while (reader.Read())
                list.Add(Read(reader));
            return list;
        }

        private static AccountStatementRow Read(SqliteDataReader reader)
        {
            return new AccountStatementRow(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetDateTime(2),
                reader.GetDateTime(3),
                (decimal)(double)reader.GetDouble(4),
                (decimal)(double)reader.GetDouble(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? nameof(StatementType.CreditCard) : reader.GetString(7));
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            if (ColumnExists(conn, "AccountStatements", columnName))
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE AccountStatements ADD COLUMN {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    public sealed record AccountStatementRow(
        Guid Id,
        Guid AccountId,
        DateTime StatementDate,
        DateTime DueDate,
        decimal AmountDue,
        decimal MinimumPayment,
        string? Notes,
        string StatementType);
}
