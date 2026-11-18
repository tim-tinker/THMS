using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class LoanAccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS LoanAccounts (
                    AccountId TEXT PRIMARY KEY,
                    Principal REAL NOT NULL,
                    InterestRate REAL NOT NULL,
                    TermMonths INTEGER NOT NULL,
                    SecuredAssetId TEXT
                );";
            cmd.ExecuteNonQuery();

            EnsureColumn(conn, "TermMonths", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "SecuredAssetId", "TEXT");
        }

        public void Upsert(SqliteConnection conn, LoanAccount account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO LoanAccounts (AccountId, Principal, InterestRate, TermMonths, SecuredAssetId)
                VALUES (@AccountId, @Principal, @InterestRate, @TermMonths, @SecuredAssetId)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Principal = excluded.Principal,
                    InterestRate = excluded.InterestRate,
                    TermMonths = excluded.TermMonths,
                    SecuredAssetId = excluded.SecuredAssetId;";

            cmd.Parameters.AddWithValue("@AccountId", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Principal", account.Principal);
            cmd.Parameters.AddWithValue("@InterestRate", account.InterestRate);
            cmd.Parameters.AddWithValue("@TermMonths", account.TermMonths);
            cmd.Parameters.AddWithValue("@SecuredAssetId", GuidText(account.SecuredAssetId));
            cmd.ExecuteNonQuery();
        }

        public (decimal Principal, decimal InterestRate, int TermMonths, Guid? SecuredAssetId)? Get(
            SqliteConnection conn,
            Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Principal, InterestRate, TermMonths, SecuredAssetId
                FROM LoanAccounts
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                (decimal)(double)reader.GetDouble(0),
                (decimal)(double)reader.GetDouble(1),
                reader.GetInt32(2),
                ReadGuid(reader, 3)
            );
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM LoanAccounts WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            if (ColumnExists(conn, columnName))
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE LoanAccounts ADD COLUMN {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection conn, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(LoanAccounts);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static object GuidText(Guid? id) =>
            id is Guid value && value != Guid.Empty ? value.ToString() : DBNull.Value;

        private static Guid? ReadGuid(SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));
    }
}
