using THMS.Logic.Diagnostics;

namespace THMS.UI.WinForms.Controls
{
    internal static class ExceptionDialog
    {
        public static void Show(
            IWin32Window? owner,
            string title,
            Exception ex,
            string? summary = null,
            MessageBoxIcon icon = MessageBoxIcon.Warning)
        {
            MessageBox.Show(owner, Format(ex, summary), title, MessageBoxButtons.OK, icon);
        }

        public static string Format(Exception ex, string? summary = null)
        {
            var message = ex.Message;
            var body = string.IsNullOrWhiteSpace(summary) ? message : summary + "\n" + message;
            try
            {
                var notes = new EntityKeyDescriber().DescribeIdsIn(Messages(ex));
                if (notes.Count == 0)
                    return body;
                return body + "\n\n" + string.Join("\n", notes);
            }
            catch (Exception lookup)
            {
                return body + "\n\nCould not identify the id in this error.\n" + lookup.Message;
            }
        }

        private static string Messages(Exception ex)
        {
            var parts = new List<string>();
            for (var current = ex; current is not null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                    parts.Add(current.Message);
            }

            return string.Join("\n", parts);
        }
    }
}
