using System.Globalization;
using ExcelDataReader;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;

namespace THMS.Ingestion.Importers.Finance
{
    public sealed class ParsedSpreadsheetStatement
    {
        public string AccountName { get; init; } = "";
        public Guid AccountId { get; init; }
        public StatementType Type { get; init; }
        public DateTime StatementDate { get; init; }
        public DateTime DueDate { get; init; }
        public decimal AmountDue { get; init; }
        public decimal StatementBalance { get; init; }
        public decimal EscrowBalance { get; init; }
        public string? Notes { get; init; }
        public string? PayFromName { get; init; }
        public Guid? PayFromAccountId { get; init; }
    }

    public class SpreadsheetStatementImporter
    {
        private readonly IAccountDataStore _accounts;

        public SpreadsheetStatementImporter()
            : this(new DataStoreFactory().GetAccountStore())
        {
        }

        public SpreadsheetStatementImporter(IAccountDataStore accounts)
        {
            _accounts = accounts;
            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);
        }

        public List<ParsedSpreadsheetStatement> Parse(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);

            var rows = new List<ParsedSpreadsheetStatement>();
            Dictionary<string, int>? headers = null;

            while (reader.Read())
            {
                if (headers is null)
                {
                    if (IsHeaderRow(reader))
                    {
                        headers = ReadHeaders(reader);
                        continue;
                    }

                    headers = PositionalHeaders(reader.FieldCount);
                }

                var parsed = ReadRow(reader, headers);
                if (parsed is not null)
                    rows.Add(parsed);
            }

            return rows;
        }

        private ParsedSpreadsheetStatement? ReadRow(
            IExcelDataReader reader,
            IReadOnlyDictionary<string, int> headers)
        {
            var accountName = ReadString(reader, Column(headers, "Account", "Account Name", "AccountName", "Name"));
            if (string.IsNullOrWhiteSpace(accountName))
                return null;

            var account = ResolveAccount(accountName);
            if (account is null)
                return null;

            var type = StatementAccountMatch.ForAccount(account);
            if (type is null)
                return null;

            var statementDate = ReadDate(reader, Column(headers, "Statement Date", "Stmt Date", "Date"));
            if (statementDate is null)
                return null;

            var dueDate = ReadDate(reader, Column(headers, "Due Date", "Due", "Payment Date"))
                ?? statementDate.Value;
            var amountDue = ReadDecimal(reader, Column(headers,
                "Amount Due", "Amount", "Payment Due", "Min Payment", "Minimum Payment")) ?? 0;
            var statementBalance = ReadDecimal(reader, Column(headers,
                "Statement Balance", "Balance", "Ending Balance", "Principal")) ?? 0;
            var escrow = ReadDecimal(reader, Column(headers, "Escrow Balance", "Escrow")) ?? 0;
            var notes = BlankToNull(ReadString(reader, Column(headers, "Notes", "Note", "Comment")));
            var payFromName = BlankToNull(ReadString(reader, Column(
                headers, "Pay From", "PayFrom", "Funding Account", "Paid From")));
            var payFrom = payFromName is null ? null : ResolveAccount(payFromName);
            if (payFrom is not null && payFrom.Id == account.Id)
                payFrom = null;

            if (type == StatementType.Bank)
            {
                dueDate = statementDate.Value;
                amountDue = 0;
            }

            return new ParsedSpreadsheetStatement
            {
                AccountName = account.Name,
                AccountId = account.Id,
                Type = type.Value,
                StatementDate = statementDate.Value.Date,
                DueDate = dueDate.Date,
                AmountDue = amountDue,
                StatementBalance = statementBalance,
                EscrowBalance = escrow,
                Notes = notes,
                PayFromName = payFrom?.Name ?? payFromName,
                PayFromAccountId = payFrom?.Id
            };
        }

        private Account? ResolveAccount(string name)
        {
            var trimmed = name.Trim();
            return _accounts.GetAccount(trimmed)
                ?? _accounts.GetAllAccounts().FirstOrDefault(a =>
                    string.Equals(a.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        private static bool IsHeaderRow(IExcelDataReader reader)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = ReadString(reader, i);
                if (Matches(value,
                        "Account", "Account Name", "AccountName",
                        "Statement Date", "Stmt Date",
                        "Due Date", "Amount Due"))
                    return true;
            }

            return false;
        }

        private static Dictionary<string, int> ReadHeaders(IExcelDataReader reader)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = ReadString(reader, i);
                if (name.Length > 0 && !headers.ContainsKey(name))
                    headers[name] = i;
            }

            if (!HasAccountHeader(headers))
                headers["Account"] = 0;

            return headers;
        }

        private static Dictionary<string, int> PositionalHeaders(int fieldCount)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Account"] = 0,
                ["Statement Date"] = 1,
                ["Due Date"] = 2,
                ["Amount Due"] = 3,
                ["Statement Balance"] = 4,
                ["Escrow Balance"] = 5,
                ["Notes"] = 6,
                ["Pay From"] = 7
            };
            return headers.Where(pair => pair.Value < Math.Max(fieldCount, 1))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static bool HasAccountHeader(IReadOnlyDictionary<string, int> headers) =>
            headers.Keys.Any(key => Matches(key, "Account", "Account Name", "AccountName", "Name"));

        private static int Column(IReadOnlyDictionary<string, int> headers, params string[] names)
        {
            foreach (var name in names)
            {
                if (headers.TryGetValue(name, out var index))
                    return index;
            }

            return -1;
        }

        private static bool Matches(string? value, params string[] expected) =>
            expected.Any(name => string.Equals(value?.Trim(), name, StringComparison.OrdinalIgnoreCase));

        private static string ReadString(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return "";
            return reader.GetValue(index)?.ToString()?.Trim() ?? "";
        }

        private static DateTime? ReadDate(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return null;

            return reader.GetValue(index) switch
            {
                DateTime date => date.Date,
                double oa when oa >= 1 => DateTime.FromOADate(oa).Date,
                int oa when oa >= 1 => DateTime.FromOADate(oa).Date,
                string text when TryParseDate(text, out var parsed) => parsed,
                _ => null
            };
        }

        private static decimal? ReadDecimal(IExcelDataReader reader, int index)
        {
            if (index < 0 || index >= reader.FieldCount)
                return null;

            return reader.GetValue(index) switch
            {
                null or DBNull => null,
                decimal number => number,
                double number => (decimal)number,
                int number => number,
                long number => number,
                string text when string.IsNullOrWhiteSpace(text) => null,
                string text when decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant) => invariant,
                string text when decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var current) => current,
                _ => null
            };
        }

        private static bool TryParseDate(string text, out DateTime date) =>
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
