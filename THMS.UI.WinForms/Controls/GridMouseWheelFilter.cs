using System.Runtime.InteropServices;

namespace THMS.UI.WinForms.Controls
{
    // A modal dialog keeps the wheel and disables its owner, so grids still visible
    // behind it never receive the message. Deliver the wheel to the grid under the pointer.
    internal sealed class GridMouseWheelFilter : IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A;
        private const uint GW_HWNDNEXT = 2;
        private const int MaxTopLevelWindows = 256;
        private const int MaxChildDepth = 32;

        private bool _forwarding;

        public bool PreFilterMessage(ref Message m)
        {
            if (_forwarding || m.Msg != WM_MOUSEWHEEL)
                return false;

            var grid = GridUnderCursor();
            if (grid is null)
                return false;

            if (m.HWnd == grid.Handle || IsChild(grid.Handle, m.HWnd))
                return false;

            _forwarding = true;
            try
            {
                SendMessage(grid.Handle, WM_MOUSEWHEEL, m.WParam, m.LParam);
            }
            finally
            {
                _forwarding = false;
            }

            return true;
        }

        private static DataGridView? GridUnderCursor()
        {
            var top = TopLevelControlAtCursor();
            if (top is null)
                return null;

            var hit = DeepestChild(top, Cursor.Position);
            for (var current = hit; current is not null; current = current.Parent)
            {
                if (current is DataGridView { Enabled: true, IsDisposed: false } grid && grid.IsHandleCreated)
                    return grid;
            }

            return null;
        }

        private static Control? TopLevelControlAtCursor()
        {
            if (!GetCursorPos(out var cursor))
                return null;

            var hwnd = GetTopWindow(IntPtr.Zero);
            for (var i = 0; hwnd != IntPtr.Zero && i < MaxTopLevelWindows; i++)
            {
                if (IsWindowVisible(hwnd)
                    && GetWindowRect(hwnd, out var rect)
                    && cursor.X >= rect.Left
                    && cursor.X < rect.Right
                    && cursor.Y >= rect.Top
                    && cursor.Y < rect.Bottom)
                {
                    return Control.FromHandle(hwnd);
                }

                hwnd = GetWindow(hwnd, GW_HWNDNEXT);
            }

            return null;
        }

        private static Control DeepestChild(Control parent, Point screen)
        {
            var current = parent;
            for (var depth = 0; depth < MaxChildDepth; depth++)
            {
                if (!current.IsHandleCreated || current.IsDisposed)
                    break;

                var child = current.GetChildAtPoint(
                    current.PointToClient(screen),
                    GetChildAtPointSkip.Invisible);
                if (child is null)
                    break;

                current = child;
            }

            return current;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
