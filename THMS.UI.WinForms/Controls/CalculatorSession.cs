using System.Globalization;

namespace THMS.UI.WinForms.Controls
{
    internal enum CalculatorKey
    {
        Digit0,
        Digit1,
        Digit2,
        Digit3,
        Digit4,
        Digit5,
        Digit6,
        Digit7,
        Digit8,
        Digit9,
        Decimal,
        Add,
        Subtract,
        Multiply,
        Divide,
        Equals,
        Clear,
        Backspace,
        Percent,
        ToggleSign
    }

    /// <summary>
    /// Immediate-execution arithmetic, matching a standard desktop calculator:
    /// operators evaluate the pending operation, equals can be repeated, and
    /// percent is taken against the stored value for addition and subtraction.
    /// </summary>
    internal sealed class CalculatorSession
    {
        private const int MaxDigits = 14;
        private const int DisplayDecimals = 8;

        private decimal _current;
        private decimal _stored;
        private decimal _repeatValue;
        private CalculatorKey? _operator;
        private CalculatorKey? _repeatOperator;
        private string? _buffer;
        private string? _error;

        public bool IsError => _error is not null;

        public string Display => _error ?? (_buffer is { Length: > 0 } text ? text : Format(_current));

        public string Expression
        {
            get
            {
                if (_error is not null || _operator is null)
                    return "";

                var text = $"{Format(_stored)} {Symbol(_operator.Value)}";
                if (_buffer is not null)
                    text += " " + (_buffer.Length == 0 ? "0" : _buffer);
                return text;
            }
        }

        public void Press(CalculatorKey key)
        {
            if (_error is not null)
                Reset();

            switch (key)
            {
                case >= CalculatorKey.Digit0 and <= CalculatorKey.Digit9:
                    AppendDigit((char)('0' + ((int)key - (int)CalculatorKey.Digit0)));
                    break;
                case CalculatorKey.Decimal:
                    AppendDecimal();
                    break;
                case CalculatorKey.Add or CalculatorKey.Subtract or CalculatorKey.Multiply or CalculatorKey.Divide:
                    ChooseOperator(key);
                    break;
                case CalculatorKey.Equals:
                    Evaluate(repeat: true);
                    break;
                case CalculatorKey.Clear:
                    Reset();
                    break;
                case CalculatorKey.Backspace:
                    Backspace();
                    break;
                case CalculatorKey.Percent:
                    Percent();
                    break;
                case CalculatorKey.ToggleSign:
                    ToggleSign();
                    break;
            }
        }

        private void AppendDigit(char digit)
        {
            BeginEntry();
            if (_buffer is "0")
                _buffer = "";
            else if (_buffer is "-0")
                _buffer = "-";

            if (DigitCount(_buffer!) >= MaxDigits)
                return;

            _buffer += digit;
        }

        private void AppendDecimal()
        {
            BeginEntry();
            var separator = DecimalSeparator();
            if (_buffer!.Contains(separator, StringComparison.Ordinal))
                return;
            if (_buffer is "" or "-")
                _buffer += "0";
            _buffer += separator;
        }

        private void BeginEntry()
        {
            if (_buffer is not null)
                return;

            _repeatOperator = null;
            _buffer = "";
        }

        private void ChooseOperator(CalculatorKey op)
        {
            if (_buffer is not null && _operator is not null)
            {
                if (!TryCompute(_stored, _operator.Value, Parse(_buffer), out var result))
                    return;
                _current = result;
                _stored = result;
            }
            else
            {
                _current = EntryValue();
                _stored = _current;
            }

            _buffer = null;
            _operator = op;
            _repeatOperator = null;
        }

        private void Evaluate(bool repeat)
        {
            if (_operator is CalculatorKey pending)
            {
                var operand = EntryValue();
                if (!TryCompute(_stored, pending, operand, out var result))
                    return;

                _repeatOperator = pending;
                _repeatValue = operand;
                _operator = null;
                _current = result;
                _stored = result;
                _buffer = null;
                return;
            }

            if (repeat && _repeatOperator is CalculatorKey again)
            {
                if (!TryCompute(_current, again, _repeatValue, out var result))
                    return;
                _current = result;
                _stored = result;
                _buffer = null;
                return;
            }

            _current = EntryValue();
            _buffer = null;
        }

        private void Percent()
        {
            var value = EntryValue();
            try
            {
                value = _operator is CalculatorKey.Add or CalculatorKey.Subtract
                    ? _stored * value / 100m
                    : value / 100m;
            }
            catch (OverflowException)
            {
                Fail("Overflow");
                return;
            }

            _current = value;
            _buffer = null;
        }

        private void ToggleSign()
        {
            if (_buffer is not null)
            {
                _buffer = _buffer.StartsWith('-') ? _buffer[1..] : "-" + _buffer;
                if (_buffer is "-" or "")
                    _buffer = "0";
                return;
            }

            _current = -_current;
        }

        private void Backspace()
        {
            if (_buffer is null)
                _buffer = ToRaw(_current);
            if (_buffer.Length > 0)
                _buffer = _buffer[..^1];
            if (_buffer is "" or "-")
            {
                _buffer = null;
                _current = 0;
            }
        }

        private void Reset()
        {
            _current = 0;
            _stored = 0;
            _repeatValue = 0;
            _operator = null;
            _repeatOperator = null;
            _buffer = null;
            _error = null;
        }

        private decimal EntryValue() => _buffer is null ? _current : Parse(_buffer);

        private bool TryCompute(decimal left, CalculatorKey op, decimal right, out decimal result)
        {
            try
            {
                if (op == CalculatorKey.Divide && right == 0)
                    throw new DivideByZeroException();

                result = op switch
                {
                    CalculatorKey.Add => left + right,
                    CalculatorKey.Subtract => left - right,
                    CalculatorKey.Multiply => left * right,
                    CalculatorKey.Divide => left / right,
                    _ => right
                };
                return true;
            }
            catch (DivideByZeroException)
            {
                Fail("Divide by zero");
                result = 0;
                return false;
            }
            catch (OverflowException)
            {
                Fail("Overflow");
                result = 0;
                return false;
            }
        }

        private void Fail(string message)
        {
            _error = message;
            _operator = null;
            _repeatOperator = null;
            _buffer = null;
        }

        private static decimal Parse(string text)
        {
            var value = text;
            var separator = DecimalSeparator();
            if (value.EndsWith(separator, StringComparison.Ordinal))
                value = value[..^separator.Length];
            if (value is "" or "-")
                return 0;

            return decimal.TryParse(
                value,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.CurrentCulture,
                out var parsed)
                ? parsed
                : 0;
        }

        private static int DigitCount(string text)
        {
            var count = 0;
            foreach (var ch in text)
            {
                if (char.IsDigit(ch))
                    count++;
            }

            return count;
        }

        private static string DecimalSeparator() =>
            CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        private static decimal Rounded(decimal value) =>
            Math.Round(value, DisplayDecimals, MidpointRounding.AwayFromZero);

        private static string Format(decimal value) =>
            Rounded(value).ToString("#,##0.########", CultureInfo.CurrentCulture);

        private static string ToRaw(decimal value) =>
            Rounded(value).ToString("0.############################", CultureInfo.CurrentCulture);

        private static string Symbol(CalculatorKey op) => op switch
        {
            CalculatorKey.Add => "+",
            CalculatorKey.Subtract => "−",
            CalculatorKey.Multiply => "×",
            CalculatorKey.Divide => "÷",
            _ => ""
        };
    }
}
