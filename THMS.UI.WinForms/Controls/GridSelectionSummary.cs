using System.Globalization;

namespace THMS.UI.WinForms.Controls
{
    internal readonly record struct GridSelectionStats(int Count, int NumericCount, decimal Sum, bool Currency)
    {
        public decimal Average => NumericCount == 0 ? 0 : Sum / NumericCount;
    }

    internal static class GridSelectionSummary
    {
        private static Action<string?>? _show;

        public static void Bind(Action<string?> show) => _show = show;

        public static void Publish(DataGridView? grid) =>
            _show?.Invoke(grid is null ? null : Format(Summarize(grid)));

        public static GridSelectionStats Summarize(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            var count = 0;
            var numericCount = 0;
            decimal sum = 0;
            var currency = true;
            foreach (DataGridViewCell cell in grid.SelectedCells)
            {
                if (cell.RowIndex < 0 || cell.OwningRow is not { IsNewRow: false })
                    continue;

                count++;
                if (!TryGetNumber(cell.Value, out var number))
                    continue;

                numericCount++;
                sum += number;
                if (!IsCurrencyFormat(cell.OwningColumn.DefaultCellStyle.Format))
                    currency = false;
            }

            return new GridSelectionStats(count, numericCount, sum, currency && numericCount > 0);
        }

        public static string? Format(GridSelectionStats stats)
        {
            if (stats.Count < 2)
                return null;
            if (stats.NumericCount == 0)
                return $"Count: {stats.Count:N0}";

            var format = stats.Currency ? "c2" : "n2";
            return $"Count: {stats.Count:N0}    Average: {stats.Average.ToString(format)}    Sum: {stats.Sum.ToString(format)}";
        }

        private static bool TryGetNumber(object? value, out decimal number)
        {
            switch (value)
            {
                case decimal decimalValue:
                    number = decimalValue;
                    return true;
                case double doubleValue when double.IsFinite(doubleValue):
                    number = (decimal)doubleValue;
                    return true;
                case float floatValue when float.IsFinite(floatValue):
                    number = (decimal)floatValue;
                    return true;
                case int intValue:
                    number = intValue;
                    return true;
                case long longValue:
                    number = longValue;
                    return true;
                case short shortValue:
                    number = shortValue;
                    return true;
                case byte byteValue:
                    number = byteValue;
                    return true;
                default:
                    number = 0;
                    return false;
            }
        }

        private static bool IsCurrencyFormat(string? format)
        {
            if (string.IsNullOrEmpty(format) || format[0] is not ('c' or 'C'))
                return false;

            return format.Length == 1 || int.TryParse(format.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }
    }
}
