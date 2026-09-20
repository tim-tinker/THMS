using System.Globalization;
using ExcelDataReader;
using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;

namespace THMS.Ingestion.Importers.Finance
{
    public sealed class ParsedSpreadsheetRecurringTransfer
    {
        public string FromAccountName { get; init; } = "";
        public Guid FromAccountId { get; init; }
        public string ToAccountName { get; init; } = "";
        public Guid ToAccountId { get; init; }
        public string Description { get; init; } = "";
        public RecurrenceFrequency Frequency { get; init; }
        public DateTime NextOccurrence { get; init; }
        public DateTime? LastOccurrence { get; init; }
        public decimal Amount { get; init; }
        public string CategoryName { get; init; } = "";
    }

    public class SpreadsheetRecurringTransferImporter
    {
        private readonly IAccountDataStore _accounts;

        public SpreadsheetRecurringTransferImporter()
            : this(new DataStoreFactory().GetAccountStore())
        {
        }

        public SpreadsheetRecurringTransferImporter(IAccountDataStore accounts)
        {
            _accounts = accounts;
            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);
        }

        public List<ParsedSpreadsheetRecurringTransfer> Parse(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);

            var rows = new List<ParsedSpreadsheetRecurringTransfer>();
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

        private ParsedSpreadsheetRecurringTransfer? ReadRow(
            IExcelDataReader reader,
            IReadOnlyDictionary<string, int> headers)
        {
            var fromName = ReadString(reader, Column(headers, "From Account", "FromAccount", "From"));
            var toName = ReadString(reader, Column(headers, "To Account", "ToAccount", "To"));
            if (string.IsNullOrWhiteSpace(fromName) || string.IsNullOrWhiteSpace(toName))
                return null;

            var from = ResolveAccount(fromName);
            var to = ResolveAccount(toName);
            if (from is null || to is null || from.Id == to.Id)
                return null;

            var description = ReadString(reader, Column(headers, "Description", "Memo", "Payee"));
            if (string.IsNullOrWhiteSpace(description))
                return null;

            var frequencyText = ReadString(reader, Column(headers, "Frequency", "Freq"));
            if (!SpreadsheetRecurringRuleImporter.TryParseFrequency(frequencyText, out var frequency))
                return null;

            var lastOccurrence = ReadDate(reader, Column(headers,
                "Last Occurrence", "LastOccurrence", "Last Date"));
            var nextOccurrence = ReadDate(reader, Column(headers,
                "Date", "Next Occurrence", "NextOccurrence", "Next Date"))
                ?? lastOccurrence;
            if (nextOccurrence is null)
                return null;

            var amount = ReadDecimal(reader, Column(headers, "Amount", "Next Amount"));
            if (amount is null or 0)
                amount = ReadDecimal(reader, Column(headers, "Last Amount", "LastAmount")) ?? amount ?? 0m;

            return new ParsedSpreadsheetRecurringTransfer
            {
                FromAccountName = from.Name,
                FromAccountId = from.Id,
                ToAccountName = to.Name,
                ToAccountId = to.Id,
                Description = description,
                Frequency = frequency,
                NextOccurrence = nextOccurrence.Value.Date,
                LastOccurrence = lastOccurrence?.Date,
                Amount = amount.Value,
                CategoryName = ReadString(reader, Column(headers, "Category"))
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
                if (Matches(value, "From Account", "FromAccount", "To Account", "ToAccount"))
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

            return headers;
        }

        private static Dictionary<string, int> PositionalHeaders(int fieldCount)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["From Account"] = 0,
                ["To Account"] = 1,
                ["Description"] = 2,
                ["Frequency"] = 3,
                ["Last Occurrence"] = 4,
                ["Last Amount"] = 5,
                ["Date"] = 6,
                ["Amount"] = 7,
                ["Category"] = 8
            };
            return headers.Where(pair => pair.Value < Math.Max(fieldCount, 1))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

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
    }
}
