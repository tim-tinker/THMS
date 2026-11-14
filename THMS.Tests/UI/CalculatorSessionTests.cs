using System.Globalization;
using THMS.UI.WinForms.Controls;

namespace THMS.Tests.UI
{
    [TestFixture]
    public class CalculatorSessionTests
    {
        private CultureInfo _culture;
        private CultureInfo _uiCulture;

        [SetUp]
        public void UseUnitedStates()
        {
            _culture = CultureInfo.CurrentCulture;
            _uiCulture = CultureInfo.CurrentUICulture;
            var unitedStates = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentCulture = unitedStates;
            CultureInfo.CurrentUICulture = unitedStates;
        }

        [TearDown]
        public void RestoreCulture()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }

        [Test]
        public void DigitsAndDecimal_BuildTheEntry()
        {
            var calc = Enter("1005.20");

            Assert.That(calc.Display, Is.EqualTo("1005.20"));
            Assert.That(calc.Expression, Is.EqualTo(""));
        }

        [Test]
        public void SecondDecimalAndLeadingZeros_AreIgnored()
        {
            var calc = Enter("00.1.2");

            Assert.That(calc.Display, Is.EqualTo("0.12"));
        }

        [Test]
        public void Arithmetic_EvaluatesWhenTheNextOperatorOrEqualsIsPressed()
        {
            Assert.That(Enter("12+3=").Display, Is.EqualTo("15"));
            Assert.That(Enter("12+3=").Expression, Is.EqualTo(""));
            Assert.That(Enter("12+3+").Display, Is.EqualTo("15"));
            Assert.That(Enter("12+3+").Expression, Is.EqualTo("15 +"));
            Assert.That(Enter("8−3=").Display, Is.EqualTo("5"));
            Assert.That(Enter("8*7=").Display, Is.EqualTo("56"));
            Assert.That(Enter("1/8=").Display, Is.EqualTo("0.125"));
            Assert.That(Enter("500+500=").Display, Is.EqualTo("1,000"));
        }

        [Test]
        public void Equals_RepeatsTheLastOperation()
        {
            Assert.That(Enter("2+3==").Display, Is.EqualTo("8"));
            Assert.That(Enter("2+=").Display, Is.EqualTo("4"));
            Assert.That(Enter("5==").Display, Is.EqualTo("5"));
        }

        [Test]
        public void Operator_ReplacesAPendingOperatorBeforeANewNumber()
        {
            Assert.That(Enter("2+−3=").Display, Is.EqualTo("-1"));
        }

        [Test]
        public void Percent_UsesTheStoredValueForAdditionAndSubtraction()
        {
            Assert.That(Enter("200+10%=").Display, Is.EqualTo("220"));
            Assert.That(Enter("200−10%=").Display, Is.EqualTo("180"));
            Assert.That(Enter("200*10%=").Display, Is.EqualTo("20"));
            Assert.That(Enter("200/10%=").Display, Is.EqualTo("2,000"));
            Assert.That(Enter("50%").Display, Is.EqualTo("0.5"));
        }

        [Test]
        public void SignBackspaceAndClear_EditTheDisplayedNumber()
        {
            Assert.That(Enter("5±").Display, Is.EqualTo("-5"));
            Assert.That(Enter("5±±").Display, Is.EqualTo("5"));
            Assert.That(Enter("123b").Display, Is.EqualTo("12"));
            Assert.That(Enter("12=b").Display, Is.EqualTo("1"));
            Assert.That(Enter("12+3=c").Display, Is.EqualTo("0"));
            Assert.That(Enter("12+3=c").Expression, Is.EqualTo(""));
        }

        [Test]
        public void DivideByZero_ShowsAnErrorUntilTheNextEntry()
        {
            var calc = Enter("1/0=");

            Assert.That(calc.IsError, Is.True);
            Assert.That(calc.Display, Is.EqualTo("Divide by zero"));

            calc.Press(CalculatorKey.Digit7);
            Assert.That(calc.IsError, Is.False);
            Assert.That(calc.Display, Is.EqualTo("7"));
        }

        [Test]
        public void Entry_StopsAfterFourteenDigits()
        {
            var calc = Enter("123456789012345");

            Assert.That(calc.Display, Is.EqualTo("12345678901234"));
        }

