using ExcelDataReader;

namespace THMS.Ingestion.Importers.Finance
{
    public sealed record CategorySpreadsheetRow(string Name, string? ParentName);

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
                var name = ReadString(reader, 0);
                var second = ReadString(reader, 1);
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(second))
                    continue;
                rows.Add([BlankToNull(name), BlankToNull(second)]);
            }

            return ParseRows(rows);
        }

        public static List<CategorySpreadsheetRow> ParseRows(IReadOnlyList<string?[]> rows)
        {
            if (rows.Count == 0)
                return [];

            var start = 0;
            var nameParent = false;
            if (IsHeader(rows[0][0], rows[0][1], out nameParent))
                start = 1;

            return nameParent
                ? ParseNameParent(rows, start)
                : ParseOutline(rows, start);
        }

        private static List<CategorySpreadsheetRow> ParseOutline(IReadOnlyList<string?[]> rows, int start)
        {
            var result = new List<CategorySpreadsheetRow>();
            string? currentParent = null;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = start; i < rows.Count; i++)
            {
                var parent = rows[i][0];
                var child = rows[i][1];
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    currentParent = parent.Trim();
                    Add(result, seen, currentParent, parentName: null);
                }

                if (!string.IsNullOrWhiteSpace(child))
                    Add(result, seen, child.Trim(), currentParent);
            }

            return result;
        }

        private static List<CategorySpreadsheetRow> ParseNameParent(IReadOnlyList<string?[]> rows, int start)
        {
            var result = new List<CategorySpreadsheetRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = start; i < rows.Count; i++)
            {
                var name = rows[i][0];
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                Add(result, seen, name.Trim(), BlankToNull(rows[i][1]));
            }

            return result;
        }

        private static void Add(
            List<CategorySpreadsheetRow> result,
            HashSet<string> seen,
            string name,
            string? parentName)
        {
            if (string.Equals(name, parentName, StringComparison.OrdinalIgnoreCase))
                parentName = null;

            if (!seen.Add(name))
            {
                var index = result.FindIndex(r =>
                    string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                    result[index] = new CategorySpreadsheetRow(name, parentName);
                return;
            }

            result.Add(new CategorySpreadsheetRow(name, parentName));
        }

        private static bool IsHeader(string? first, string? second, out bool nameParent)
        {
            nameParent = false;
            if (!Matches(first, "Category") && !Matches(first, "Name"))
                return false;

            if (Matches(second, "Parent"))
            {
                nameParent = true;
                return true;
            }

            return Matches(second, "Child")
                || Matches(second, "Subcategory")
                || Matches(second, "Sub-Category")
                || Matches(second, "Sub Category");
        }

        private static bool Matches(string? value, string expected) =>
            string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

        private static IExcelDataReader CreateReader(Stream stream, string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return ExcelReaderFactory.CreateCsvReader(stream);

            return ExcelReaderFactory.CreateReader(stream);
        }

        private static string ReadString(IExcelDataReader reader, int index)
        {
            if (reader.FieldCount <= index)
                return string.Empty;
            return reader.GetValue(index)?.ToString()?.Trim() ?? string.Empty;
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
