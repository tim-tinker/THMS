using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Investments;

namespace THMS.Data.Stores.SqlTables
{
    public class AccountInvestmentImportMapTable
    {
        public void InitializeSchema(SqliteConnection conn)
        {
            using var positions = conn.CreateCommand();
            positions.CommandText = @"
                CREATE TABLE IF NOT EXISTS AccountInvestmentPositionImportMaps (
                    AccountId TEXT PRIMARY KEY,
                    SymbolColumn TEXT NOT NULL DEFAULT '',
                    DescriptionColumn TEXT NOT NULL DEFAULT '',
                    QuantityColumn TEXT NOT NULL DEFAULT '',
                    PriceColumn TEXT NOT NULL DEFAULT '',
                    ValueColumn TEXT NOT NULL DEFAULT '',
                    CostBasisColumn TEXT NOT NULL DEFAULT '',
                    AverageCostColumn TEXT NOT NULL DEFAULT '',
                    AccountNumberColumn TEXT NOT NULL DEFAULT '',
                    AccountNameColumn TEXT NOT NULL DEFAULT '',
                    AsOfColumn TEXT NOT NULL DEFAULT '',
                    AmountPerShareColumn TEXT NOT NULL DEFAULT '',
                    EstimatedIncomeColumn TEXT NOT NULL DEFAULT '',
                    DistributionRateColumn TEXT NOT NULL DEFAULT '',
                    RateAsOfColumn TEXT NOT NULL DEFAULT '',
                    ExDateColumn TEXT NOT NULL DEFAULT '',
                    PayDateColumn TEXT NOT NULL DEFAULT ''
                );";
            positions.ExecuteNonQuery();

            using var history = conn.CreateCommand();
            history.CommandText = @"
                CREATE TABLE IF NOT EXISTS AccountInvestmentHistoryImportMaps (
                    AccountId TEXT PRIMARY KEY,
                    DateColumn TEXT NOT NULL DEFAULT '',
                    ActionColumn TEXT NOT NULL DEFAULT '',
                    SymbolColumn TEXT NOT NULL DEFAULT '',
                    DescriptionColumn TEXT NOT NULL DEFAULT '',
                    QuantityColumn TEXT NOT NULL DEFAULT '',
                    PriceColumn TEXT NOT NULL DEFAULT '',
                    AmountColumn TEXT NOT NULL DEFAULT '',
                    CashBalanceColumn TEXT NOT NULL DEFAULT ''
                );";
            history.ExecuteNonQuery();
        }

        public void UpsertPosition(SqliteConnection conn, InvestmentPositionColumnMap map)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AccountInvestmentPositionImportMaps
                (AccountId, SymbolColumn, DescriptionColumn, QuantityColumn, PriceColumn, ValueColumn,
                 CostBasisColumn, AverageCostColumn, AccountNumberColumn, AccountNameColumn, AsOfColumn,
                 AmountPerShareColumn, EstimatedIncomeColumn, DistributionRateColumn, RateAsOfColumn,
                 ExDateColumn, PayDateColumn)
                VALUES
                (@AccountId, @SymbolColumn, @DescriptionColumn, @QuantityColumn, @PriceColumn, @ValueColumn,
                 @CostBasisColumn, @AverageCostColumn, @AccountNumberColumn, @AccountNameColumn, @AsOfColumn,
                 @AmountPerShareColumn, @EstimatedIncomeColumn, @DistributionRateColumn, @RateAsOfColumn,
                 @ExDateColumn, @PayDateColumn)
                ON CONFLICT(AccountId) DO UPDATE SET
                    SymbolColumn = excluded.SymbolColumn,
                    DescriptionColumn = excluded.DescriptionColumn,
                    QuantityColumn = excluded.QuantityColumn,
                    PriceColumn = excluded.PriceColumn,
                    ValueColumn = excluded.ValueColumn,
                    CostBasisColumn = excluded.CostBasisColumn,
                    AverageCostColumn = excluded.AverageCostColumn,
                    AccountNumberColumn = excluded.AccountNumberColumn,
                    AccountNameColumn = excluded.AccountNameColumn,
                    AsOfColumn = excluded.AsOfColumn,
                    AmountPerShareColumn = excluded.AmountPerShareColumn,
                    EstimatedIncomeColumn = excluded.EstimatedIncomeColumn,
                    DistributionRateColumn = excluded.DistributionRateColumn,
                    RateAsOfColumn = excluded.RateAsOfColumn,
                    ExDateColumn = excluded.ExDateColumn,
                    PayDateColumn = excluded.PayDateColumn;";
            BindPosition(cmd, map);
            cmd.ExecuteNonQuery();
        }