        [Test]
        public void Multiply_ReportsOverflow()
        {
            var calc = Enter("1");
            foreach (var ch in string.Concat(Enumerable.Repeat("*10", 40)))
            {
                calc.Press(ToKey(ch));
                if (calc.IsError)
                    break;
            }

            Assert.That(calc.Display, Is.EqualTo("Overflow"));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Calculator_StaysAtTheBottomOfTheNavigationColumn()
        {
            using var form = new Form { Width = 320, Height = 900 };
            var column = new Panel { Dock = DockStyle.Left, Width = 291 };
            var list = new Panel { Dock = DockStyle.Fill };
            var calculator = new NavCalculator { Dock = DockStyle.Bottom };
            column.Controls.Add(list);
            column.Controls.Add(calculator);
            form.Controls.Add(column);
            form.Show();

            Assert.That(calculator.Bottom, Is.EqualTo(column.ClientSize.Height));
            Assert.That(list.Top, Is.EqualTo(0));
            Assert.That(list.Bottom, Is.EqualTo(calculator.Top));
            Assert.That(list.Height, Is.GreaterThan(0));
            Assert.That(list.Width, Is.EqualTo(column.ClientSize.Width));

            var keys = calculator.Controls.OfType<TableLayoutPanel>().Single();
            foreach (Button button in keys.Controls)
            {
                var textHeight = TextRenderer.MeasureText(button.Text, button.Font).Height;
                Assert.That(button.ClientSize.Height, Is.GreaterThanOrEqualTo(textHeight + 12), button.Text);
            }
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void Result_ShowsTheFullNumber()
        {
            using var form = new Form { Width = 320, Height = 900 };
            var calculator = new NavCalculator { Dock = DockStyle.Left, Width = 291 };
            form.Controls.Add(calculator);
            form.Show();

            Press(calculator, "1234567890");
            var value = ResultLabel(calculator);
            Assert.That(value.Text, Is.EqualTo("1234567890"));
            AssertResultFits(value);

            Press(calculator, "1234");
            Assert.That(value.Text, Is.EqualTo("12345678901234"));
            AssertResultFits(value);

            Press(calculator, "+0=");
            Assert.That(value.Text, Is.EqualTo("12,345,678,901,234"));
            AssertResultFits(value);

            Press(calculator, "C1/0=");
            Assert.That(value.Text, Is.EqualTo("Divide by zero"));
            AssertResultFits(value);
        }

        private static void Press(Control calculator, string keys)
        {
            var buttons = calculator.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<Button>().ToList();
            foreach (var ch in keys)
            {
                var caption = ch switch
                {
                    '+' => "+",
                    '/' => "÷",
                    '=' => "=",
                    'C' => "C",
                    _ => ch.ToString()
                };
                buttons.Single(button => button.Text == caption).PerformClick();
            }
        }

        private static Label ResultLabel(Control calculator) =>
            calculator.Controls.OfType<Panel>()
                .SelectMany(panel => panel.Controls.OfType<Label>())
                .Single(label => label.Font.Bold && label.TextAlign == ContentAlignment.MiddleRight);

        private static void AssertResultFits(Label value)
        {
            var flags = TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.GlyphOverhangPadding
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.Right;
            using var graphics = value.CreateGraphics();
            var measured = TextRenderer.MeasureText(
                graphics,
                value.Text,
                value.Font,
                new Size(int.MaxValue, int.MaxValue),
                flags);
            Assert.That(measured.Width, Is.LessThanOrEqualTo(value.ClientSize.Width - value.Padding.Horizontal), value.Text);
            Assert.That(measured.Height, Is.LessThanOrEqualTo(value.ClientSize.Height - value.Padding.Vertical), value.Text);
        }

        private static CalculatorSession Enter(string keys) => Continue(new CalculatorSession(), keys);

        private static CalculatorSession Continue(CalculatorSession session, string keys)
        {
            foreach (var ch in keys)
                session.Press(ToKey(ch));
            return session;
        }

        private static CalculatorKey ToKey(char ch) => ch switch
        {
            '0' => CalculatorKey.Digit0,
            '1' => CalculatorKey.Digit1,
            '2' => CalculatorKey.Digit2,
            '3' => CalculatorKey.Digit3,
            '4' => CalculatorKey.Digit4,
            '5' => CalculatorKey.Digit5,
            '6' => CalculatorKey.Digit6,
            '7' => CalculatorKey.Digit7,
            '8' => CalculatorKey.Digit8,
            '9' => CalculatorKey.Digit9,
            '.' => CalculatorKey.Decimal,
            '+' => CalculatorKey.Add,
            '-' or '−' => CalculatorKey.Subtract,
            '*' or '×' => CalculatorKey.Multiply,
            '/' or '÷' => CalculatorKey.Divide,
            '=' => CalculatorKey.Equals,
            '%' => CalculatorKey.Percent,
            '±' => CalculatorKey.ToggleSign,
            'c' => CalculatorKey.Clear,
            'b' => CalculatorKey.Backspace,
            _ => throw new ArgumentException($"No calculator key for '{ch}'.", nameof(ch))
        };
    }
}
