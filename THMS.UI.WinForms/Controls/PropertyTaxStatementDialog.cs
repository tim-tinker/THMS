using System.ComponentModel;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class PropertyTaxStatementDialog : Form
    {
        private const string NewCollector = "(new collector)";
        private const string NewProperty = "(new property)";

        private readonly AssetOrchestrator _orchestrator;
        private readonly PropertyTaxStatement? _existing;
        private readonly ComboBox _collector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly TextBox _collectorName = new() { Width = 220 };
        private readonly ComboBox _property = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly TextBox _propertyName = new() { Width = 220 };
        private readonly NumericUpDown _taxYear = new() { Minimum = 1900, Maximum = 2200, Width = 80 };
        private readonly DateTimePicker _process = new() { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _due = new() { Format = DateTimePickerFormat.Short };
        private readonly TextBox _notes = new() { Width = 220 };
        private readonly TextBox _accountNo = new() { Width = 180 };
        private readonly TextBox _cad = new() { Width = 140 };
        private readonly TextBox _address = new() { Width = 260 };
        private readonly NumericUpDown _acres = new() { DecimalPlaces = 4, Maximum = 100000m, Width = 100 };
        private readonly TextBox _roll = new() { Width = 140 };
        private readonly TextBox _legal = new() { Width = 260 };
        private readonly TextBox _exemptions = new() { Width = 260 };
        private readonly NumericUpDown _land = Money();
        private readonly NumericUpDown _improvement = Money();
        private readonly NumericUpDown _total = Money();
        private readonly NumericUpDown _cap = Money();
        private readonly NumericUpDown _assessed = Money();
        private readonly DataGridView _lines = new();
        private readonly BindingList<JurisdictionRow> _rows = [];
        private readonly Label _totalDue = new() { AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        private readonly List<Account> _collectors;
        private readonly List<PropertyAsset> _properties;
        private bool _loading = true;

        public PropertyTaxStatementDialog(
            AssetOrchestrator orchestrator,
            PropertyTaxStatement? existing,
            Account? collector,
            PropertyAsset? property)
        {
            _orchestrator = orchestrator;
            _existing = existing;
            _collectors = orchestrator.GetTaxCollectors().ToList();
            _properties = orchestrator.GetAssetRows()
                .Select(row => orchestrator.GetAsset(row.Id))
                .OfType<PropertyAsset>()
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Text = existing is null ? "Record Property Tax Statement" : "Edit Property Tax Statement";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.Control;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(1120, 860);

            _collector.FormattingEnabled = true;
            _property.FormattingEnabled = true;
            _totalDue.Text = 0m.ToString("C2");
            _totalDue.AutoSize = false;
            _totalDue.Font = new Font(Font, FontStyle.Bold);
            ConfigureGrid();

            var statementRows = new Control[]
            {
                FieldRow("Collector", _collector),
                FieldRow("Collector name", _collectorName),
                FieldRow("Property", _property),
                FieldRow("Name", _propertyName),
                FieldRow("Tax year", _taxYear),
                FieldRow("Process date", _process),
                FieldRow("Due date", _due),
                FieldRow("Notes", _notes)
            };
            var propertyRows = new Control[]
            {
                FieldRow("Account no.", _accountNo),
                FieldRow("CAD no.", _cad),
                FieldRow("Legal acres", _acres),
                FieldRow("Roll code", _roll),
                FieldRow("Address", _address),
                FieldRow("Legal description", _legal),
                FieldRow("Exemptions", _exemptions)
            };
            var valueRows = new Control[]
            {
                FieldRow("Land market", _land),
                FieldRow("Improvement market", _improvement),
                FieldRow("Total market", _total, bold: true),
                FieldRow("Less homestead", _cap),
                FieldRow("Assessed", _assessed, bold: true)
            };

            var facts = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Height = Math.Max(SectionHeight(statementRows.Length), SectionHeight(propertyRows.Length))
            };
            facts.ColumnStyles.Clear();
            facts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            facts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            facts.RowStyles.Clear();
            facts.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var statement = Section("Statement", statementRows);
            var identity = Section("Property on the bill", propertyRows);
            statement.Margin = new Padding(0, 0, 8, 0);
            identity.Margin = new Padding(8, 0, 0, 0);
            facts.Controls.Add(statement, 0, 0);
            facts.Controls.Add(identity, 1, 0);

            var values = Section("Appraised values", valueRows);
            values.Dock = DockStyle.Top;
            values.Height = SectionHeight(valueRows.Length);
            values.Margin = Padding.Empty;

            var jurisdictions = Jurisdictions();
            jurisdictions.Dock = DockStyle.Fill;
            jurisdictions.Margin = Padding.Empty;

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            content.Controls.Add(jurisdictions);
            content.Controls.Add(values);
            content.Controls.Add(facts);

            var cancel = new ThmsButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Margin = new Padding(8, 0, 0, 0) };
            var save = new ThmsButton { Text = "Save", Margin = new Padding(8, 0, 0, 0) };
            save.Click += (_, _) => OnSave();
            AcceptButton = save;
            CancelButton = cancel;
            var buttonHeight = Math.Max(save.PreferredSize.Height, cancel.PreferredSize.Height);
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 12, 0, 0),
                Margin = Padding.Empty,
                Height = buttonHeight + 12
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = SystemColors.Control,
                Padding = new Padding(18, 12, 18, 12),
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Clear();
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, buttons.Height));
            layout.Controls.Add(content, 0, 0);
            layout.Controls.Add(buttons, 0, 1);
            Controls.Add(layout);

            _collector.SelectedIndexChanged += (_, _) => _collectorName.Enabled = IsNew(_collector);
            _property.SelectedIndexChanged += (_, _) => LoadPropertyFields();
            _rows.ListChanged += (_, _) => RefreshTotal();
            _lines.CellEndEdit += (_, _) => RefreshTotal();

            BindChoices(collector, property);
            if (existing is not null)
                LoadStatement(existing);
            else
            {
                _taxYear.Value = DateTime.Today.Year;
                _process.Value = DateTime.Today;
                _due.Value = DateTime.Today;
                _rows.Add(new JurisdictionRow());
            }

            _loading = false;
            _collectorName.Enabled = IsNew(_collector);
            _propertyName.Enabled = IsNew(_property);
            RefreshTotal();
            ApplyInitialSize(facts, values, buttons, layout);
            Load += (_, _) => ApplyInitialSize(facts, values, buttons, layout);
        }

        private void ApplyInitialSize(Control facts, Control values, Control buttons, Control layout)
        {
            var commandHeight = Math.Max(buttons.Controls.Cast<Control>().Select(control => control.PreferredSize.Height).DefaultIfEmpty(36).Max(), 36) + 16;
            var jurisdiction = 8 + BarHeight + RowHeight * 4 + commandHeight + RowHeight;
            var needed = facts.Height + values.Height + jurisdiction + buttons.Height + layout.Padding.Vertical;
            var work = Screen.FromPoint(Cursor.Position).WorkingArea;
            var nonClient = Math.Max(Height - ClientSize.Height, 0);
            var clientHeight = Math.Min(needed, Math.Max(480, work.Height - nonClient - 32));
            MinimumSize = new Size(200, 200);
            ClientSize = new Size(Math.Max(ClientSize.Width, 1120), clientHeight);
            var floor = facts.Height + values.Height + 8 + BarHeight + commandHeight + RowHeight + 72
                + buttons.Height + layout.Padding.Vertical;
            MinimumSize = SizeFromClientSize(new Size(960, Math.Min(floor, clientHeight)));
        }

        private void OnSave()
        {
            try
            {
                _lines.EndEdit();
                var collector = ResolveCollector();
                var property = ResolveProperty();
                var statement = _existing ?? new PropertyTaxStatement();
                statement.AccountId = collector.Id;
                statement.TaxYear = (int)_taxYear.Value;
                statement.StatementDate = _process.Value.Date;
                statement.DueDate = _due.Value.Date;
                statement.Notes = _notes.Text.Trim();
                statement.LandMarketValue = _land.Value;
                statement.ImprovementMarketValue = _improvement.Value;
                statement.TotalMarketValue = _total.Value;
                statement.HomesteadCap = _cap.Value;
                statement.AssessedValue = _assessed.Value;
                statement.Exemptions = _exemptions.Text.Trim();
                statement.Jurisdictions = _rows
                    .Where(row => !row.IsBlank)
                    .Select(row => new PropertyTaxJurisdictionLine
                    {
                        Name = row.Name.Trim(),
                        ExemptionAmount = row.ExemptionAmount,
                        TaxableValue = row.TaxableValue,
                        RatePerHundred = row.RatePerHundred,
                        TaxAmount = row.TaxAmount
                    })
                    .ToList();
                _orchestrator.SavePropertyTaxStatement(property, statement);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private Account ResolveCollector()
        {
            if (_collector.SelectedItem is Account account)
                return account;
            return _orchestrator.EnsureTaxCollector(_collectorName.Text);
        }

        private PropertyAsset ResolveProperty()
        {
            var name = _propertyName.Text.Trim();
            if (name.Length == 0)
                throw new InvalidOperationException("Enter the property name.");

            var selected = _property.SelectedItem as PropertyAsset;
            var property = selected ?? new PropertyAsset();
            property.Name = name;
            property.ParcelAddress = _address.Text.Trim();
            property.CadNumber = _cad.Text.Trim();
            property.TaxAccountNumber = _accountNo.Text.Trim();
            property.LegalAcres = _acres.Value;
            property.RollCode = _roll.Text.Trim();
            property.LegalDescription = _legal.Text.Trim();
            if (string.IsNullOrWhiteSpace(property.Category))
                property.Category = "Real estate";
            return property;
        }

        private void BindChoices(Account? collector, PropertyAsset? property)
        {
            _collector.Items.Clear();
            _collector.Items.Add(NewCollector);
            foreach (var account in _collectors)
                _collector.Items.Add(account);
            _collector.Format += (_, args) =>
            {
                args.Value = args.ListItem switch
                {
                    Account account => account.Name,
                    string text => text,
                    _ => ""
                };
            };

            _property.Items.Clear();
            _property.Items.Add(NewProperty);
            foreach (var item in _properties)
                _property.Items.Add(item);
            _property.Format += (_, args) =>
            {
                args.Value = args.ListItem switch
                {
                    PropertyAsset asset => asset.Name,
                    string text => text,
                    _ => ""
                };
            };

            var collectorMatch = collector is null
                ? null
                : _collectors.FirstOrDefault(account => account.Id == collector.Id);
            if (_existing is not null)
                collectorMatch ??= _collectors.FirstOrDefault(account => account.Id == _existing.AccountId);
            _collector.SelectedItem = collectorMatch ?? (object)NewCollector;
            if (collectorMatch is not null)
                _collectorName.Text = collectorMatch.Name;

            var propertyMatch = property is null
                ? null
                : _properties.FirstOrDefault(item => item.Id == property.Id);
            if (_existing is not null)
                propertyMatch ??= _properties.FirstOrDefault(item => item.Id == _existing.AssetId);
            _property.SelectedItem = propertyMatch ?? (object)NewProperty;
            if (propertyMatch is not null)
                LoadProperty(propertyMatch);
        }

        private void LoadStatement(PropertyTaxStatement statement)
        {
            _taxYear.Value = statement.TaxYear is >= 1900 and <= 2200 ? statement.TaxYear : DateTime.Today.Year;
            _process.Value = statement.StatementDate;
            _due.Value = statement.DueDate;
            _notes.Text = statement.Notes ?? "";
            _land.Value = Clamp(_land, statement.LandMarketValue);
            _improvement.Value = Clamp(_improvement, statement.ImprovementMarketValue);
            _total.Value = Clamp(_total, statement.TotalMarketValue);
            _cap.Value = Clamp(_cap, statement.HomesteadCap);
            _assessed.Value = Clamp(_assessed, statement.AssessedValue);
            _exemptions.Text = statement.Exemptions;
            foreach (var line in statement.Jurisdictions)
            {
                _rows.Add(new JurisdictionRow
                {
                    Name = line.Name,
                    ExemptionAmount = line.ExemptionAmount,
                    TaxableValue = line.TaxableValue,
                    RatePerHundred = line.RatePerHundred,
                    TaxAmount = line.TaxAmount
                });
            }

            if (_rows.Count == 0)
                _rows.Add(new JurisdictionRow());
        }

        private void LoadPropertyFields()
        {
            _propertyName.Enabled = IsNew(_property);
            if (_loading)
                return;
            if (_property.SelectedItem is PropertyAsset asset)
                LoadProperty(asset);
            else
            {
                _propertyName.Text = "";
                _accountNo.Text = "";
                _cad.Text = "";
                _address.Text = "";
                _acres.Value = 0;
                _roll.Text = "";
                _legal.Text = "";
            }
        }

        private void LoadProperty(PropertyAsset asset)
        {
            _propertyName.Text = asset.Name;
            _accountNo.Text = asset.TaxAccountNumber;
            _cad.Text = asset.CadNumber;
            _address.Text = asset.ParcelAddress;
            _acres.Value = Clamp(_acres, asset.LegalAcres);
            _roll.Text = asset.RollCode;
            _legal.Text = asset.LegalDescription;
        }

        private void RemoveLine()
        {
            if (_lines.CurrentRow?.DataBoundItem is JurisdictionRow row)
                _rows.Remove(row);
        }

        private void RefreshTotal()
        {
            var total = _rows.Sum(row => row.TaxAmount);
            _totalDue.Text = total.ToString("C2");
        }

        private void ConfigureGrid()
        {
            _lines.Dock = DockStyle.Fill;
            _lines.AutoGenerateColumns = false;
            _lines.AllowUserToAddRows = false;
            _lines.AllowUserToDeleteRows = false;
            _lines.RowHeadersVisible = false;
            _lines.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _lines.BackgroundColor = Color.White;
            _lines.BorderStyle = BorderStyle.None;
            _lines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _lines.DataSource = _rows;
            _lines.Columns.Add(TextColumn(nameof(JurisdictionRow.Name), "Tax jurisdiction", 160));
            _lines.Columns.Add(MoneyColumn(nameof(JurisdictionRow.ExemptionAmount), "Exemptions"));
            _lines.Columns.Add(MoneyColumn(nameof(JurisdictionRow.TaxableValue), "Taxable value"));
            _lines.Columns.Add(RateColumn());
            _lines.Columns.Add(MoneyColumn(nameof(JurisdictionRow.TaxAmount), "Taxes"));
            DataGridViewUtil.EnableDoubleBuffering(_lines);
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header, int weight) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                FillWeight = weight
            };

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header) =>
            new()
            {
                DataPropertyName = property,
                HeaderText = header,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
            };

        private static DataGridViewTextBoxColumn RateColumn() =>
            new()
            {
                DataPropertyName = nameof(JurisdictionRow.RatePerHundred),
                HeaderText = "Rate per $100",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "0.######",
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            };

        private static readonly int RowHeight = MeasuredRowHeight();
        private static readonly int BarHeight = TextRenderer.MeasureText("Property on the bill", SectionFont).Height + 16;
        private const int ValueColumnWidth = 340;

        private static readonly Font SectionFont = new("Segoe UI", 11F, FontStyle.Bold);
        private static readonly Color SectionBarColor = Color.FromArgb(255, 232, 168);

        private Control Jurisdictions()
        {
            var heading = SectionBar("Jurisdictions");
            heading.Dock = DockStyle.Top;
            heading.Height = BarHeight;

            var total = FieldRow("Total taxes due", _totalDue, bold: true);
            total.Dock = DockStyle.Bottom;
            total.Height = RowHeight;

            var add = new ThmsButton { Text = "Add", Margin = new Padding(0, 0, 8, 0) };
            var remove = new ThmsButton { Text = "Remove", Destructive = true, Margin = Padding.Empty };
            add.Click += (_, _) => _rows.Add(new JurisdictionRow());
            remove.Click += (_, _) => RemoveLine();
            var commands = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = Padding.Empty,
                Padding = new Padding(0, 8, 0, 4),
                BackColor = Color.White,
                Height = Math.Max(add.PreferredSize.Height, remove.PreferredSize.Height) + 16
            };
            commands.Controls.Add(add);
            commands.Controls.Add(remove);

            _lines.Dock = DockStyle.Fill;
            _lines.Margin = Padding.Empty;

            var host = new Panel { BackColor = Color.White, Padding = new Padding(0, 8, 0, 0) };
            host.Controls.Add(_lines);
            host.Controls.Add(commands);
            host.Controls.Add(total);
            host.Controls.Add(heading);
            return host;
        }

        private static int MeasuredRowHeight()
        {
            using var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            using var date = new DateTimePicker { Format = DateTimePickerFormat.Short };
            using var money = new NumericUpDown();
            var editor = Math.Max(combo.PreferredSize.Height, Math.Max(date.PreferredSize.Height, money.PreferredSize.Height));
            return editor + 10;
        }

        private static Control Section(string title, IReadOnlyList<Control> rows)
        {
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = rows.Count + 2,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            stack.ColumnStyles.Clear();
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            stack.RowStyles.Clear();
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            stack.Controls.Add(new Panel { Margin = Padding.Empty, BackColor = Color.White }, 0, 0);
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
            Padding = new Padding(10, 4, 8, 4),
            Margin = Padding.Empty
        };

        private static Control FieldRow(string caption, Control value, bool bold = false)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.White
            };
            row.ColumnStyles.Clear();
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ValueColumnWidth));
            row.RowStyles.Clear();
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

            if (value is Label readOnly)
            {
                readOnly.Dock = DockStyle.Fill;
                readOnly.TextAlign = ContentAlignment.MiddleRight;
                readOnly.Margin = Padding.Empty;
                readOnly.Padding = new Padding(0, 0, 2, 0);
                row.Controls.Add(readOnly, 1, 0);
            }
            else
            {
                row.Controls.Add(EditorHost(value), 1, 0);
            }

            return row;
        }

        private static Control EditorHost(Control editor)
        {
            var height = editor.PreferredSize.Height;
            if (height <= 0)
                height = editor.Height > 0 ? editor.Height : 23;
            var extra = Math.Max(0, RowHeight - height);
            var top = extra / 2;
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(0, top, 0, extra - top),
                BackColor = Color.White
            };
            editor.Dock = DockStyle.Fill;
            editor.Margin = Padding.Empty;
            host.Controls.Add(editor);
            return host;
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

        private static bool IsNew(ComboBox box) => box.SelectedItem is string;

        private sealed class JurisdictionRow
        {
            public string Name { get; set; } = "";
            public decimal ExemptionAmount { get; set; }
            public decimal TaxableValue { get; set; }
            public decimal RatePerHundred { get; set; }
            public decimal TaxAmount { get; set; }

            public bool IsBlank =>
                string.IsNullOrWhiteSpace(Name)
                && ExemptionAmount == 0
                && TaxableValue == 0
                && RatePerHundred == 0
                && TaxAmount == 0;
        }
    }
}