        public InvestmentPositionColumnMap? GetPosition(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT AccountId, SymbolColumn, DescriptionColumn, QuantityColumn, PriceColumn, ValueColumn,
                       CostBasisColumn, AverageCostColumn, AccountNumberColumn, AccountNameColumn, AsOfColumn,
                       AmountPerShareColumn, EstimatedIncomeColumn, DistributionRateColumn, RateAsOfColumn,
                       ExDateColumn, PayDateColumn
                FROM AccountInvestmentPositionImportMaps
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;
            return new InvestmentPositionColumnMap
            {
                AccountId = Guid.Parse(reader.GetString(0)),
                SymbolColumn = Text(reader, 1),
                DescriptionColumn = Text(reader, 2),
                QuantityColumn = Text(reader, 3),
                PriceColumn = Text(reader, 4),
                ValueColumn = Text(reader, 5),
                CostBasisColumn = Text(reader, 6),
                AverageCostColumn = Text(reader, 7),
                AccountNumberColumn = Text(reader, 8),
                AccountNameColumn = Text(reader, 9),
                AsOfColumn = Text(reader, 10),
                AmountPerShareColumn = Text(reader, 11),
                EstimatedIncomeColumn = Text(reader, 12),
                DistributionRateColumn = Text(reader, 13),
                RateAsOfColumn = Text(reader, 14),
                ExDateColumn = Text(reader, 15),
                PayDateColumn = Text(reader, 16)
            };
        }

