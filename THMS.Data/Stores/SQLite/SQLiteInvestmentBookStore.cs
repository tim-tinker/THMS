using System.Globalization;
using Microsoft.Data.Sqlite;
using THMS.Domain.Finance.Investments;

namespace THMS.Data.Stores.SQLite
{
    public sealed class SQLiteInvestmentBookStore : IInvestmentBookStore
    {
        private readonly string _connectionString;

        public SQLiteInvestmentBookStore(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            using var conn = Open();
            InvestmentBookSql.Initialize(conn);
        }

        public IReadOnlyList<InvestmentAction> GetActions(Guid accountId)
        {
            using var conn = Open();
            return InvestmentBookSql.GetActions(conn, accountId);
        }

        public void UpsertAction(InvestmentAction action)
        {
            using var conn = Open();
            InvestmentBookSql.UpsertAction(conn, action);
            FinanceDataRevision.NoteChanged();
        }

        public IReadOnlyList<SecurityQuote> GetQuotes(Guid accountId)
        {
            using var conn = Open();
            return InvestmentBookSql.GetQuotes(conn, accountId);
        }

        public void UpsertQuote(SecurityQuote quote)
        {
            using var conn = Open();
            InvestmentBookSql.UpsertQuote(conn, quote);
            FinanceDataRevision.NoteChanged();
        }

        public IReadOnlyList<InvestmentSecurity> GetSecurities()
        {
            using var conn = Open();
            return InvestmentBookSql.GetSecurities(conn);
        }

        public void DeleteAccount(Guid accountId)
        {
            using var conn = Open();
            InvestmentBookSql.DeleteAccount(conn, accountId);
            FinanceDataRevision.NoteChanged();
        }

        public void UpsertSecurity(InvestmentSecurity security)
        {
            using var conn = Open();
            InvestmentBookSql.UpsertSecurity(conn, security);
        }

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }

    internal static class InvestmentBookSql
    {
        public static void Initialize(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS InvestmentSecurities (
                    Symbol TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    IsSweep INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS SecurityQuotes (
                    Id TEXT PRIMARY KEY,
                    AccountId TEXT NOT NULL,
                    Symbol TEXT NOT NULL,
                    AsOf TEXT NOT NULL,
                    Price TEXT,
                    AnnualIncomePerShare TEXT,
                    DistributionRate TEXT,
                    AmountPerShare TEXT,
                    ExDate TEXT,
                    PayDate TEXT,
                    UNIQUE(AccountId, Symbol, AsOf)
                );

                UPDATE SecurityQuotes SET Price = NULL WHERE Price IS NOT NULL;

                CREATE TABLE IF NOT EXISTS InvestmentActions (
                    Id TEXT PRIMARY KEY,
                    AccountId TEXT NOT NULL,
                    ImportKey TEXT NOT NULL,
                    Sequence INTEGER NOT NULL,
                    RunDate TEXT NOT NULL,
                    ActionText TEXT NOT NULL,
                    Symbol TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    Amount TEXT,
                    Quantity TEXT,
                    Price TEXT,
                    SplitFactor TEXT,
                    CashChange TEXT,
                    ShareChange TEXT,
                    Kind INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    CashBalanceText TEXT,
                    FileCashBalance TEXT,
                    BookCashAfter TEXT,
                    Note TEXT,
                    LedgerTransactionId TEXT,
                    UNIQUE(AccountId, ImportKey)
                );";
            cmd.ExecuteNonQuery();
        }

        public static void DeleteAccount(SqliteConnection conn, Guid accountId)
        {
            Execute(conn, "DELETE FROM InvestmentActions WHERE AccountId = @Id;", accountId);
            Execute(conn, "DELETE FROM SecurityQuotes WHERE AccountId = @Id;", accountId);
        }

        public static List<InvestmentAction> GetActions(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, AccountId, ImportKey, Sequence, RunDate, ActionText, Symbol, Description,
                       Amount, Quantity, Price, SplitFactor, CashChange, ShareChange, Kind, Status,
                       CashBalanceText, FileCashBalance, BookCashAfter, Note, LedgerTransactionId
                FROM InvestmentActions
                WHERE AccountId = @Id
                ORDER BY Sequence;";
            cmd.Parameters.AddWithValue("@Id", accountId.ToString());
            var list = new List<InvestmentAction>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new InvestmentAction
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    AccountId = Guid.Parse(reader.GetString(1)),
                    ImportKey = reader.GetString(2),
                    Sequence = reader.GetInt32(3),
                    RunDate = ReadDate(reader.GetString(4)),
                    ActionText = reader.GetString(5),
                    Symbol = reader.GetString(6),
                    Description = reader.GetString(7),
                    Amount = ReadDecimal(reader, 8),
                    Quantity = ReadDecimal(reader, 9),
                    Price = ReadDecimal(reader, 10),
                    SplitFactor = ReadDecimal(reader, 11),
                    CashChange = ReadDecimal(reader, 12),
                    ShareChange = ReadDecimal(reader, 13),
                    Kind = (InvestmentActionKind)reader.GetInt32(14),
                    Status = (InvestmentActionStatus)reader.GetInt32(15),
                    CashBalanceText = ReadString(reader, 16),
                    FileCashBalance = ReadDecimal(reader, 17),
                    BookCashAfter = ReadDecimal(reader, 18),
                    Note = ReadString(reader, 19),
                    LedgerTransactionId = ReadGuid(reader, 20)
                });
            }

