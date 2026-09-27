using THMS.Logic.Orchestrators.Finance;

namespace THMS.UI.WinForms
{
    public static class AppStatus
    {
        private static Action<string, bool>? _set;

        public static void Bind(Action<string, bool> set) => _set = set;

        public static void Set(string message, bool busy = false) =>
            _set?.Invoke(message, busy);

        public static void Report(PlaidSyncProgress progress)
        {
            ArgumentNullException.ThrowIfNull(progress);
            Set(progress.Message, busy: !progress.Complete);
        }
    }
}
