using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class AccountTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Institution TEXT NOT NULL,
                    AccountNumber TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    BalanceAsOf TEXT,
                    ClassType TEXT NOT NULL,
                    WebsiteUrl TEXT NOT NULL DEFAULT '',
                    AutoPay INTEGER NOT NULL DEFAULT 0,
                    AutoPayFromAccountId TEXT
                );";
            cmd.ExecuteNonQuery();

            EnsureColumn(conn, "WebsiteUrl", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(conn, "AutoPay", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "AutoPayFromAccountId", "TEXT");
        }

        public void Upsert(SqliteConnection conn, Account account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Accounts
                    (Id, Name, Institution, AccountNumber, Type, BalanceAsOf, ClassType,
                     WebsiteUrl, AutoPay, AutoPayFromAccountId)
                VALUES
                    (@Id, @Name, @Institution, @AccountNumber, @Type, @BalanceAsOf, @ClassType,
                     @WebsiteUrl, @AutoPay, @AutoPayFromAccountId)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = excluded.Name,
                    Institution = excluded.Institution,
                    AccountNumber = excluded.AccountNumber,
                    Type = excluded.Type,
                    BalanceAsOf = excluded.BalanceAsOf,
                    ClassType = excluded.ClassType,
                    WebsiteUrl = excluded.WebsiteUrl,
                    AutoPay = excluded.AutoPay,
                    AutoPayFromAccountId = excluded.AutoPayFromAccountId;";

            cmd.Parameters.AddWithValue("@Id", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", account.Name);
            cmd.Parameters.AddWithValue("@Institution", account.Institution);
            cmd.Parameters.AddWithValue("@AccountNumber", account.AccountNumber);
            cmd.Parameters.AddWithValue("@Type", account.Type.ToString());
            cmd.Parameters.AddWithValue("@BalanceAsOf", (object?)account.BalanceAsOf ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ClassType", account.GetType().Name);
            cmd.Parameters.AddWithValue("@WebsiteUrl", account.WebsiteUrl ?? "");
            cmd.Parameters.AddWithValue("@AutoPay", account.AutoPay ? 1 : 0);
            cmd.Parameters.AddWithValue(
                "@AutoPayFromAccountId",
                account.AutoPayFromAccountId is Guid funding && funding != Guid.Empty
                    ? funding.ToString()
                    : DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public Guid? GetIdByName(SqliteConnection conn, string name)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id FROM Accounts WHERE Name = @Name LIMIT 1;";
            cmd.Parameters.AddWithValue("@Name", name);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return Guid.Parse(reader.GetString(0));
        }

        public AccountBase? GetBase(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Name, Institution, AccountNumber, Type, BalanceAsOf, ClassType,
                       WebsiteUrl, AutoPay, AutoPayFromAccountId
                FROM Accounts
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            Guid? autoPayFrom = null;
            if (!reader.IsDBNull(8) && Guid.TryParse(reader.GetString(8), out var funding))
                autoPayFrom = funding;

            return new AccountBase(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                Enum.Parse<AccountType>(reader.GetString(3)),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? "" : reader.GetString(6),
                !reader.IsDBNull(7) && reader.GetInt32(7) != 0,
                autoPayFrom);
        }

        public IEnumerable<Guid> GetAllIds(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id FROM Accounts ORDER BY Name;";

            using var reader = cmd.ExecuteReader();
            var ids = new List<Guid>();
            while (reader.Read())
                ids.Add(Guid.Parse(reader.GetString(0)));
            return ids;
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Accounts WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            if (ColumnExists(conn, columnName))
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE Accounts ADD COLUMN {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection conn, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(Accounts);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public sealed record AccountBase(
            string Name,
            string Institution,
            string AccountNumber,
            AccountType Type,
            DateTime? BalanceAsOf,
            string ClassType,
            string WebsiteUrl,
            bool AutoPay,
            Guid? AutoPayFromAccountId);
    }
}
