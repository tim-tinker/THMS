using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace THMS.UI.WinForms.Controls
{
    public class ThmsButton : Button
    {
        public static readonly Color ActionBack = Color.FromArgb(39, 137, 71);
        public static readonly Color ActionFore = Color.White;
        public static readonly Color DestructiveBack = Color.FromArgb(192, 41, 41);
        public static readonly Color DisabledBack = Color.FromArgb(210, 210, 210);
        public static readonly Color DisabledFore = Color.FromArgb(110, 110, 110);

        private const int CornerRadiusLogical = 8;
        private bool _destructive;
        private bool _hovering;

        public ThmsButton()
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(0, 36);
            Padding = new Padding(12, 6, 12, 6);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Font = new Font(Font, FontStyle.Bold);
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
            ApplyColors();
            EnabledChanged += (_, _) => ApplyColors();
        }

        [DefaultValue(false)]
        [Category("Appearance")]
        public bool Destructive
        {
            get => _destructive;
            set
            {
                if (_destructive == value)
                    return;
                _destructive = value;
                ApplyColors();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            var parentColor = Parent?.BackColor ?? SystemColors.Control;
            using var brush = new SolidBrush(parentColor);
            pevent.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var parentColor = Parent?.BackColor ?? SystemColors.Control;
            using (var background = new SolidBrush(parentColor))
                g.FillRectangle(background, ClientRectangle);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var fill = FillColor();
            var border = ControlPaint.Dark(fill);
            var radius = Math.Max(1, LogicalToDeviceUnits(CornerRadiusLogical));
            var bounds = new RectangleF(0.5f, 0.5f, Math.Max(0, Width - 1.5f), Math.Max(0, Height - 1.5f));

            using (var path = RoundedRect(bounds, radius))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            var textBounds = Rectangle.FromLTRB(
                Padding.Left,
                Padding.Top,
                Math.Max(Padding.Left, Width - Padding.Right),
                Math.Max(Padding.Top, Height - Padding.Bottom));
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textBounds,
                ForeColor,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPadding
                | TextFormatFlags.PreserveGraphicsClipping);

            if (Focused && ShowFocusCues)
            {
                var focus = Rectangle.Inflate(Rectangle.Round(bounds), -3, -3);
                if (focus.Width > 0 && focus.Height > 0)
                {
                    using var path = RoundedRect(focus, Math.Max(1, radius - 3));
                    using var pen = new Pen(ForeColor) { DashStyle = DashStyle.Dot };
                    g.DrawPath(pen, path);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            Invalidate();
            base.OnMouseUp(e);
        }

        private Color FillColor()
        {
            var fill = BackColor;
            if (!Enabled)
                return fill;
            if (Capture && ClientRectangle.Contains(PointToClient(MousePosition)))
                return ControlPaint.Dark(fill);
            if (_hovering)
                return ControlPaint.Light(fill);
            return fill;
        }

        private void ApplyColors()
        {
            var back = _destructive ? DestructiveBack : ActionBack;
            if (Enabled)
            {
                BackColor = back;
                ForeColor = ActionFore;
                FlatAppearance.BorderColor = ControlPaint.Dark(back);
            }
            else
            {
                BackColor = DisabledBack;
                ForeColor = DisabledFore;
                FlatAppearance.BorderColor = DisabledBack;
            }

            Invalidate();
        }

        private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            var diameter = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0.5f || bounds.Width <= 0 || bounds.Height <= 0)
            {
                if (bounds.Width > 0 && bounds.Height > 0)
                    path.AddRectangle(bounds);
                return path;
            }

            var arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