        public void UpsertHistory(SqliteConnection conn, InvestmentHistoryColumnMap map)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AccountInvestmentHistoryImportMaps
                (AccountId, DateColumn, ActionColumn, SymbolColumn, DescriptionColumn,
                 QuantityColumn, PriceColumn, AmountColumn, CashBalanceColumn)
                VALUES
                (@AccountId, @DateColumn, @ActionColumn, @SymbolColumn, @DescriptionColumn,
                 @QuantityColumn, @PriceColumn, @AmountColumn, @CashBalanceColumn)
                ON CONFLICT(AccountId) DO UPDATE SET
                    DateColumn = excluded.DateColumn,
                    ActionColumn = excluded.ActionColumn,
                    SymbolColumn = excluded.SymbolColumn,
                    DescriptionColumn = excluded.DescriptionColumn,
                    QuantityColumn = excluded.QuantityColumn,
                    PriceColumn = excluded.PriceColumn,
                    AmountColumn = excluded.AmountColumn,
                    CashBalanceColumn = excluded.CashBalanceColumn;";
            BindHistory(cmd, map);
            cmd.ExecuteNonQuery();
        }

        public InvestmentHistoryColumnMap? GetHistory(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT AccountId, DateColumn, ActionColumn, SymbolColumn, DescriptionColumn,
                       QuantityColumn, PriceColumn, AmountColumn, CashBalanceColumn
                FROM AccountInvestmentHistoryImportMaps
                WHERE AccountId = @AccountId;";
            cmd.Parameters.AddWithValue("@AccountId", accountId.ToString());
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;
            return new InvestmentHistoryColumnMap
            {
                AccountId = Guid.Parse(reader.GetString(0)),
                DateColumn = Text(reader, 1),
                ActionColumn = Text(reader, 2),
                SymbolColumn = Text(reader, 3),
                DescriptionColumn = Text(reader, 4),
                QuantityColumn = Text(reader, 5),
                PriceColumn = Text(reader, 6),
                AmountColumn = Text(reader, 7),
                CashBalanceColumn = Text(reader, 8)
            };
        }

        public void Delete(SqliteConnection conn, Guid accountId)
        {
            using var positions = conn.CreateCommand();
            positions.CommandText = "DELETE FROM AccountInvestmentPositionImportMaps WHERE AccountId = @AccountId;";
            positions.Parameters.AddWithValue("@AccountId", accountId.ToString());
            positions.ExecuteNonQuery();

            using var history = conn.CreateCommand();
            history.CommandText = "DELETE FROM AccountInvestmentHistoryImportMaps WHERE AccountId = @AccountId;";
            history.Parameters.AddWithValue("@AccountId", accountId.ToString());
            history.ExecuteNonQuery();
        }

        private static void BindPosition(SqliteCommand cmd, InvestmentPositionColumnMap map)
        {
            cmd.Parameters.AddWithValue("@AccountId", map.AccountId.ToString());
            cmd.Parameters.AddWithValue("@SymbolColumn", map.SymbolColumn ?? "");
            cmd.Parameters.AddWithValue("@DescriptionColumn", map.DescriptionColumn ?? "");
            cmd.Parameters.AddWithValue("@QuantityColumn", map.QuantityColumn ?? "");
            cmd.Parameters.AddWithValue("@PriceColumn", map.PriceColumn ?? "");
            cmd.Parameters.AddWithValue("@ValueColumn", map.ValueColumn ?? "");
            cmd.Parameters.AddWithValue("@CostBasisColumn", map.CostBasisColumn ?? "");
            cmd.Parameters.AddWithValue("@AverageCostColumn", map.AverageCostColumn ?? "");
            cmd.Parameters.AddWithValue("@AccountNumberColumn", map.AccountNumberColumn ?? "");
            cmd.Parameters.AddWithValue("@AccountNameColumn", map.AccountNameColumn ?? "");
            cmd.Parameters.AddWithValue("@AsOfColumn", map.AsOfColumn ?? "");
            cmd.Parameters.AddWithValue("@AmountPerShareColumn", map.AmountPerShareColumn ?? "");
            cmd.Parameters.AddWithValue("@EstimatedIncomeColumn", map.EstimatedIncomeColumn ?? "");
            cmd.Parameters.AddWithValue("@DistributionRateColumn", map.DistributionRateColumn ?? "");
            cmd.Parameters.AddWithValue("@RateAsOfColumn", map.RateAsOfColumn ?? "");
            cmd.Parameters.AddWithValue("@ExDateColumn", map.ExDateColumn ?? "");
            cmd.Parameters.AddWithValue("@PayDateColumn", map.PayDateColumn ?? "");
        }

        private static void BindHistory(SqliteCommand cmd, InvestmentHistoryColumnMap map)
        {
            cmd.Parameters.AddWithValue("@AccountId", map.AccountId.ToString());
            cmd.Parameters.AddWithValue("@DateColumn", map.DateColumn ?? "");
            cmd.Parameters.AddWithValue("@ActionColumn", map.ActionColumn ?? "");
            cmd.Parameters.AddWithValue("@SymbolColumn", map.SymbolColumn ?? "");
            cmd.Parameters.AddWithValue("@DescriptionColumn", map.DescriptionColumn ?? "");
            cmd.Parameters.AddWithValue("@QuantityColumn", map.QuantityColumn ?? "");
            cmd.Parameters.AddWithValue("@PriceColumn", map.PriceColumn ?? "");
            cmd.Parameters.AddWithValue("@AmountColumn", map.AmountColumn ?? "");
            cmd.Parameters.AddWithValue("@CashBalanceColumn", map.CashBalanceColumn ?? "");
        }

        private static string Text(SqliteDataReader reader, int index) =>
            reader.IsDBNull(index) ? "" : reader.GetString(index);
    }
}
