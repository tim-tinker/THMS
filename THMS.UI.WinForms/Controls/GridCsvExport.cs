using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("THMS.Tests")]

namespace THMS.UI.WinForms.Controls
{
    internal static class GridCsvExport
    {
        private const string ItemName = "thmsExportCsv";
        private const string SeparatorName = "thmsExportCsvSeparator";

        private static readonly ConditionalWeakTable<DataGridView, Hook> Hooks = new();
        private static readonly Dictionary<ContextMenuStrip, DataGridView> ContextMenus = new();
        private static readonly List<WeakReference<ContextMenuStrip>> ShownMenus = new();

        public static void Attach(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            if (!Hooks.TryAdd(grid, new Hook()))
                return;

            grid.KeyDown += OnKeyDown;
            grid.MouseDown += OnMouseDown;
            grid.MouseUp += OnMouseUp;
            grid.ContextMenuStripChanged += (_, _) => ArmContextMenu(grid);
            grid.EditingControlShowing += (_, e) =>
            {
                e.Control.KeyDown -= OnEditorKeyDown;
                e.Control.KeyDown += OnEditorKeyDown;
            };
            grid.Disposed += (_, _) => Forget(grid);
            ArmContextMenu(grid);
        }

        public static void AddCommand(ContextMenuStrip menu, DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(menu);
            ArgumentNullException.ThrowIfNull(grid);
            EnsureItem(menu, grid);
            Track(menu);
            MarkMenuShown(grid);
        }

        public static string Build(DataGridView grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            if (grid.IsCurrentCellInEditMode)
            {
                try
                {
                    grid.EndEdit();
                }
                catch (InvalidOperationException)
                {
                }
            }

            var columns = grid.Columns
                .Cast<DataGridViewColumn>()
                .Where(column => column.Visible)
                .OrderBy(column => column.DisplayIndex)
                .ThenBy(column => column.Index)
                .ToList();
            var culture = CultureInfo.CurrentCulture;
            var separator = culture.TextInfo.ListSeparator;
            var csv = new StringBuilder();
            WriteRow(csv, separator, columns.Select(Header));
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;
                WriteRow(csv, separator, columns.Select(column => CellText(row.Cells[column.Index], culture)));
            }

            return csv.ToString();
        }

