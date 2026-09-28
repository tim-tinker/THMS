using System.Reflection;

namespace THMS.UI.WinForms.Controls
{
    internal static class DataGridViewUtil
    {
        public static readonly Color HeaderBack = Color.FromArgb(232, 224, 240);
        public static readonly Color HeaderFore = Color.FromArgb(48, 20, 80);
        public static readonly Color HeaderRule = Color.FromArgb(92, 45, 145);
        public static readonly Color SelectionBack = Color.LightSteelBlue;
        public static readonly Color SelectionFore = Color.FromArgb(32, 32, 32);
        public static readonly Color LinkFore = Color.Blue;
        public static readonly Color LinkActiveFore = Color.FromArgb(0, 0, 180);

        public static void EnableDoubleBuffering(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            typeof(DataGridView)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(grid, true);
            ApplyHeaderStyle(grid);
            ApplySelectionStyle(grid);
        }

        public static void ApplyLinkAppearance(
            DataGridViewLinkCell cell,
            DataGridViewCellStyle style,
            bool hasUrl,
            Color plainFore)
        {
            ArgumentNullException.ThrowIfNull(cell);
            ArgumentNullException.ThrowIfNull(style);
            if (hasUrl)
            {
                cell.LinkBehavior = LinkBehavior.HoverUnderline;
                cell.LinkColor = LinkFore;
                cell.ActiveLinkColor = LinkActiveFore;
                cell.VisitedLinkColor = LinkFore;
                style.SelectionForeColor = LinkFore;
                return;
            }

            cell.LinkBehavior = LinkBehavior.NeverUnderline;
            cell.LinkColor = plainFore;
            cell.ActiveLinkColor = plainFore;
            cell.VisitedLinkColor = plainFore;
            style.SelectionForeColor = SelectionFore;
        }

        public static void SetContentForeColor(DataGridViewCellStyle style, Color color)
        {
            ArgumentNullException.ThrowIfNull(style);
            style.ForeColor = color;
            style.SelectionForeColor = color;
        }

        private static void ApplyHeaderStyle(DataGridView grid)
        {
            var first = grid.EnableHeadersVisualStyles;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = HeaderFore;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBack;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = HeaderFore;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
            if (!first)
                return;

            grid.CellPainting += OnHeaderCellPainting;
        }

        private static void ApplySelectionStyle(DataGridView grid)
        {
            ApplySelection(grid.DefaultCellStyle);
            ApplySelection(grid.RowsDefaultCellStyle);
            ApplySelection(grid.AlternatingRowsDefaultCellStyle);
            ApplySelection(grid.RowHeadersDefaultCellStyle);
        }

        private static void ApplySelection(DataGridViewCellStyle style)
        {
            style.SelectionBackColor = SelectionBack;
            style.SelectionForeColor = SelectionFore;
        }

        private static void OnHeaderCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1)
                return;

            e.Paint(e.ClipBounds, e.PaintParts);
            var y = e.CellBounds.Bottom - 2;
            using var pen = new Pen(HeaderRule, 2f);
            e.Graphics.DrawLine(pen, e.CellBounds.Left, y, e.CellBounds.Right, y);
            e.Handled = true;
        }
    }
}
