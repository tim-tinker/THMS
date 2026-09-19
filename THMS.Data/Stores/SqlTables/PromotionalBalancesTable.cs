using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Planning;

namespace THMS.Data.Stores.SqlTables
{
    public class PromotionalBalancesTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PromotionalBalances (
                    Id TEXT PRIMARY KEY,
                    StatementId TEXT NOT NULL,
                    AccountId TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Deadline TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    SortOrder INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS IX_PromotionalBalances_StatementId
                    ON PromotionalBalances (StatementId);";
            cmd.ExecuteNonQuery();
            EnsureColumn(conn, "StatementId", "TEXT NOT NULL DEFAULT ''");
            EnsureColumn(conn, "SortOrder", "INTEGER NOT NULL DEFAULT 0");
            EnsureColumn(conn, "DateAcquired", "TEXT NOT NULL DEFAULT '0001-01-01'");
            EnsureColumn(conn, "InitialAmount", "REAL NOT NULL DEFAULT 0");
        }

        public void ReplaceAll(SqliteConnection conn, Guid statementId, Guid accountId, IEnumerable<PromotionalBalance> promotions)
        {
            DeleteByStatement(conn, statementId);
            var order = 0;
            foreach (var promo in promotions)
            {
                if (promo.Id == Guid.Empty)
                    promo.Id = Guid.NewGuid();
                if (promo.AccountId == Guid.Empty)
                    promo.AccountId = accountId;
                Insert(conn, statementId, promo, order++);
            }
        }

        public void DeleteByStatement(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM PromotionalBalances WHERE StatementId = @StatementId;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.ExecuteNonQuery();
        }

        public List<PromotionalBalance> GetByStatement(SqliteConnection conn, Guid statementId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, AccountId, Amount, Deadline, Type, DateAcquired, InitialAmount
                FROM PromotionalBalances
                WHERE StatementId = @StatementId
                ORDER BY SortOrder, Id;";
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            using var reader = cmd.ExecuteReader();
            var list = new List<PromotionalBalance>();
            while (reader.Read())
            {
                var typeName = reader.IsDBNull(4) ? nameof(PromoType.LumpSum) : reader.GetString(4);
                if (!Enum.TryParse<PromoType>(typeName, out var type))
                    type = PromoType.LumpSum;

                var currentBalance = (decimal)(double)reader.GetDouble(2);
                var initialAmount = reader.IsDBNull(6) ? 0 : (decimal)(double)reader.GetDouble(6);
                if (initialAmount == 0 && currentBalance != 0)
                    initialAmount = currentBalance;

                list.Add(new PromotionalBalance
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    AccountId = Guid.Parse(reader.GetString(1)),
                    CurrentBalance = currentBalance,
                    Deadline = reader.GetDateTime(3),
                    Type = type,
                    DateAcquired = reader.IsDBNull(5) ? default : reader.GetDateTime(5),
                    InitialAmount = initialAmount
                });
            }

            return list;
        }

        private static void Insert(SqliteConnection conn, Guid statementId, PromotionalBalance promo, int sortOrder)
        {
            var currentBalance = promo.CurrentBalance;
            var initialAmount = promo.InitialAmount > 0 ? promo.InitialAmount : currentBalance;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PromotionalBalances
                (Id, StatementId, AccountId, Amount, Deadline, Type, SortOrder, DateAcquired, InitialAmount)
                VALUES
                (@Id, @StatementId, @AccountId, @Amount, @Deadline, @Type, @SortOrder, @DateAcquired, @InitialAmount);";
            cmd.Parameters.AddWithValue("@Id", promo.Id.ToString());
            cmd.Parameters.AddWithValue("@StatementId", statementId.ToString());
            cmd.Parameters.AddWithValue("@AccountId", promo.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Amount", currentBalance);
            cmd.Parameters.AddWithValue("@Deadline", promo.Deadline);
            cmd.Parameters.AddWithValue("@Type", promo.Type.ToString());
            cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
            cmd.Parameters.AddWithValue("@DateAcquired", promo.DateAcquired);
            cmd.Parameters.AddWithValue("@InitialAmount", initialAmount);
            cmd.ExecuteNonQuery();
        }

        private static void EnsureColumn(SqliteConnection conn, string columnName, string columnDef)
        {
            if (ColumnExists(conn, columnName))
                return;

            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE PromotionalBalances ADD COLUMN {columnName} {columnDef};";
            alter.ExecuteNonQuery();
        }

        private static bool ColumnExists(SqliteConnection conn, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(PromotionalBalances);";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
