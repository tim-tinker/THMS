using System.Globalization;
using ExcelDataReader;
using THMS.Domain.Finance.Transactions;

namespace THMS.Ingestion.Importers.Finance
{
    public sealed record CategorySpreadsheetRow(
        string Name,
        string? ParentName,
        BudgetFrequency? Frequency = null,
        decimal? Amount = null,
        DateTime? PeriodStart = null)
    {
        public bool HasBudget => Frequency is not null || Amount is not null;
    }

    public class SpreadsheetCategoryImporter
    {
        public SpreadsheetCategoryImporter()
        {
            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);
        }

        public List<CategorySpreadsheetRow> Parse(string filePath)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = CreateReader(stream, filePath);

            var rows = new List<string?[]>();
            while (reader.Read())
            {
                var width = Math.Max(reader.FieldCount, 5);
                var cells = new string?[width];
                var any = false;
                for (var i = 0; i < width; i++)
                {
                    cells[i] = BlankToNull(ReadCell(reader, i));
                    any |= cells[i] is not null;
                }

                if (any)
                    rows.Add(cells);
            }

            return ParseRows(rows);
        }

        public static List<CategorySpreadsheetRow> ParseRows(IReadOnlyList<string?[]> rows)
        {
            if (rows.Count == 0)
                return [];

            var columns = ReadHeader(rows[0]);
            var start = columns.HasHeader ? 1 : 0;
            return columns.Outline
                ? ParseOutline(rows, start, columns)
                : ParseNameParent(rows, start, columns);
        }

        private static List<CategorySpreadsheetRow> ParseNameParent(
            IReadOnlyList<string?[]> rows,
            int start,
            ColumnMap columns)
        {
            var result = new List<CategorySpreadsheetRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = start; i < rows.Count; i++)
            {
                var name = Cell(rows[i], columns.Name);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var parent = Cell(rows[i], columns.Parent);
                if (string.Equals(name, parent, StringComparison.OrdinalIgnoreCase))
                    parent = null;

                var row = new CategorySpreadsheetRow(
                    name.Trim(),
                    parent,
                    ParseFrequency(Cell(rows[i], columns.Frequency)),
                    ParseAmount(Cell(rows[i], columns.Amount)),
                    ParseDate(Cell(rows[i], columns.PeriodStart)));

                if (!seen.Add(row.Name))
                {
                    var index = result.FindIndex(r =>
                        string.Equals(r.Name, row.Name, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                        result[index] = row;
                    continue;
                }

                result.Add(row);
            }

            return result;
        }

        private static List<CategorySpreadsheetRow> ParseOutline(
            IReadOnlyList<string?[]> rows,
            int start,
            ColumnMap columns)
        {
            var result = new List<CategorySpreadsheetRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? currentParent = null;

            for (var i = start; i < rows.Count; i++)
            {
                var parent = Cell(rows[i], columns.Name);
                var child = Cell(rows[i], columns.Child);
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    currentParent = parent.Trim();
                    AddOutline(result, seen, currentParent, parentName: null, rows[i], columns);
                }

                if (!string.IsNullOrWhiteSpace(child))
                    AddOutline(result, seen, child.Trim(), currentParent, rows[i], columns);
            }

            return result;
        }

        private static void AddOutline(
            List<CategorySpreadsheetRow> result,
            HashSet<string> seen,
            string name,
            string? parentName,
            string?[] cells,
            ColumnMap columns)
        {
            if (string.Equals(name, parentName, StringComparison.OrdinalIgnoreCase))
                parentName = null;

            var row = new CategorySpreadsheetRow(
                name,
                parentName,
                ParseFrequency(Cell(cells, columns.Frequency)),
                ParseAmount(Cell(cells, columns.Amount)),
                ParseDate(Cell(cells, columns.PeriodStart)));

            if (!seen.Add(name))
            {
                var index = result.FindIndex(r =>
                    string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                    result[index] = row;
                return;
            }

            result.Add(row);
        }

        private static ColumnMap ReadHeader(string?[] first)
        {
            if (!LooksLikeHeader(first))
            {
                return new ColumnMap
                {
                    Name = 0,
                    Parent = 1,
                    Frequency = 2,
                    Amount = 3,
                    PeriodStart = 4
                };
            }

            var map = new ColumnMap { HasHeader = true };
            for (var i = 0; i < first.Length; i++)
            {
                var header = first[i]?.Trim() ?? "";
                if (Matches(header, "Category") || Matches(header, "Name"))
                    map.Name = i;
                else if (Matches(header, "Parent"))
                    map.Parent = i;
                else if (Matches(header, "Child") ||
                         Matches(header, "Subcategory") ||
                         Matches(header, "Sub-Category") ||
                         Matches(header, "Sub Category"))
                {
                    map.Child = i;
                    map.Outline = true;
                }
                else if (Matches(header, "Frequency"))
                    map.Frequency = i;
                else if (Matches(header, "Amount") ||
                         Matches(header, "Default Amount") ||
                         Matches(header, "Budget Amount"))
                    map.Amount = i;
                else if (Matches(header, "Period Start") ||
                         Matches(header, "PeriodStart") ||
                         Matches(header, "Start") ||
                         Matches(header, "Cycle Start"))
                    map.PeriodStart = i;
            }

            return map;
        }

        private static bool LooksLikeHeader(string?[] first)
        {
            var a = first.ElementAtOrDefault(0);
            var b = first.ElementAtOrDefault(1);
            return Matches(a, "Category") || Matches(a, "Name")
                || Matches(b, "Parent") || Matches(b, "Child")
                || Matches(b, "Subcategory") || Matches(b, "Frequency");
        }

        private static string? Cell(string?[] row, int index) =>
            index >= 0 && index < row.Length ? BlankToNull(row[index]) : null;

        private static BudgetFrequency? ParseFrequency(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim().Replace("-", "", StringComparison.Ordinal)
                .Replace(" ", "", StringComparison.Ordinal);
            if (normalized.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
                return BudgetFrequency.Weekly;
            if (normalized.Equals("Biweekly", StringComparison.OrdinalIgnoreCase))
                return BudgetFrequency.Biweekly;
            if (normalized.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
                return BudgetFrequency.Monthly;
            if (normalized.Equals("Quarterly", StringComparison.OrdinalIgnoreCase))
                return BudgetFrequency.Quarterly;
            if (normalized.Equals("Annual", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Yearly", StringComparison.OrdinalIgnoreCase))
                return BudgetFrequency.Annual;

            return Enum.TryParse<BudgetFrequency>(value, true, out var parsed) ? parsed : null;
        }

        private static decimal? ParseAmount(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var raw = value.Trim().Replace("$", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                return amount;
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out amount))
                return amount;
            return null;
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                ? date.Date
                : null;
        }

        private static bool Matches(string? value, string expected) =>
            string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        private static string ReadCell(IExcelDataReader reader, int index)
        {
            if (reader.FieldCount <= index)
                return string.Empty;

            var value = reader.GetValue(index);
            return value switch
            {
                null => string.Empty,
                DateTime date => date.ToString("d", CultureInfo.InvariantCulture),
                double number => number.ToString("G", CultureInfo.InvariantCulture),
                float number => number.ToString("G", CultureInfo.InvariantCulture),
                decimal number => number.ToString("G", CultureInfo.InvariantCulture),
                _ => value.ToString()?.Trim() ?? string.Empty
            };
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private sealed class ColumnMap
        {
            public bool HasHeader { get; set; }
            public bool Outline { get; set; }
            public int Name { get; set; }
            public int Parent { get; set; } = -1;
            public int Child { get; set; } = -1;
            public int Frequency { get; set; } = -1;
            public int Amount { get; set; } = -1;
            public int PeriodStart { get; set; } = -1;
        }
    }
}
