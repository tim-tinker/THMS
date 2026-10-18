using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        private static readonly ConditionalWeakTable<DataGridView, object> DateMoneySizingHooked = new();

        private static readonly HashSet<string> DateProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Date", "DueDate", "StatementDate", "AsOfDate", "PeriodStart", "PeriodEnd",
            "StartTime", "EndTime", "Timestamp", "DateAcquired", "Deadline", "DateText",
            "StartDate", "EndDate", "Period"
        };

        private static readonly HashSet<string> MoneyProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "AmountDue", "AmountText", "StatementBalance", "EscrowBalance", "InterestPaid",
            "CreditLimit", "BankCreditAvailable", "Cost", "FuelCost", "CostPerMile", "SessionCost",
            "InitialAmount", "CurrentBalance", "RequiredAmount", "SelectedAmount", "Recommended",
            "Remaining", "Starting", "BudgetAmount", "Actual", "ForecastBalance", "Balance", "Available"
        };

        public static void EnableDoubleBuffering(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            typeof(DataGridView)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(grid, true);
            ApplyHeaderStyle(grid);
            ApplySelectionStyle(grid);
            HookDisplayedDateAndMoneyColumns(grid);
            HookStableDisplayedRow(grid);
        }

        public static void FitDisplayedDateAndMoneyColumns(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            foreach (DataGridViewColumn column in grid.Columns)
                FitDisplayedDateOrMoneyColumn(column);
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

        private static readonly ConditionalWeakTable<DataGridView, DisplayedRowAnchor> DisplayedRowAnchors = new();

        private static void HookStableDisplayedRow(DataGridView grid)
        {
            if (DisplayedRowAnchors.TryGetValue(grid, out _))
                return;

            DisplayedRowAnchors.Add(grid, new DisplayedRowAnchor(grid));
        }

        private static void HookDisplayedDateAndMoneyColumns(DataGridView grid)
        {
            if (!DateMoneySizingHooked.TryAdd(grid, grid))
                return;

            grid.ColumnAdded += (_, e) => FitDisplayedDateOrMoneyColumn(e.Column);
            grid.DataBindingComplete += (_, _) => FitDisplayedDateAndMoneyColumns(grid);
            FitDisplayedDateAndMoneyColumns(grid);
        }

        private static void FitDisplayedDateOrMoneyColumn(DataGridViewColumn column)
        {
            if (!DisplaysDateOrMoney(column))
                return;
            if (column.AutoSizeMode == DataGridViewAutoSizeColumnMode.DisplayedCells)
                return;

            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        private static bool DisplaysDateOrMoney(DataGridViewColumn column)
        {
            if (column is CalendarColumn)
                return true;
            if (column is not DataGridViewTextBoxColumn)
                return false;

            var format = column.DefaultCellStyle.Format;
            if (string.IsNullOrEmpty(format))
                format = column.CellTemplate?.Style.Format;
            if (IsCurrencyFormat(format) || IsDateFormat(format))
                return true;

            var valueType = column.ValueType is null
                ? null
                : Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType;
            if (valueType == typeof(DateTime) || valueType == typeof(DateTimeOffset) || valueType == typeof(DateOnly))
                return true;

            var property = string.IsNullOrEmpty(column.DataPropertyName) ? column.Name : column.DataPropertyName;
            if (DateProperties.Contains(property) || MoneyProperties.Contains(property))
                return true;

            return property.Equals("Amount", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(format);
        }

        private static bool IsCurrencyFormat(string? format)
        {
            if (string.IsNullOrEmpty(format) || format[0] is not ('c' or 'C'))
                return false;

            return format.Length == 1 || int.TryParse(format.AsSpan(1), out _);
        }

        private static bool IsDateFormat(string? format)
        {
            if (format is not { Length: 1 })
                return false;

            return format[0] is 'd' or 'D' or 'g' or 'G' or 'f' or 'F'
                or 'm' or 'M' or 'o' or 'O' or 'r' or 'R' or 's'
                or 't' or 'T' or 'u' or 'U' or 'y' or 'Y';
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
            if (e.Graphics is null || e.CellBounds.Width <= 1 || e.CellBounds.Height <= 1)
                return;

            try
            {
                e.Paint(e.ClipBounds, e.PaintParts);
                var y = e.CellBounds.Bottom - 2;
                using var pen = new Pen(HeaderRule, 2f);
                e.Graphics.DrawLine(pen, e.CellBounds.Left, y, e.CellBounds.Right, y);
                e.Handled = true;
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        /// Keeps the first visible row in place when a bind or current-cell change would
        /// otherwise scroll that row to the bottom of the viewport.
        /// </summary>
        private sealed class DisplayedRowAnchor
        {
            private readonly DataGridView _grid;
            private int _anchor = -1;
            private int _epoch;
            private int _suspend;
            private bool _pending;
            private bool _scheduling;
            private int _pendingAnchor;
            private int _pendingEpoch;

            public DisplayedRowAnchor(DataGridView grid)
            {
                _grid = grid;
                grid.MouseDown += NoteStablePosition;
                grid.KeyDown += NoteStablePosition;
                grid.Scroll += OnScroll;
                grid.CurrentCellChanged += OnCurrentCellChanged;
                grid.DataBindingComplete += OnDataBindingComplete;
            }

            private void NoteStablePosition(object? sender, EventArgs e)
            {
                if (_suspend > 0 || _grid.IsDisposed)
                    return;

                _epoch++;
                CommitAnchor();
            }

            private void OnScroll(object? sender, ScrollEventArgs e)
            {
                if (e.ScrollOrientation != ScrollOrientation.VerticalScroll || _suspend > 0)
                    return;
                if (_grid.IsDisposed || !_grid.IsHandleCreated)
                    return;

                _epoch++;
                var epoch = _epoch;
                Post(() =>
                {
                    if (epoch != _epoch || _suspend > 0)
                        return;
                    CommitAnchor();
                });
            }

            private void OnCurrentCellChanged(object? sender, EventArgs e)
            {
                if (_suspend > 0 || _anchor < 0 || SelectionMovedOffScreen(_anchor))
                    return;

                ScheduleRestore();
            }

            private void OnDataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
            {
                if (_suspend > 0)
                    return;
                if (e.ListChangedType is not (
                    ListChangedType.Reset
                    or ListChangedType.ItemChanged
                    or ListChangedType.ItemAdded
                    or ListChangedType.ItemDeleted
                    or ListChangedType.ItemMoved))
                    return;

                if (_anchor < 0)
                {
                    CommitAnchor();
                    return;
                }

                // The bound position can still change after this event. Queue the restore
                // and decide once the current row has settled.
                ScheduleRestore(followLaterSelection: true);
            }

            private void ScheduleRestore(bool followLaterSelection = false)
            {
                if (_scheduling || _grid.IsDisposed || _anchor < 0)
                    return;
                if (!followLaterSelection && SelectionMovedOffScreen(_anchor))
                    return;

                _scheduling = true;
                try
                {
                    _pendingAnchor = _anchor;
                    _suspend++;
                    if (!SelectionMovedOffScreen(_anchor))
                        Apply(_pendingAnchor);
                    _epoch++;
                    _pendingEpoch = _epoch;

                    if (_pending || !_grid.IsHandleCreated)
                    {
                        _suspend = Math.Max(0, _suspend - 1);
                        return;
                    }

                    _pending = true;
                    if (!Post(RestorePass))
                        FinishPending();
                }
                finally
                {
                    _scheduling = false;
                }
            }

            private void RestorePass()
            {
                var posted = false;
                try
                {
                    RestoreOrFollowSelection();
                    posted = !_grid.IsDisposed && _grid.IsHandleCreated;
                }
                finally
                {
                    if (!posted)
                        FinishPending();
                }

                if (!posted)
                    return;

                if (!Post(RestoreLast))
                    FinishPending();
            }

            private void RestoreLast()
            {
                try
                {
                    RestoreOrFollowSelection();
                }
                finally
                {
                    FinishPending();
                }
            }

            private void RestoreOrFollowSelection()
            {
                if (_grid.IsDisposed || _pendingEpoch != _epoch)
                    return;
                if (SelectionMovedOffScreen(_pendingAnchor))
                {
                    RememberDisplayedRow();
                    return;
                }

                Apply(_pendingAnchor);
            }

            private void FinishPending()
            {
                _pending = false;
                _suspend = Math.Max(0, _suspend - 1);
            }

            private void CommitAnchor()
            {
                if (_suspend > 0)
                    return;

                RememberDisplayedRow();
            }

            private void RememberDisplayedRow()
            {
                var row = FirstDisplayedRow();
                if (row >= 0)
                    _anchor = row;
            }

            private bool SelectionMovedOffScreen(int anchor)
            {
                if (anchor < 0 || _grid.CurrentCell is not { RowIndex: >= 0 } cell)
                    return false;

                var visible = VisibleRowCount();
                if (visible <= 0)
                    return false;

                return cell.RowIndex < anchor || cell.RowIndex >= anchor + visible;
            }

            private void Apply(int firstDisplayed)
            {
                var visible = VisibleRowCount();
                if (visible <= 0 || firstDisplayed < 0)
                    return;

                var maxFirst = Math.Max(0, _grid.RowCount - visible);
                var index = Math.Clamp(firstDisplayed, 0, maxFirst);
                if (FirstDisplayedRow() == index)
                    return;

                try
                {
                    _grid.FirstDisplayedScrollingRowIndex = index;
                }
                catch (InvalidOperationException)
                {
                }
            }

            private int FirstDisplayedRow()
            {
                if (_grid.IsDisposed || _grid.RowCount == 0)
                    return -1;

                try
                {
                    return _grid.FirstDisplayedScrollingRowIndex;
                }
                catch (InvalidOperationException)
                {
                    return -1;
                }
            }

            private int VisibleRowCount()
            {
                if (_grid.IsDisposed || !_grid.IsHandleCreated || _grid.RowCount == 0)
                    return 0;

                try
                {
                    return _grid.DisplayedRowCount(includePartialRow: false);
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }

            private bool Post(Action action)
            {
                if (_grid.IsDisposed || !_grid.IsHandleCreated)
                    return false;

                try
                {
                    _grid.BeginInvoke(action);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }
    }
}
