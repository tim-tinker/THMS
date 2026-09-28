using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms.Controls
{
    internal static class ImportStatusText
    {
        public static string Imported(ImportResult result, string singular, string plural, bool includeTime = false) =>
            Completed("Imported", result, singular, plural, includeTime);

        public static string Reconciled(ImportResult result) =>
            Completed("Reconciled", result, "transaction", "transactions");

        private static string Completed(
            string verb,
            ImportResult result,
            string singular,
            string plural,
            bool includeTime = false)
        {
            var text = $"{verb} {result.Count:N0} {Noun(result.Count, singular, plural)}";
            if (result.Start is DateTime start && result.End is DateTime end)
            {
                var format = includeTime ? "g" : "d";
                text += $" from {start.ToString(format)} to {end.ToString(format)}";
            }

            return text + ".";
        }

        private static string Noun(int count, string singular, string plural) =>
            count == 1 ? singular : plural;
    }
}
