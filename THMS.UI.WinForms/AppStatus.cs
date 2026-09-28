using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public static class AppStatus
    {
        private static Action<string, bool, int?, int?>? _set;

        public static void Bind(Action<string, bool, int?, int?> set) => _set = set;

        public static void Set(string message, bool busy = false) =>
            _set?.Invoke(message, busy, null, null);

        public static IProgress<ImportProgress> ForImport() =>
            new InlineProgress<ImportProgress>(Report);

        public static void Report(PlaidSyncProgress progress)
        {
            ArgumentNullException.ThrowIfNull(progress);
            Set(progress.Message, busy: !progress.Complete);
        }

        public static void Report(ImportProgress progress)
        {
            var message = Importing(progress);
            int? total = progress.Total > 0 ? progress.Total : null;
            int? completed = total is null ? null : progress.Completed;
            _set?.Invoke(message, true, completed, total);
        }

        private static string Importing(ImportProgress progress)
        {
            if (!string.IsNullOrWhiteSpace(progress.Phase))
                return progress.Phase!;
            var verb = string.IsNullOrWhiteSpace(progress.Activity) ? "Importing" : progress.Activity;
            if (progress.Total <= 0 || progress.Completed >= progress.Total)
                return "Finishing...";
            return $"{verb} {progress.Completed:N0} of {progress.Total:N0}...";
        }

        private sealed class InlineProgress<T>(Action<T> action) : IProgress<T>
        {
            public void Report(T value) => action(value);
        }
    }
}
