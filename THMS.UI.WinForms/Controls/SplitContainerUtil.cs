namespace THMS.UI.WinForms.Controls
{
    internal static class SplitContainerUtil
    {
        public const int WidthDip = 5;
        public static readonly Color Bar = Color.FromArgb(168, 168, 168);

        public static void MakeSplitterVisible(SplitContainer split)
        {
            ArgumentNullException.ThrowIfNull(split);
            ApplyBarColor(split);
            ApplyWidth(split);
            split.HandleCreated -= OnLaidOut;
            split.HandleCreated += OnLaidOut;
            split.SizeChanged -= OnLaidOut;
            split.SizeChanged += OnLaidOut;
        }

        public static bool TrySetLeadingRatio(SplitContainer split, int leadingParts, int trailingParts)
        {
            ArgumentNullException.ThrowIfNull(split);
            if (leadingParts < 1 || trailingParts < 1)
                return false;

            var span = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
            var available = span - split.SplitterWidth;
            var min = split.Panel1MinSize;
            var max = available - split.Panel2MinSize;
            if (available < 200 || max < min)
                return false;

            var leading = available * leadingParts / (leadingParts + trailingParts);
            split.SplitterDistance = Math.Clamp(leading, min, max);
            return true;
        }

        private static void OnLaidOut(object? sender, EventArgs e)
        {
            if (sender is SplitContainer split)
                ApplyWidth(split);
        }

        private static void ApplyBarColor(SplitContainer split)
        {
            split.BackColor = Bar;
            var panelBack = split.Parent?.BackColor ?? Color.White;
            if (panelBack.ToArgb() == Bar.ToArgb())
                panelBack = Color.White;
            split.Panel1.BackColor = panelBack;
            split.Panel2.BackColor = panelBack;
        }

        private static void ApplyWidth(SplitContainer split)
        {
            var width = Math.Max(4, split.LogicalToDeviceUnits(WidthDip));
            var span = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
            if (span - split.Panel1MinSize - split.Panel2MinSize < width)
                return;
            if (split.SplitterWidth == width)
                return;
            split.SplitterWidth = width;
        }
    }
}
