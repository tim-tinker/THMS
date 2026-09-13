using System.ComponentModel;

namespace THMS.UI.WinForms.Controls
{
    public class ThmsTabControl : TabControl
    {
        public static readonly Font HeaderFont = new("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Color IdleBack = Color.FromArgb(92, 45, 145);
        public static readonly Color IdleFore = Color.White;
        public static readonly Color SelectedBack = Color.FromArgb(255, 213, 0);
        public static readonly Color SelectedFore = Color.FromArgb(48, 20, 80);
        public static readonly Color PageTint = Color.FromArgb(255, 252, 235);

        private int _minTabWidth = 90;
        private int _maxTabWidth = 140;
        private int _tabHeight = 28;

        public ThmsTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            Padding = new Point(10, 3);
            ItemSize = new Size(_minTabWidth, _tabHeight);
        }

        [DefaultValue(90)]
        [Category("Appearance")]
        public int MinTabWidth
        {
            get => _minTabWidth;
            set
            {
                _minTabWidth = Math.Max(40, value);
                RecalculateItemSize();
            }
        }

        [DefaultValue(140)]
        [Category("Appearance")]
        public int MaxTabWidth
        {
            get => _maxTabWidth;
            set
            {
                _maxTabWidth = Math.Max(_minTabWidth, value);
                RecalculateItemSize();
            }
        }

        [DefaultValue(28)]
        [Category("Appearance")]
        public int TabHeight
        {
            get => _tabHeight;
            set
            {
                _tabHeight = Math.Max(24, value);
                RecalculateItemSize();
            }
        }

        public void RecalculateItemSize()
        {
            var width = _minTabWidth;
            foreach (TabPage page in TabPages)
            {
                var measured = TextRenderer.MeasureText(page.Text, HeaderFont).Width + 24;
                width = Math.Max(width, measured);
            }

            width = Math.Min(width, _maxTabWidth);
            ItemSize = new Size(LogicalToDeviceUnits(width), LogicalToDeviceUnits(_tabHeight));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RecalculateItemSize();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control is TabPage page)
            {
                page.BackColor = PageTint;
                page.UseVisualStyleBackColor = false;
            }
            RecalculateItemSize();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            RecalculateItemSize();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            RecalculateItemSize();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= TabCount)
                return;

            var selected = e.Index == SelectedIndex
                || (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var bounds = e.Bounds;
            if (selected)
                bounds.Inflate(0, 2);

            using var back = new SolidBrush(selected ? SelectedBack : IdleBack);
            e.Graphics.FillRectangle(back, bounds);

            TextRenderer.DrawText(
                e.Graphics,
                TabPages[e.Index].Text,
                HeaderFont,
                bounds,
                selected ? SelectedFore : IdleFore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            base.OnDrawItem(e);
        }
    }
}