        private static void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not DataGridView grid)
                return;
            ArmContextMenu(grid);
            if (!IsExport(e))
                return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            Prompt(grid);
        }

        private static void OnEditorKeyDown(object? sender, KeyEventArgs e)
        {
            if (!IsExport(e) || sender is not Control control)
                return;
            var grid = ParentGrid(control);
            if (grid is null)
                return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            Prompt(grid);
        }

        private static void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && sender is DataGridView grid)
                ArmContextMenu(grid);
        }

        private static void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || sender is not DataGridView grid || grid.IsDisposed)
                return;
            if (IsScrollBar(grid, e.Location))
                return;

            ArmContextMenu(grid);
            if (!Hooks.TryGetValue(grid, out var hook))
                return;

            var click = ++hook.Click;
            hook.Location = e.Location;
            Application.Idle += OnIdle;
            return;

            void OnIdle(object? idleSender, EventArgs idleArgs)
            {
                Application.Idle -= OnIdle;
                if (grid.IsDisposed || hook.Click != click)
                    return;
                // Menus shown from this click are open by the time the queue goes idle.
                // Header clicks and grids without a menu still get an export command.
                if (hook.MenuForClick == click || MenuIsOpen(grid))
                    return;
                ShowExportMenu(grid, hook.Location);
            }
        }

        private static void ArmContextMenu(DataGridView grid)
        {
            var menu = grid.ContextMenuStrip;
            if (menu is null || menu.IsDisposed)
                return;

            ContextMenus[menu] = grid;
            menu.Disposed -= OnContextMenuDisposed;
            menu.Disposed += OnContextMenuDisposed;
            // Subscribe last so a menu that rebuilds its items while opening still keeps Export.
            menu.Opening -= OnMenuOpening;
            menu.Opening += OnMenuOpening;
        }

        private static void OnMenuOpening(object? sender, CancelEventArgs e)
        {
            if (e.Cancel || sender is not ContextMenuStrip menu || menu.IsDisposed)
                return;
            if (!ContextMenus.TryGetValue(menu, out var grid) || grid.IsDisposed)
                return;
            EnsureItem(menu, grid);
            Track(menu);
            MarkMenuShown(grid);
        }

        private static void OnContextMenuDisposed(object? sender, EventArgs e)
        {
            if (sender is ContextMenuStrip menu)
                ContextMenus.Remove(menu);
        }

        private static void ShowExportMenu(DataGridView grid, Point location)
        {
            if (grid.IsDisposed)
                return;

            var menu = new ContextMenuStrip();
            AddCommand(menu, grid);
            menu.Closed += (_, _) =>
            {
                if (!grid.IsDisposed && grid.IsHandleCreated)
                    grid.BeginInvoke(() => menu.Dispose());
                else if (!menu.IsDisposed)
                    menu.Dispose();
            };
            menu.Show(grid, location);
        }

        private static void Prompt(DataGridView grid)
        {
            if (grid.IsDisposed)
                return;

            using var dialog = new SaveFileDialog
            {
                Title = "Export to CSV",
                Filter = "CSV spreadsheet (*.csv)|*.csv",
                DefaultExt = "csv",
                AddExtension = true,
                FileName = FileName(grid),
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(grid.FindForm()) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
                return;

            try
            {
                File.WriteAllText(dialog.FileName, Build(grid), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(grid.FindForm(), ex.Message, "Export to CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void EnsureItem(ContextMenuStrip menu, DataGridView grid)
        {
            RemoveNamed(menu, SeparatorName);
            RemoveNamed(menu, ItemName);
            if (menu.Items.Count > 0)
                menu.Items.Add(new ToolStripSeparator { Name = SeparatorName });

            var item = new ToolStripMenuItem("Export to CSV…")
            {
                Name = ItemName,
                ShortcutKeyDisplayString = "Ctrl+E"
            };
            item.Click += (_, _) => Prompt(grid);
            menu.Items.Add(item);
        }

        private static void RemoveNamed(ContextMenuStrip menu, string name)
        {
            for (var i = menu.Items.Count - 1; i >= 0; i--)
            {
                if (menu.Items[i].Name != name)
                    continue;
                var item = menu.Items[i];
                menu.Items.RemoveAt(i);
                item.Dispose();
            }
        }

        private static void MarkMenuShown(DataGridView grid)
        {
            if (Hooks.TryGetValue(grid, out var hook))
                hook.MenuForClick = hook.Click;
        }

        private static void Track(ContextMenuStrip menu)
        {
            for (var i = ShownMenus.Count - 1; i >= 0; i--)
            {
                if (!ShownMenus[i].TryGetTarget(out var existing) || existing.IsDisposed)
                {
                    ShownMenus.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(existing, menu))
                    return;
            }

            ShownMenus.Add(new WeakReference<ContextMenuStrip>(menu));
        }

        private static bool MenuIsOpen(DataGridView grid)
        {
            if (grid.ContextMenuStrip is { Visible: true, IsDisposed: false })
                return true;

            foreach (var weak in ShownMenus)
            {
                if (!weak.TryGetTarget(out var menu) || menu.IsDisposed || !menu.Visible)
                    continue;
                if (Serves(menu, grid))
                    return true;
            }

            return false;
        }

        private static bool Serves(ContextMenuStrip menu, DataGridView grid)
        {
            var source = menu.SourceControl;
            if (source is null)
                return false;
            return ReferenceEquals(source, grid) || grid.Contains(source) || source.Contains(grid);
        }

        private static void Forget(DataGridView grid)
        {
            var stale = ContextMenus
                .Where(pair => ReferenceEquals(pair.Value, grid) || pair.Key.IsDisposed)
                .Select(pair => pair.Key)
                .ToList();
            foreach (var menu in stale)
                ContextMenus.Remove(menu);
        }

        private static bool IsExport(KeyEventArgs e) =>
            e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.E;

        private static bool IsScrollBar(DataGridView grid, Point location)
        {
            var hit = grid.HitTest(location.X, location.Y);
            return hit.Type is DataGridViewHitTestType.HorizontalScrollBar
                or DataGridViewHitTestType.VerticalScrollBar;
        }

        private static DataGridView? ParentGrid(Control control)
        {
            for (Control? current = control; current is not null; current = current.Parent)
            {
                if (current is DataGridView grid)
                    return grid;
            }

            return null;
        }

        private static string Header(DataGridViewColumn column)
        {
            if (!string.IsNullOrWhiteSpace(column.HeaderText))
                return column.HeaderText;
            if (!string.IsNullOrWhiteSpace(column.DataPropertyName))
                return column.DataPropertyName;
            return column.Name;
        }

        private static string CellText(DataGridViewCell cell, CultureInfo culture)
        {
            var value = cell.Value;
            if (value is null or DBNull)
                return "";
            if (value is bool boolean)
                return boolean ? "true" : "false";
            if (value is CheckState state)
            {
                return state switch
                {
                    CheckState.Checked => "true",
                    CheckState.Unchecked => "false",
                    _ => ""
                };
            }

            if (value is DateTime dateTime)
                return dateTime.ToString(DateFormat(cell, dateTime), culture);
            if (value is DateTimeOffset offset)
                return offset.ToString(DateFormat(cell, offset.DateTime), culture);
            if (value is DateOnly date)
                return date.ToString(DateFormat(cell, date.ToDateTime(TimeOnly.MinValue)), culture);
            if (IsNumber(value) && value is IFormattable number)
                return number.ToString(NumberPattern(cell), culture) ?? "";
            if (value is string text)
                return text;
            if (value is Image)
                return "";

            return DisplayText(cell, culture);
        }

        private static string DisplayText(DataGridViewCell cell, CultureInfo culture)
        {
            try
            {
                return cell.FormattedValue switch
                {
                    null or DBNull => "",
                    string text => text,
                    bool boolean => boolean ? "true" : "false",
                    IFormattable formattable => formattable.ToString(null, culture) ?? "",
                    var other => Convert.ToString(other, culture) ?? ""
                };
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException or ArgumentException)
            {
                return Convert.ToString(cell.Value, culture) ?? "";
            }
        }

        private static string DateFormat(DataGridViewCell cell, DateTime value)
        {
            var format = ColumnFormat(cell);
            if (!string.IsNullOrEmpty(format))
                return format;
            return value.TimeOfDay == TimeSpan.Zero ? "d" : "g";
        }

        private static string NumberPattern(DataGridViewCell cell)
        {
            var format = ColumnFormat(cell);
            if (string.IsNullOrEmpty(format) || format is "G" or "g")
                return "G";

            var kind = format[0];
            if (kind is not ('c' or 'C' or 'n' or 'N' or 'f' or 'F'))
                return format;

            var digits = format.Length == 1
                ? 2
                : int.TryParse(format.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : -1;
            if (digits < 0)
                return format;

            digits = Math.Clamp(digits, 0, 8);
            return digits == 0 ? "0" : "0." + new string('0', digits);
        }

        private static string? ColumnFormat(DataGridViewCell cell)
        {
            try
            {
                var format = cell.InheritedStyle.Format;
                if (!string.IsNullOrEmpty(format))
                    return format;
            }
            catch (InvalidOperationException)
            {
            }

            return cell.OwningColumn?.DefaultCellStyle.Format;
        }

        private static bool IsNumber(object value) =>
            value is byte or sbyte or short or ushort or int or uint or long or ulong
                or float or double or decimal;

        private static void WriteRow(StringBuilder csv, string separator, IEnumerable<string> fields)
        {
            var first = true;
            foreach (var field in fields)
            {
                if (!first)
                    csv.Append(separator);
                first = false;
                csv.Append(Escape(field, separator));
            }

            csv.Append("\r\n");
        }

        private static string Escape(string value, string separator)
        {
            if (value.Contains('"') || value.Contains('\r') || value.Contains('\n') || value.Contains(separator))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static string FileName(DataGridView grid)
        {
            var raw = SectionTitle(grid);
            if (string.IsNullOrWhiteSpace(raw))
                raw = grid.FindForm()?.Text;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "Export";

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(raw.Length);
            foreach (var ch in raw.Trim())
                builder.Append(invalid.Contains(ch) ? ' ' : ch);

            var name = builder.ToString();
            while (name.Contains("  ", StringComparison.Ordinal))
                name = name.Replace("  ", " ", StringComparison.Ordinal);
            name = name.Trim().TrimEnd('.');
            if (name.Length == 0)
                name = "Export";
            if (name.Length > 80)
                name = name[..80].Trim().TrimEnd('.');
            return name + ".csv";
        }

        private static string? SectionTitle(DataGridView grid)
        {
            var title = TopLabel(grid.Parent);
            return title ?? TopLabel(grid.Parent?.Parent);
        }

        private static string? TopLabel(Control? parent)
        {
            if (parent is null)
                return null;
            foreach (Control child in parent.Controls)
            {
                if (child is Label { Dock: DockStyle.Top } label && !string.IsNullOrWhiteSpace(label.Text))
                    return label.Text.Trim();
            }

            return parent is GroupBox group && !string.IsNullOrWhiteSpace(group.Text)
                ? group.Text.Trim()
                : null;
        }

        private sealed class Hook
        {
            public int Click;
            public int MenuForClick = -1;
            public Point Location;
        }
    }
}
