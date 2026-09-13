using System.Reflection;

namespace THMS.UI.WinForms.Controls
{
    internal static class DataGridViewUtil
    {
        public static void EnableDoubleBuffering(DataGridView grid)
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(grid, true);
        }
    }
}
