using System.Globalization;
using ExcelDataReader;
using THMS.Domain.Finance.Transactions;

namespace THMS.Ingestion.Importers.Finance
{
    public sealed class MappedTransactionFileRow
    {
        public int SourceRow { get; init; }
        public DateTime? Date { get; init; }
        public string Description { get; init; } = "";
        public decimal? Amount { get; init; }
        public string? Problem { get; init; }
    }

    public class AccountTransactionFileReader
    {
        public AccountTransactionFileReader()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public TransactionFileSheet Read(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);

            var raw = new List<(int RowNumber, string[] Cells)>();
            var fileRow = 0;
            while (reader.Read())
            {
                fileRow++;
                var cells = new string[reader.FieldCount];
                var any = false;
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    cells[i] = Clean(ReadCell(reader, i));
                    any |= cells[i].Length > 0;
                }

                if (any)
                    raw.Add((fileRow, cells));
            }

            if (raw.Count == 0)
                return new TransactionFileSheet();

            var rows = new List<TransactionFileRawRow>();
            for (var i = 1; i < raw.Count; i++)
            {
                rows.Add(new TransactionFileRawRow
                {
                    SourceRow = raw[i].RowNumber,
                    Cells = raw[i].Cells
                });
            }

            return new TransactionFileSheet
            {
                Headers = DisplayHeaders(raw[0].Cells),
                Rows = rows
            };
        }

        public List<MappedTransactionFileRow> Apply(TransactionFileSheet sheet, TransactionFileColumnMap map)
        {
            ArgumentNullException.ThrowIfNull(sheet);
            ArgumentNullException.ThrowIfNull(map);

            var dateIndex = TransactionFileColumnMapping.ColumnIndex(sheet.Headers, map.DateColumn);
            var descriptionIndex = TransactionFileColumnMapping.ColumnIndex(sheet.Headers, map.DescriptionColumn);
            var amountIndex = TransactionFileColumnMapping.ColumnIndex(sheet.Headers, map.AmountColumn);
            var debitIndex = TransactionFileColumnMapping.ColumnIndex(sheet.Headers, map.DebitColumn);
            var creditIndex = TransactionFileColumnMapping.ColumnIndex(sheet.Headers, map.CreditColumn);

            var mapped = new List<MappedTransactionFileRow>();
            foreach (var row in sheet.Rows)
            {
                var dateText = Cell(row.Cells, dateIndex);
                var description = Cell(row.Cells, descriptionIndex);
                var amountText = Cell(row.Cells, amountIndex);
                var debitText = Cell(row.Cells, debitIndex);
                var creditText = Cell(row.Cells, creditIndex);

                var amountBlank = map.UseDebitCredit
                    ? IsBlankAmount(debitText) && IsBlankAmount(creditText)
                    : IsBlankAmount(amountText);
                if (string.IsNullOrWhiteSpace(dateText)
                    && string.IsNullOrWhiteSpace(description)
                    && amountBlank)
                    continue;

                var problems = new List<string>();
                DateTime? date = null;
                if (dateIndex < 0)
                    problems.Add("Date column was not found.");
                else if (!TryParseDate(dateText, out var parsedDate))
                    problems.Add("Date could not be read.");
                else
                    date = parsedDate;

                decimal? amount = null;
                if (map.UseDebitCredit)
                {
                    if (!TryDebitCredit(debitText, creditText, debitIndex, creditIndex, out var signed, out var amountProblem))
                        problems.Add(amountProblem!);
                    else
                        amount = signed;
                }
                else if (amountIndex < 0)
                {
                    problems.Add("Amount column was not found.");
                }
                else if (!TryParseAmount(amountText, out var parsedAmount))
                {
                    problems.Add("Amount could not be read.");
                }
                else
                {
                    amount = map.FlipSign ? -parsedAmount : parsedAmount;
                }

                mapped.Add(new MappedTransactionFileRow
                {
                    SourceRow = row.SourceRow,
                    Date = date,
                    Description = description.Trim(),
                    Amount = amount,
                    Problem = problems.Count == 0 ? null : string.Join(" ", problems)
                });
            }

            return mapped;
        }

        public static IReadOnlyList<string> DisplayHeaders(IReadOnlyList<string> cells)
        {
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headers = new List<string>(cells.Count);
            for (var i = 0; i < cells.Count; i++)
            {
                var name = string.IsNullOrWhiteSpace(cells[i]) ? "Column" : cells[i].Trim();
                seen.TryGetValue(name, out var count);
                count++;
                seen[name] = count;
                headers.Add(count == 1 ? name : $"{name} ({count})");
            }

            return headers;
        }

        private static bool TryDebitCredit(
            string debitText,
            string creditText,
            int debitIndex,
            int creditIndex,
            out decimal amount,
            out string? problem)
        {
            amount = 0;
            problem = null;
            if (debitIndex < 0 || creditIndex < 0)
            {
                problem = "Debit and credit columns were not found.";
                return false;
            }

            var debitBlank = IsBlankAmount(debitText);
            var creditBlank = IsBlankAmount(creditText);
            if (debitBlank && creditBlank)
            {
                problem = "Amount could not be read.";
                return false;
            }

            decimal debit = 0;
            decimal credit = 0;
            if (!debitBlank && !TryParseAmount(debitText, out debit))
            {
                problem = "Debit could not be read.";
                return false;
            }

            if (!creditBlank && !TryParseAmount(creditText, out credit))
            {
                problem = "Credit could not be read.";
                return false;
            }

            amount = credit - debit;
            return true;
        }

        internal static bool TryParseDate(string? value, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out date)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
            {
                date = date.Date;
                return true;
            }

            return false;
        }

        internal static bool TryParseAmount(string? value, out decimal amount)
        {
            amount = 0;
            if (IsBlankAmount(value))
                return false;

            var raw = value!.Trim();
            var negative = false;
            if (raw.StartsWith('(') && raw.EndsWith(')'))
            {
                negative = true;
                raw = raw[1..^1].Trim();
            }

            if (raw.EndsWith('-'))
            {
                negative = !negative;
                raw = raw[..^1].Trim();
            }

            raw = raw.Replace("$", "", StringComparison.Ordinal)
                .Replace(",", "", StringComparison.Ordinal)
                .Replace(" ", "", StringComparison.Ordinal);
            if (raw.StartsWith('+'))
                raw = raw[1..];

            if (!decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount)
                && !decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out amount))
                return false;

            if (negative)
                amount = -amount;
            return true;
        }

        private static bool IsBlankAmount(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            var trimmed = value.Trim();
            return trimmed is "-" or "—" or "–";
        }

        private static string Cell(IReadOnlyList<string> cells, int index) =>
            index >= 0 && index < cells.Count ? cells[index] : "";

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        private static string ReadCell(IExcelDataReader reader, int index)
        {
            if (reader.FieldCount <= index)
                return "";

            var value = reader.GetValue(index);
            return value switch
            {
                null => "",
                DateTime date => date.ToString("d", CultureInfo.InvariantCulture),
                double number => number.ToString("G", CultureInfo.InvariantCulture),
                float number => number.ToString("G", CultureInfo.InvariantCulture),
                decimal number => number.ToString("G", CultureInfo.InvariantCulture),
                _ => value.ToString()?.Trim() ?? ""
            };
        }

        private static string Clean(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            return value.Trim().TrimStart('\uFEFF');
        }
    }
}
