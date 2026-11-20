using THMS.Domain.Assets;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class PropertyValuationDialog : Form
    {
        private readonly AssetOrchestrator _orchestrator;
        private readonly PropertyAsset _property;
        private readonly PropertyAppraisal _appraisal;
        private readonly DateTimePicker _asOf = new() { Format = DateTimePickerFormat.Short };
        private readonly TextBox _source = new() { Width = 260 };
        private readonly NumericUpDown _land = Money();
        private readonly NumericUpDown _improvement = Money();
        private readonly NumericUpDown _total = Money();
        private readonly NumericUpDown _cap = Money();
        private readonly NumericUpDown _ratio = Percent();
        private readonly NumericUpDown _assessed = Money();
        private readonly Panel _adjustHost = new() { BackColor = Color.White, Margin = Padding.Empty };
        private Label _capCaption = null!;
        private Control _capRow = null!;
        private TableLayoutPanel _valuesSection = null!;
        private int _adjustRowIndex = -1;
        private bool _loading = true;
        private bool _calculating;

        public PropertyValuationDialog(AssetOrchestrator orchestrator, PropertyAsset property, PropertyAppraisal? existing)
        {
            _orchestrator = orchestrator;
            _property = property;
            _appraisal = Copy(existing) ?? SeedFromLatest(orchestrator, property);

            Text = existing is null ? "Add Real Estate Valuation" : "Edit Real Estate Valuation";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.Control;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(760, 760);

            _cap.Dock = DockStyle.Fill;
            _ratio.Dock = DockStyle.Fill;
            _ratio.Visible = false;
            _adjustHost.Controls.Add(_ratio);
            _adjustHost.Controls.Add(_cap);

            var propertyRows = new Control[]
            {
                ReadOnlyRow("Name", property.Name),
                ReadOnlyRow("Account no.", property.TaxAccountNumber),
                ReadOnlyRow("CAD no.", property.CadNumber),
                ReadOnlyRow("Legal acres", property.LegalAcres.ToString("0.0000")),
                ReadOnlyRow("Roll code", property.RollCode),
                ReadOnlyRow("Address", property.ParcelAddress),
                ReadOnlyRow("Legal description", property.LegalDescription),
                ReadOnlyRow("Assessment method", AssessmentMethods.Description(property.AssessmentMethod)),
                ReadOnlyRow("Tax calculation", LevyMethods.Description(property.LevyMethod))
            };
            var valueRows = new Control[]
            {
                FieldRow("As of", _asOf),
                FieldRow("Source", _source),
                FieldRow("Land market", _land),
                FieldRow("Improvement market", _improvement),
                FieldRow("Total market", _total, bold: true),
                FieldRow("Less homestead", _adjustHost),
                FieldRow("Assessed", _assessed, bold: true)
            };

            var identity = Section("Real Estate Property", propertyRows);
            identity.Dock = DockStyle.Top;
            identity.Height = SectionHeight(propertyRows.Length);
            var values = Section("Appraised values", valueRows);
            values.Dock = DockStyle.Top;
            values.Height = SectionHeight(valueRows.Length);
            _valuesSection = (TableLayoutPanel)values;
            _capRow = valueRows[5];
            _capCaption = _capRow.Controls.OfType<Label>().First();
            _adjustRowIndex = _valuesSection.GetRow(_capRow);

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
                Padding = new Padding(0, 12, 0, 0),
                Height = save.PreferredSize.Height + 16,
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

            _land.ValueChanged += (_, _) => Recalculate();
            _improvement.ValueChanged += (_, _) => Recalculate();
            _cap.ValueChanged += (_, _) => Recalculate();
            _ratio.ValueChanged += (_, _) => Recalculate();

            _asOf.Value = _appraisal.AsOf == default ? DateTime.Today : _appraisal.AsOf;
            _source.Text = _appraisal.Source;
            _land.Value = Clamp(_land, _appraisal.LandMarketValue);
            _improvement.Value = Clamp(_improvement, _appraisal.ImprovementMarketValue);
            _total.Value = Clamp(_total, _appraisal.TotalMarketValue);
            _cap.Value = Clamp(_cap, _appraisal.HomesteadCap);
            _ratio.Value = Clamp(_ratio, _appraisal.AssessmentRatioPercent == 0m ? 100m : _appraisal.AssessmentRatioPercent);
            _assessed.Value = Clamp(_assessed, _appraisal.AssessedValue);
            _loading = false;
            Recalculate();
            FitToContent();
        }

        private Control _identity = null!;
        private FlowLayoutPanel _buttons = null!;
        private TableLayoutPanel _layout = null!;
        private Panel _content = null!;

        private void FitToContent()
        {
            GrowHeader(_identity);
            GrowHeader(_valuesSection);
            var needed = _identity.Height + _valuesSection.Height + _buttons.Height + _layout.Padding.Vertical + 8;
            var work = Screen.FromControl(this).WorkingArea;
            var chrome = Math.Max(Height - ClientSize.Height, SystemInformation.CaptionHeight);
            var maxClient = Math.Max(480, work.Height - chrome - 24);
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
                Recalculate();
                _appraisal.AsOf = _asOf.Value.Date;
                _appraisal.Source = _source.Text.Trim();
                _appraisal.LandMarketValue = _land.Value;
                _appraisal.ImprovementMarketValue = _improvement.Value;
                _appraisal.TotalMarketValue = _total.Value;
                _appraisal.HomesteadCap = _property.AssessmentMethod is AssessmentMethod.ApplyHomesteadCap or AssessmentMethod.Manual
                    ? _cap.Value
                    : 0m;
                _appraisal.AssessmentRatioPercent = _ratio.Value;
                _appraisal.AssessedValue = _assessed.Value;
                _orchestrator.SavePropertyAppraisal(_property, _appraisal);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Recalculate()
        {
            if (_loading || _calculating)
                return;

            _calculating = true;
            try
            {
                var method = _property.AssessmentMethod;
                var manual = method == AssessmentMethod.Manual;
                SetLocked(_total, !manual);
                SetLocked(_assessed, !manual);
                var showRatio = method == AssessmentMethod.AssessmentRatio;
                var showCap = method is AssessmentMethod.ApplyHomesteadCap or AssessmentMethod.Manual;
                _capCaption.Text = showRatio ? "Assessment ratio %" : "Less homestead";
                _cap.Visible = showCap;
                _ratio.Visible = showRatio;
                _cap.Enabled = showCap;
                if (showRatio)
                    _ratio.BringToFront();
                if (showCap)
                    _cap.BringToFront();
                SetAdjustRowVisible(showCap || showRatio);
                if (manual)
                    return;

                var total = PropertyAssessment.TotalMarket(_land.Value, _improvement.Value);
                _total.Value = Clamp(_total, total);
                var assessed = PropertyAssessment.AssessedValue(
                    method,
                    total,
                    method == AssessmentMethod.ApplyHomesteadCap ? _cap.Value : 0m,
                    showRatio ? _ratio.Value : 0m);
                _assessed.Value = Clamp(_assessed, assessed);
            }
            finally
            {
                _calculating = false;
            }
        }

        private void SetAdjustRowVisible(bool visible)
        {
            if (_adjustRowIndex < 0 || _valuesSection.RowStyles.Count <= _adjustRowIndex)
                return;
            _valuesSection.RowStyles[_adjustRowIndex].Height = visible ? RowHeight : 0;
            _capRow.Visible = visible;
            var height = 0;
            for (var i = 0; i < _valuesSection.RowStyles.Count; i++)
                height += (int)Math.Round(_valuesSection.RowStyles[i].Height);
            _valuesSection.Height = height;
        }

        private static PropertyAppraisal? Copy(PropertyAppraisal? source)
        {
            if (source is null)
                return null;
            var copy = new PropertyAppraisal
            {
                Id = source.Id,
                AssetId = source.AssetId,
                AsOf = source.AsOf,
                Source = source.Source,
                SourceStatementId = source.SourceStatementId,
                LandMarketValue = source.LandMarketValue,
                ImprovementMarketValue = source.ImprovementMarketValue,
                TotalMarketValue = source.TotalMarketValue,
                HomesteadCap = source.HomesteadCap,
                AssessmentRatioPercent = source.AssessmentRatioPercent,
                AssessedValue = source.AssessedValue
            };
            foreach (var pair in source.ComponentIds)
                copy.ComponentIds[pair.Key] = pair.Value;
            return copy;
        }

        private static PropertyAppraisal SeedFromLatest(AssetOrchestrator orchestrator, PropertyAsset property)
        {
            var latest = orchestrator.GetPropertyAppraisals(property.Id).FirstOrDefault();
            if (latest is null)
            {
                return new PropertyAppraisal
                {
                    AsOf = DateTime.Today,
                    AssessmentRatioPercent = 100m
                };
            }

            var seeded = Copy(latest)!;
            seeded.Id = Guid.Empty;
            seeded.AssetId = property.Id;
            seeded.SourceStatementId = null;
            seeded.ComponentIds.Clear();
            var anniversary = latest.AsOf.Date.AddYears(1);
            seeded.AsOf = anniversary < DateTime.Today ? anniversary : DateTime.Today;
            return seeded;
        }

        private static readonly Font SectionFont = new("Segoe UI", 11F, FontStyle.Bold);
        private static readonly int RowHeight = MeasuredRowHeight();
        private static readonly int BarHeight = TextRenderer.MeasureText("Real Estate Property", SectionFont).Height + 24;
        private const int ValueColumnWidth = 340;
        private static readonly Color SectionBarColor = Color.FromArgb(255, 232, 168);

        private static int MeasuredRowHeight()
        {
            using var date = new DateTimePicker { Format = DateTimePickerFormat.Short };
            using var money = new NumericUpDown();
            return Math.Max(date.PreferredSize.Height, money.PreferredSize.Height) + 10;
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
            Margin = Padding.Empty
        };

        private static Control ReadOnlyRow(string caption, string value)
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

        private static Control FieldRow(string caption, Control value, bool bold = false)
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
                Font = new Font("Segoe UI", 9F, bold ? FontStyle.Bold : FontStyle.Regular),
                Margin = Padding.Empty,
                Padding = new Padding(4, 0, 8, 0)
            }, 0, 0);
            value.Dock = DockStyle.Fill;
            value.Margin = Padding.Empty;
            row.Controls.Add(value, 1, 0);
            return row;
        }

        private static void SetLocked(NumericUpDown box, bool locked)
        {
            box.ReadOnly = locked;
            box.InterceptArrowKeys = !locked;
            box.Increment = locked ? 0 : 100;
            box.TabStop = !locked;
            box.BackColor = locked ? SystemColors.Control : SystemColors.Window;
        }

        private static NumericUpDown Money() => new()
        {
            DecimalPlaces = 2,
            Maximum = 100_000_000m,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right
        };

        private static NumericUpDown Percent() => new()
        {
            DecimalPlaces = 4,
            Maximum = 100m,
            Increment = 0.01m,
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
