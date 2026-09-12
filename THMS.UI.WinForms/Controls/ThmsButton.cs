using System.ComponentModel;

namespace THMS.UI.WinForms.Controls
{
    public class ThmsButton : Button
    {
        public static readonly Color ActionBack = Color.FromArgb(92, 45, 145);
        public static readonly Color ActionFore = Color.White;
        public static readonly Color DestructiveBack = Color.FromArgb(153, 51, 85);
        public static readonly Color DisabledBack = Color.FromArgb(210, 210, 210);
        public static readonly Color DisabledFore = Color.FromArgb(110, 110, 110);

        private bool _destructive;

        public ThmsButton()
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(0, 36);
            Padding = new Padding(12, 6, 12, 6);
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            Font = new Font(Font, FontStyle.Bold);
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
        }
    }
}
