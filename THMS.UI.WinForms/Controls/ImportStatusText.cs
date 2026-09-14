using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms.Controls
{
    internal static class ImportStatusText
    {
        public static string Loaded(int count, string singular, string plural) =>
            $"{count:N0} {Noun(count, singular, plural)} loaded. Edit cells or delete rows, then click OK to import.";

        public static string Remaining(int count, string singular, string plural) =>
            $"{count:N0} {Noun(count, singular, plural)} remaining.";

        public static string Importing(ImportProgress progress)
        {
            if (!string.IsNullOrWhiteSpace(progress.Phase))
                return progress.Phase!;
            if (progress.Total == 0 || progress.Completed >= progress.Total)
                return "Finishing...";
            return $"Importing {progress.Completed:N0} of {progress.Total:N0}...";
        }

        public static string Imported(ImportResult result, string singular, string plural, bool includeTime = false)
        {
            var text = $"Imported {result.Count:N0} {Noun(result.Count, singular, plural)}";
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
