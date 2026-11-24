using THMS.Domain.Assets;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class VehicleValuationDialog : RecordedValuationDialog
    {
        public VehicleValuationDialog(AssetOrchestrator orchestrator, VehicleAsset asset, AssetValuation? existing)
            : base(
                orchestrator,
                asset,
                existing,
                "Vehicle",
                "Vehicle",
                ValuationTypes.Vehicle,
                Identity(orchestrator, asset))
        {
        }

        private static IReadOnlyList<Control> Identity(AssetOrchestrator orchestrator, VehicleAsset asset)
        {
            var vehicle = asset.VehicleId == Guid.Empty ? null : orchestrator.GetVehicle(asset.VehicleId);
            var rows = new List<Control> { ReadOnlyRow("Name", asset.Name) };
            if (vehicle is not null)
            {
                rows.Add(ReadOnlyRow("Year", vehicle.Year == 0 ? "" : vehicle.Year.ToString()));
                rows.Add(ReadOnlyRow("Make", vehicle.Make));
                rows.Add(ReadOnlyRow("Model", vehicle.Model));
                rows.Add(ReadOnlyRow("VIN", vehicle.Vin));
            }

            rows.Add(ReadOnlyRow("How acquired", asset.HowAcquired));
            return rows;
        }
    }

    internal sealed class PersonalValuationDialog : RecordedValuationDialog
    {
        public PersonalValuationDialog(AssetOrchestrator orchestrator, PersonalAsset asset, AssetValuation? existing)
            : base(
                orchestrator,
                asset,
                existing,
                "Personal Property",
                "Personal property",
                ValuationTypes.Personal,
                Identity(asset))
        {
        }

        private static IReadOnlyList<Control> Identity(PersonalAsset asset) =>
        [
            ReadOnlyRow("Name", asset.Name),
            ReadOnlyRow("Category", asset.Category),
            ReadOnlyRow("How acquired", asset.HowAcquired),
            ReadOnlyRow("Acquired", asset.AcquiredOn?.ToString("d") ?? ""),
            ReadOnlyRow("Cost basis", asset.CostBasis.ToString("c2"))
        ];
    }

    internal class RecordedValuationDialog : Form
    {
        private readonly AssetOrchestrator _orchestrator;
        private readonly Guid _assetId;
        private readonly AssetValuation? _existing;
        private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _amount = Money();
        private readonly DateTimePicker _asOf = new() { Format = DateTimePickerFormat.Short };
        private readonly TextBox _source = new();
        private Control _identity = null!;
        private TableLayoutPanel _values = null!;
        private FlowLayoutPanel _buttons = null!;
        private TableLayoutPanel _layout = null!;
        private Panel _content = null!;

        protected RecordedValuationDialog(
            AssetOrchestrator orchestrator,
            Asset asset,
            AssetValuation? existing,
            string titleNoun,
            string identityTitle,
            IReadOnlyList<string> valueTypes,
            IReadOnlyList<Control> identityRows)
        {
            _orchestrator = orchestrator;
            _assetId = asset.Id;
            _existing = existing;

            Text = existing is null ? $"Add {titleNoun} Valuation" : $"Edit {titleNoun} Valuation";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.Control;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(760, 520);

            var types = valueTypes.ToList();
            if (existing is not null && !types.Contains(existing.ValueType))
                types.Insert(0, existing.ValueType);
            _type.Items.AddRange(types.Cast<object>().ToArray());
            _type.SelectedItem = existing?.ValueType ?? ValuationTypes.Market;
            if (_type.SelectedIndex < 0)
                _type.SelectedIndex = 0;
            var dropWidth = types.Count == 0
                ? 160
                : types.Max(text => TextRenderer.MeasureText(text, Font).Width) + SystemInformation.VerticalScrollBarWidth + 16;
            _type.DropDownWidth = Math.Max(ValueColumnWidth, dropWidth);

            if (existing is not null)
            {
                _amount.Value = Clamp(_amount, existing.Amount);
                _asOf.Value = existing.AsOf == default ? DateTime.Today : existing.AsOf;
                _source.Text = existing.Source;
            }

            var valueRows = new Control[]
            {
                FieldRow("Value", _type),
                FieldRow("Amount", _amount),
                FieldRow("As of", _asOf),
                FieldRow("Source", _source)
            };

            var identity = Section(identityTitle, identityRows);
            identity.Dock = DockStyle.Top;
            identity.Height = SectionHeight(identityRows.Count);
            var values = Section("Valuation", valueRows);
            values.Dock = DockStyle.Top;
            values.Height = SectionHeight(valueRows.Length);
            _values = (TableLayoutPanel)values;

            var cancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Margin = new Padding(8, 0, 0, 0) };
            var save = new ThmsButton { Text = "Save", Margin = new Padding(8, 0, 0, 0) };
            save.Click += (_, _) => OnSave();
            AcceptButton = save;
            CancelButton = cancel;
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(12, 12, 12, 12),
                Height = save.PreferredSize.Height + 28,
                BackColor = SystemColors.Control
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);

            var content = new Panel { Dock = DockStyle.Fill, AutoScroll = false, BackColor = Color.White };
            content.Controls.Add(values);
            content.Controls.Add(identity);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = SystemColors.Control,
                Padding = new Padding(18, 12, 18, 12)
            };
            layout.Controls.Add(content, 0, 0);
            Controls.Add(layout);
            Controls.Add(buttons);

            _identity = identity;
            _buttons = buttons;
            _layout = layout;
            _content = content;
            Load += (_, _) => FitToContent();
            FitToContent();
        }

        private void FitToContent()
        {
            GrowHeader(_identity);
            GrowHeader(_values);
            var buttonHeight = _buttons.Controls.Cast<Control>().Select(control => control.Height).DefaultIfEmpty(36).Max();
            _buttons.Height = buttonHeight + _buttons.Padding.Vertical;
            var needed = _identity.Height + _values.Height + _buttons.Height + _layout.Padding.Vertical + 8;
            var work = Screen.FromControl(this).WorkingArea;
            var chrome = Math.Max(Height - ClientSize.Height, SystemInformation.CaptionHeight);
            var maxClient = Math.Max(360, work.Height - chrome - 24);
            var height = Math.Min(needed, maxClient);
            ClientSize = new Size(Math.Max(ClientSize.Width, 760), height);
            MinimumSize = SizeFromClientSize(new Size(720, height));
            _content.AutoScroll = needed > height + 4;
        }

        private static void GrowHeader(Control section)
        {
            if (section is not TableLayoutPanel stack)
                return;
            var bar = stack.Controls.OfType<Label>().FirstOrDefault(label => label.BackColor == SectionBarColor);
            if (bar is null)
                return;
            var row = stack.GetRow(bar);
            if (row < 0 || stack.RowStyles.Count <= row)
                return;
            var needed = TextRenderer.MeasureText(bar.Text, bar.Font).Height + bar.Padding.Vertical + 8;
            var current = (int)Math.Round(stack.RowStyles[row].Height);
            if (needed <= current)
                return;
            stack.RowStyles[row].Height = needed;
            stack.Height += needed - current;
        }

        private void OnSave()
        {
            try
            {
                var valuation = _existing ?? new AssetValuation();
                valuation.AssetId = _assetId;
                valuation.ValueType = _type.SelectedItem as string ?? "";
                valuation.Amount = _amount.Value;
                valuation.AsOf = _asOf.Value.Date;
                valuation.Source = _source.Text.Trim();
                _orchestrator.SaveValuation(valuation);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected static Control ReadOnlyRow(string caption, string value)
        {
            var box = new TextBox
            {
                Text = value,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                TabStop = false
            };
            return FieldRow(caption, box);
        }

        private static readonly Font SectionFont = new("Segoe UI", 11F, FontStyle.Bold);
        private static readonly int RowHeight = MeasuredRowHeight();
        private static readonly int BarHeight = TextRenderer.MeasureText("Personal property", SectionFont).Height + 24;
        private const int ValueColumnWidth = 340;
        private static readonly Color SectionBarColor = Color.FromArgb(255, 232, 168);

        private static int MeasuredRowHeight()
        {
            using var date = new DateTimePicker { Format = DateTimePickerFormat.Short };
            using var money = new NumericUpDown();
            using var list = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            return Math.Max(date.PreferredSize.Height, Math.Max(money.PreferredSize.Height, list.PreferredSize.Height)) + 10;
        }

        private static Control Section(string title, IReadOnlyList<Control> rows)
        {
            var stack = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = rows.Count + 2,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            stack.Controls.Add(new Panel { BackColor = Color.White }, 0, 0);
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, BarHeight));
            stack.Controls.Add(SectionBar(title), 0, 1);
            for (var i = 0; i < rows.Count; i++)
            {
                stack.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
                stack.Controls.Add(rows[i], 0, i + 2);
            }

            return stack;
        }

        private static int SectionHeight(int dataRows) => 8 + BarHeight + RowHeight * dataRows;

        private static Label SectionBar(string title) => new()
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = SectionFont,
            BackColor = SectionBarColor,
            ForeColor = Color.Black,
            Padding = new Padding(10, 8, 8, 8),
            Margin = Padding.Empty,
            UseMnemonic = false
        };

        private static Control FieldRow(string caption, Control value)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Margin = Padding.Empty,
                BackColor = Color.White
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ValueColumnWidth));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            row.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Margin = Padding.Empty,
                Padding = new Padding(4, 0, 8, 0),
                UseMnemonic = false
            }, 0, 0);
            value.Dock = DockStyle.Fill;
            value.Margin = Padding.Empty;
            row.Controls.Add(value, 1, 0);
            return row;
        }

        private static NumericUpDown Money() => new()
        {
            DecimalPlaces = 2,
            Maximum = 100_000_000m,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right
        };

        private static decimal Clamp(NumericUpDown box, decimal value)
        {
            if (value < box.Minimum)
                return box.Minimum;
            if (value > box.Maximum)
                return box.Maximum;
            return value;
        }
    }
}
