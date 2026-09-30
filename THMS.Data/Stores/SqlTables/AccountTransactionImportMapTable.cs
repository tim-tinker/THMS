using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Transactions;

namespace THMS.Data.Stores.SqlTables
{
    public class AccountTransactionImportMapTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AccountTransactionImportMaps (
                    AccountId TEXT PRIMARY KEY,
                    DateColumn TEXT NOT NULL DEFAULT '',
                    DescriptionColumn TEXT NOT NULL DEFAULT '',
                    UseDebitCredit INTEGER NOT NULL DEFAULT 0,
                    AmountColumn TEXT NOT NULL DEFAULT '',
                    DebitColumn TEXT NOT NULL DEFAULT '',
                    CreditColumn TEXT NOT NULL DEFAULT '',
                    FlipSign INTEGER NOT NULL DEFAULT 0
                );";
            cmd.ExecuteNonQuery();
        }

        public void Upsert(SqliteConnection conn, TransactionFileColumnMap map)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AccountTransactionImportMaps
                (AccountId, DateColumn, DescriptionColumn, UseDebitCredit, AmountColumn, DebitColumn, CreditColumn, FlipSign)
                VALUES
                (@AccountId, @DateColumn, @DescriptionColumn, @UseDebitCredit, @AmountColumn, @DebitColumn, @CreditColumn, @FlipSign)
                ON CONFLICT(AccountId) DO UPDATE SET
                    DateColumn = excluded.DateColumn,
                    DescriptionColumn = excluded.DescriptionColumn,
                    UseDebitCredit = excluded.UseDebitCredit,
                    AmountColumn = excluded.AmountColumn,
                    DebitColumn = excluded.DebitColumn,
                    CreditColumn = excluded.CreditColumn,
                    FlipSign = excluded.FlipSign;";
            Bind(cmd, map);
            cmd.ExecuteNonQuery();
        }

        public TransactionFileColumnMap? Get(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT AccountId, DateColumn, DescriptionColumn, UseDebitCredit,
                       AmountColumn, DebitColumn, CreditColumn, FlipSign
                FROM AccountTransactionImportMaps
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }

        public void Delete(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM AccountTransactionImportMaps WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void Bind(SqliteCommand cmd, TransactionFileColumnMap map)
        {
            cmd.Parameters.AddWithValue("@AccountId", map.AccountId.ToString());
            cmd.Parameters.AddWithValue("@DateColumn", map.DateColumn ?? "");
            cmd.Parameters.AddWithValue("@DescriptionColumn", map.DescriptionColumn ?? "");
            cmd.Parameters.AddWithValue("@UseDebitCredit", map.UseDebitCredit ? 1 : 0);
            cmd.Parameters.AddWithValue("@AmountColumn", map.AmountColumn ?? "");
            cmd.Parameters.AddWithValue("@DebitColumn", map.DebitColumn ?? "");
            cmd.Parameters.AddWithValue("@CreditColumn", map.CreditColumn ?? "");
            cmd.Parameters.AddWithValue("@FlipSign", map.FlipSign ? 1 : 0);
        }

        private static TransactionFileColumnMap Read(SqliteDataReader reader) =>
            new()
            {
                AccountId = Guid.Parse(reader.GetString(0)),
                DateColumn = reader.IsDBNull(1) ? "" : reader.GetString(1),
                DescriptionColumn = reader.IsDBNull(2) ? "" : reader.GetString(2),
                UseDebitCredit = !reader.IsDBNull(3) && reader.GetInt32(3) != 0,
                AmountColumn = reader.IsDBNull(4) ? "" : reader.GetString(4),
                DebitColumn = reader.IsDBNull(5) ? "" : reader.GetString(5),
                CreditColumn = reader.IsDBNull(6) ? "" : reader.GetString(6),
                FlipSign = !reader.IsDBNull(7) && reader.GetInt32(7) != 0
            };
    }
}
