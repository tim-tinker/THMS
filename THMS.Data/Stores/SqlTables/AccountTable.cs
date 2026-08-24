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
                    ClassType TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Account account)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Accounts (Id, Name, Institution, AccountNumber, Type, BalanceAsOf, ClassType)
                VALUES (@Id, @Name, @Institution, @AccountNumber, @Type, @BalanceAsOf, @ClassType)
                ON CONFLICT(Id) DO UPDATE SET
                    Name = excluded.Name,
                    Institution = excluded.Institution,
                    AccountNumber = excluded.AccountNumber,
                    Type = excluded.Type,
                    BalanceAsOf = excluded.BalanceAsOf,
                    ClassType = excluded.ClassType;";

            cmd.Parameters.AddWithValue("@Id", account.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", account.Name);
            cmd.Parameters.AddWithValue("@Institution", account.Institution);
            cmd.Parameters.AddWithValue("@AccountNumber", account.AccountNumber);
            cmd.Parameters.AddWithValue("@Type", account.Type.ToString());
            cmd.Parameters.AddWithValue("@BalanceAsOf", (object?)account.BalanceAsOf ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ClassType", account.GetType().Name);

            cmd.ExecuteNonQuery();
        }

        public (string Name, string Institution, string AccountNumber, AccountType Type, DateTime? BalanceAsOf, string ClassType)?
            GetBase(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Name, Institution, AccountNumber, Type, BalanceAsOf, ClassType
                FROM Accounts
                WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return (
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                Enum.Parse<AccountType>(reader.GetString(3)),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                reader.GetString(5)
            );
        }

        public IEnumerable<Guid> GetAllIds(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT Id FROM Accounts ORDER BY Name;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                yield return Guid.Parse(reader.GetString(0));
        }

        public void Delete(SqliteConnection conn, Guid id)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Accounts WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
