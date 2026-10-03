using System.Globalization;

namespace THMS.Logic.Finance.Transactions
{
    public static class LedgerEntryDate
    {
        private static readonly string[] Formats =
        [
            "M/d/yyyy",
            "M/d/yy",
            "MM/dd/yyyy",
            "MM/dd/yy",
            "M-d-yyyy",
            "M-d-yy",
            "yyyy-M-d"
        ];

        public static bool TryParse(string? text, out DateTime date) =>
            TryParse(text, CultureInfo.CurrentCulture, out date);

        public static bool TryParse(string? text, CultureInfo culture, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var trimmed = text.Trim();
            if (!DateTime.TryParseExact(trimmed, Formats, culture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
                && !DateTime.TryParse(trimmed, culture, DateTimeStyles.AllowWhiteSpaces, out parsed))
                return false;

            date = parsed.Date;
            return true;
        }
    }
}