            return list;
        }

        public static void UpsertAction(SqliteConnection conn, InvestmentAction action)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO InvestmentActions (
                    Id, AccountId, ImportKey, Sequence, RunDate, ActionText, Symbol, Description,
                    Amount, Quantity, Price, SplitFactor, CashChange, ShareChange, Kind, Status,
                    CashBalanceText, FileCashBalance, BookCashAfter, Note, LedgerTransactionId)
                VALUES (
                    @Id, @AccountId, @ImportKey, @Sequence, @RunDate, @ActionText, @Symbol, @Description,
                    @Amount, @Quantity, @Price, @SplitFactor, @CashChange, @ShareChange, @Kind, @Status,
                    @CashBalanceText, @FileCashBalance, @BookCashAfter, @Note, @LedgerTransactionId)
                ON CONFLICT(AccountId, ImportKey) DO UPDATE SET
                    Sequence = excluded.Sequence,
                    RunDate = excluded.RunDate,
                    ActionText = excluded.ActionText,
                    Symbol = excluded.Symbol,
                    Description = excluded.Description,
                    Amount = excluded.Amount,
                    Quantity = excluded.Quantity,
                    Price = excluded.Price,
                    SplitFactor = excluded.SplitFactor,
                    CashChange = excluded.CashChange,
                    ShareChange = excluded.ShareChange,
                    Kind = excluded.Kind,
                    Status = excluded.Status,
                    CashBalanceText = excluded.CashBalanceText,
                    FileCashBalance = excluded.FileCashBalance,
                    BookCashAfter = excluded.BookCashAfter,
                    Note = excluded.Note,
                    LedgerTransactionId = excluded.LedgerTransactionId;";
            cmd.Parameters.AddWithValue("@Id", action.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", action.AccountId.ToString());
            cmd.Parameters.AddWithValue("@ImportKey", action.ImportKey);
            cmd.Parameters.AddWithValue("@Sequence", action.Sequence);
            cmd.Parameters.AddWithValue("@RunDate", WriteDate(action.RunDate));
            cmd.Parameters.AddWithValue("@ActionText", action.ActionText ?? "");
            cmd.Parameters.AddWithValue("@Symbol", action.Symbol ?? "");
            cmd.Parameters.AddWithValue("@Description", action.Description ?? "");
            AddDecimal(cmd, "@Amount", action.Amount);
            AddDecimal(cmd, "@Quantity", action.Quantity);
            AddDecimal(cmd, "@Price", action.Price);
            AddDecimal(cmd, "@SplitFactor", action.SplitFactor);
            AddDecimal(cmd, "@CashChange", action.CashChange);
            AddDecimal(cmd, "@ShareChange", action.ShareChange);
            cmd.Parameters.AddWithValue("@Kind", (int)action.Kind);
            cmd.Parameters.AddWithValue("@Status", (int)action.Status);
            cmd.Parameters.AddWithValue("@CashBalanceText", (object?)action.CashBalanceText ?? DBNull.Value);
            AddDecimal(cmd, "@FileCashBalance", action.FileCashBalance);
            AddDecimal(cmd, "@BookCashAfter", action.BookCashAfter);
            cmd.Parameters.AddWithValue("@Note", (object?)action.Note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LedgerTransactionId", action.LedgerTransactionId?.ToString() ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public static List<SecurityQuote> GetQuotes(SqliteConnection conn, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, AccountId, Symbol, AsOf, AnnualIncomePerShare, DistributionRate,
                       AmountPerShare, ExDate, PayDate
                FROM SecurityQuotes
                WHERE AccountId = @Id;";
            cmd.Parameters.AddWithValue("@Id", accountId.ToString());
            var list = new List<SecurityQuote>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SecurityQuote
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    AccountId = Guid.Parse(reader.GetString(1)),
                    Symbol = reader.GetString(2),
                    AsOf = ReadDate(reader.GetString(3)),
                    AnnualIncomePerShare = ReadDecimal(reader, 4),
                    DistributionRate = ReadDecimal(reader, 5),
                    AmountPerShare = ReadDecimal(reader, 6),
                    ExDate = ReadOptionalDate(reader, 7),
                    PayDate = ReadOptionalDate(reader, 8)
                });
            }

            return list;
        }

        public static void UpsertQuote(SqliteConnection conn, SecurityQuote quote)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO SecurityQuotes (
                    Id, AccountId, Symbol, AsOf, Price, AnnualIncomePerShare, DistributionRate,
                    AmountPerShare, ExDate, PayDate)
                VALUES (
                    @Id, @AccountId, @Symbol, @AsOf, NULL, @AnnualIncomePerShare, @DistributionRate,
                    @AmountPerShare, @ExDate, @PayDate)
                ON CONFLICT(AccountId, Symbol, AsOf) DO UPDATE SET
                    Price = NULL,
                    AnnualIncomePerShare = excluded.AnnualIncomePerShare,
                    DistributionRate = excluded.DistributionRate,
                    AmountPerShare = excluded.AmountPerShare,
                    ExDate = excluded.ExDate,
                    PayDate = excluded.PayDate;";
            cmd.Parameters.AddWithValue("@Id", quote.Id.ToString());
            cmd.Parameters.AddWithValue("@AccountId", quote.AccountId.ToString());
            cmd.Parameters.AddWithValue("@Symbol", quote.Symbol);
            cmd.Parameters.AddWithValue("@AsOf", WriteDate(quote.AsOf));
            AddDecimal(cmd, "@AnnualIncomePerShare", quote.AnnualIncomePerShare);
            AddDecimal(cmd, "@DistributionRate", quote.DistributionRate);
            AddDecimal(cmd, "@AmountPerShare", quote.AmountPerShare);
            cmd.Parameters.AddWithValue("@ExDate", quote.ExDate is DateTime ex ? WriteDate(ex) : DBNull.Value);
            cmd.Parameters.AddWithValue("@PayDate", quote.PayDate is DateTime pay ? WriteDate(pay) : DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public static List<InvestmentSecurity> GetSecurities(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Symbol, Name, IsSweep FROM InvestmentSecurities;";
            var list = new List<InvestmentSecurity>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new InvestmentSecurity
                {
                    Symbol = reader.GetString(0),
                    Name = reader.GetString(1),
                    IsSweep = reader.GetInt32(2) != 0
                });
            }

            return list;
        }

        public static void UpsertSecurity(SqliteConnection conn, InvestmentSecurity security)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO InvestmentSecurities (Symbol, Name, IsSweep)
                VALUES (@Symbol, @Name, @IsSweep)
                ON CONFLICT(Symbol) DO UPDATE SET
                    Name = excluded.Name,
                    IsSweep = excluded.IsSweep;";
            cmd.Parameters.AddWithValue("@Symbol", security.Symbol);
            cmd.Parameters.AddWithValue("@Name", security.Name ?? "");
            cmd.Parameters.AddWithValue("@IsSweep", security.IsSweep ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        private static void Execute(SqliteConnection conn, string sql, Guid accountId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Id", accountId.ToString());
            cmd.ExecuteNonQuery();
        }

        private static void AddDecimal(SqliteCommand cmd, string name, decimal? value) =>
            cmd.Parameters.AddWithValue(name, value?.ToString(CultureInfo.InvariantCulture) ?? (object)DBNull.Value);

        private static decimal? ReadDecimal(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
                return null;
            return decimal.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture);
        }

        private static string? ReadString(SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

        private static Guid? ReadGuid(SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));

        private static DateTime ReadDate(string text) =>
            DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static DateTime? ReadOptionalDate(SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? null : ReadDate(reader.GetString(ordinal));

        private static string WriteDate(DateTime value) =>
            value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
