using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Accounts;

namespace THMS.Data.Stores.SqlTables
{
    public class ExternalAccountLinksTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ExternalAccountLinks (
                    AccountId TEXT PRIMARY KEY,
                    Provider TEXT NOT NULL,
                    ItemId TEXT NOT NULL,
                    AccessToken TEXT NOT NULL,
                    PlaidAccountId TEXT NOT NULL,
                    InstitutionId TEXT NOT NULL,
                    AccountMask TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, Guid accountId, ExternalAccountLink link)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ExternalAccountLinks
                (AccountId, Provider, ItemId, AccessToken, PlaidAccountId, InstitutionId, AccountMask)
                VALUES
                (@AccountId, @Provider, @ItemId, @AccessToken, @PlaidAccountId, @InstitutionId, @AccountMask)
                ON CONFLICT(AccountId) DO UPDATE SET
                    Provider = excluded.Provider,
                    ItemId = excluded.ItemId,
                    AccessToken = excluded.AccessToken,
                    PlaidAccountId = excluded.PlaidAccountId,
                    InstitutionId = excluded.InstitutionId,
                    AccountMask = excluded.AccountMask;";

            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.Parameters.AddWithValue("@Provider", link.Provider);
            cmd.Parameters.AddWithValue("@ItemId", link.ItemId);
            cmd.Parameters.AddWithValue("@AccessToken", link.AccessToken);
            cmd.Parameters.AddWithValue("@PlaidAccountId", link.PlaidAccountId);
            cmd.Parameters.AddWithValue("@InstitutionId", link.InstitutionId);
            cmd.Parameters.AddWithValue("@AccountMask", link.AccountMask);
            cmd.ExecuteNonQuery();
        }

        public ExternalAccountLink? Get(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Provider, ItemId, AccessToken, PlaidAccountId, InstitutionId, AccountMask
                FROM ExternalAccountLinks
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new ExternalAccountLink
            {
                Provider = reader.GetString(0),
                ItemId = reader.GetString(1),
                AccessToken = reader.GetString(2),
                PlaidAccountId = reader.GetString(3),
                InstitutionId = reader.GetString(4),
                AccountMask = reader.GetString(5)
            };
        }

        public void Delete(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM ExternalAccountLinks WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.ExecuteNonQuery();
        }
    }
}
