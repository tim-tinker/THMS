using System.Globalization;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class NavCalculator : UserControl
    {
        private static readonly Color DigitBack = Color.White;
        private static readonly Color CommandBack = Color.FromArgb(232, 232, 232);
        private static readonly Color OperatorBack = Color.FromArgb(220, 230, 240);
        private static readonly Color EqualsBack = Color.FromArgb(0, 99, 177);
        private static readonly Color Ink = Color.FromArgb(32, 32, 32);
        private static readonly Color Muted = Color.FromArgb(80, 80, 80);

        private readonly CalculatorSession _session = new();
        private readonly Label _expression;
        private readonly Label _value;
        private bool _applyingDisplay;

        private const int OuterPaddingTop = 10;
        private const int OuterPaddingBottom = 12;
        private const int KeypadPaddingTop = 8;
        private const int KeypadRows = 5;
        private const int KeyRowHeight = 52;
        private const int DisplayHeight = 68;
        private const int CalculatorHeight =
            OuterPaddingTop + OuterPaddingBottom + DisplayHeight + KeypadPaddingTop + KeypadRows * KeyRowHeight;

        public NavCalculator()
        {
            BackColor = Color.LightGray;
            Height = CalculatorHeight;
            MinimumSize = new Size(200, CalculatorHeight);
            Padding = new Padding(12, OuterPaddingTop, 12, OuterPaddingBottom);
            AccessibleName = "Calculator";
            // Keep the keypad's pixel size. Font autoscaling would enlarge the
            // glyphs without enlarging these rows, which clips the keys.
            AutoScaleMode = AutoScaleMode.None;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, 180, 180));
                e.Graphics.DrawLine(pen, 0, 0, Width, 0);
            };

            var display = new Panel
            {
                Dock = DockStyle.Top,
                Height = DisplayHeight,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 4, 8, 4)
            };
            var header = new Panel { Dock = DockStyle.Top, Height = 22 };
            _expression = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 8.25f),
                UseMnemonic = false
            };
            var caption = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Left,
                Text = "Calculator",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                UseMnemonic = false
            };
            header.Controls.Add(_expression);
            header.Controls.Add(caption);
            _value = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = false,
                Padding = new Padding(2, 0, 8, 0),
                ForeColor = Ink,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                UseMnemonic = false,
                Text = "0"
            };
            display.Controls.Add(_value);
            display.Controls.Add(header);
            display.Click += (_, _) => Focus();
            _value.Click += (_, _) => Focus();
            _expression.Click += (_, _) => Focus();
            caption.Click += (_, _) => Focus();
            _value.SizeChanged += (_, _) => ApplyDisplay();

            var keys = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = KeypadRows,
                Margin = new Padding(0),
                Padding = new Padding(0, KeypadPaddingTop, 0, 0)
            };
            for (var column = 0; column < 4; column++)
                keys.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            for (var row = 0; row < KeypadRows; row++)
                keys.RowStyles.Add(new RowStyle(SizeType.Absolute, KeyRowHeight));

            var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            AddKeys(keys, new[]
            {
                Key("C", CalculatorKey.Clear, CommandBack),
                Key("⌫", CalculatorKey.Backspace, CommandBack),
                Key("%", CalculatorKey.Percent, CommandBack),
                Key("÷", CalculatorKey.Divide, OperatorBack),
                Key("7", CalculatorKey.Digit7, DigitBack),
                Key("8", CalculatorKey.Digit8, DigitBack),
                Key("9", CalculatorKey.Digit9, DigitBack),
                Key("×", CalculatorKey.Multiply, OperatorBack),
                Key("4", CalculatorKey.Digit4, DigitBack),
                Key("5", CalculatorKey.Digit5, DigitBack),
                Key("6", CalculatorKey.Digit6, DigitBack),
                Key("−", CalculatorKey.Subtract, OperatorBack),
                Key("1", CalculatorKey.Digit1, DigitBack),
                Key("2", CalculatorKey.Digit2, DigitBack),
                Key("3", CalculatorKey.Digit3, DigitBack),
                Key("+", CalculatorKey.Add, OperatorBack),
                Key("±", CalculatorKey.ToggleSign, DigitBack),
                Key("0", CalculatorKey.Digit0, DigitBack),
                Key(separator, CalculatorKey.Decimal, DigitBack),
                Key("=", CalculatorKey.Equals, EqualsBack, Color.White)
            });

            Controls.Add(keys);
            Controls.Add(display);
            ApplyDisplay();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDisplay();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyDisplay();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
                ApplyDisplay();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (TryMap(keyData, out var key))
            {
                Press(key);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Press(CalculatorKey key)
        {
            _session.Press(key);
            ApplyDisplay();
        }

        private void ApplyDisplay()
        {
            if (_applyingDisplay)
                return;

            _applyingDisplay = true;
            try
            {
                var text = _session.Display;
                _expression.Text = _session.Expression;
                _value.Text = text;
                _value.ForeColor = _session.IsError ? Color.Firebrick : Ink;
                FitValueFont(text);
            }
            finally
            {
                _applyingDisplay = false;
            }
        }

        private void FitValueFont(string text)
        {
            // Label draws inside its padding and clips anything past that box.
            // Measure with the same flags, against that box, and keep shrinking
            // until the whole string fits. A 9pt floor still cut off long entries.
            var width = Math.Max(1, _value.ClientSize.Width - _value.Padding.Horizontal);
            var height = Math.Max(1, _value.ClientSize.Height - _value.Padding.Vertical);
            var flags = TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.GlyphOverhangPadding
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.Right;
            Font? chosen = null;
            for (var size = 16f; size >= 5f; size -= 0.5f)
            {
                var candidate = new Font("Segoe UI", size, FontStyle.Bold);
                var measured = Measure(text, candidate, flags);
                if ((measured.Width <= width && measured.Height <= height) || size <= 5f)
                {
                    chosen = candidate;
                    break;
                }

                candidate.Dispose();
            }

            var previous = _value.Font;
            _value.Font = chosen!;
            if (!ReferenceEquals(previous, Font))
                previous.Dispose();
        }

        private Size Measure(string text, Font font, TextFormatFlags flags)
        {
            var unlimited = new Size(int.MaxValue, int.MaxValue);
            if (!_value.IsHandleCreated)
                return TextRenderer.MeasureText(text, font, unlimited, flags);

            using var graphics = _value.CreateGraphics();
            return TextRenderer.MeasureText(graphics, text, font, unlimited, flags);
        }

        private void AddKeys(TableLayoutPanel table, IReadOnlyList<Button> buttons)
        {
            for (var i = 0; i < buttons.Count; i++)
                table.Controls.Add(buttons[i], i % 4, i / 4);
        }

        private Button Key(string text, CalculatorKey key, Color back, Color? fore = null)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Padding = new Padding(0),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore ?? Ink,
                Font = new Font("Segoe UI", 12f),
                TextAlign = ContentAlignment.MiddleCenter,
                TabStop = false,
                UseMnemonic = false,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = fore is null
                ? Color.FromArgb(200, 200, 200)
                : EqualsBack;
            button.FlatAppearance.MouseOverBackColor = back.ToArgb() == DigitBack.ToArgb()
                ? Color.FromArgb(232, 240, 248)
                : ControlPaint.Light(back, 0.12f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back, 0.08f);
            button.Click += (_, _) => Press(key);
            return button;
        }

        private static bool TryMap(Keys keyData, out CalculatorKey key)
        {
            key = default;
            if ((keyData & (Keys.Control | Keys.Alt)) != 0)
                return false;

            var code = keyData & Keys.KeyCode;
            var shift = (keyData & Keys.Shift) != 0;
            switch (code)
            {
                case Keys.D0 or Keys.NumPad0:
                    if (shift) return false;
                    key = CalculatorKey.Digit0;
                    return true;
                case Keys.D1 or Keys.NumPad1:
                    if (shift) return false;
                    key = CalculatorKey.Digit1;
                    return true;
                case Keys.D2 or Keys.NumPad2:
                    if (shift) return false;
                    key = CalculatorKey.Digit2;
                    return true;
                case Keys.D3 or Keys.NumPad3:
                    if (shift) return false;
                    key = CalculatorKey.Digit3;
                    return true;
                case Keys.D4 or Keys.NumPad4:
                    if (shift) return false;
                    key = CalculatorKey.Digit4;
                    return true;
                case Keys.D5:
                    key = shift ? CalculatorKey.Percent : CalculatorKey.Digit5;
                    return true;
                case Keys.NumPad5:
                    key = CalculatorKey.Digit5;
                    return true;
                case Keys.D6 or Keys.NumPad6:
                    if (shift) return false;
                    key = CalculatorKey.Digit6;
                    return true;
                case Keys.D7 or Keys.NumPad7:
                    if (shift) return false;
                    key = CalculatorKey.Digit7;
                    return true;
                case Keys.D8:
                    key = shift ? CalculatorKey.Multiply : CalculatorKey.Digit8;
                    return true;
                case Keys.NumPad8:
                    key = CalculatorKey.Digit8;
                    return true;
                case Keys.D9 or Keys.NumPad9:
                    if (shift) return false;
                    key = CalculatorKey.Digit9;
                    return true;
                case Keys.Decimal:
                case Keys.OemPeriod:
                case Keys.Oemcomma:
                    key = CalculatorKey.Decimal;
                    return true;
                case Keys.Add:
                    key = CalculatorKey.Add;
                    return true;
                case Keys.Oemplus:
                    key = shift ? CalculatorKey.Add : CalculatorKey.Equals;
                    return true;
                case Keys.Subtract:
                case Keys.OemMinus:
                    key = CalculatorKey.Subtract;
                    return true;
                case Keys.Multiply:
                    key = CalculatorKey.Multiply;
                    return true;
                case Keys.Divide:
                case Keys.Oem2:
                    key = CalculatorKey.Divide;
                    return true;
                case Keys.Enter:
                    key = CalculatorKey.Equals;
                    return true;
                case Keys.Back:
                    key = CalculatorKey.Backspace;
                    return true;
                case Keys.Escape:
                    key = CalculatorKey.Clear;
                    return true;
                default:
                    return false;
            }
        }
    }
}
