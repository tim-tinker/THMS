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
        private const string ChooseValuation = "(choose a valuation)";

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
        private readonly ComboBox _assessmentMethod = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly ComboBox _levyMethod = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly ComboBox _valuation = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
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
        private readonly NumericUpDown _ratio = Percent();
        private readonly NumericUpDown _assessed = Money();
        private readonly Panel _adjustHost = new() { BackColor = Color.White, Margin = Padding.Empty };
        private Label _capCaption = null!;
        private Control _capRow = null!;
        private TableLayoutPanel _valuesSection = null!;
        private int _adjustRowIndex = -1;
        private bool _calculating;
        private readonly DataGridView _lines = new();
        private readonly BindingList<JurisdictionRow> _rows = [];
        private readonly Label _totalDue = new() { AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        private DataGridViewTextBoxColumn _taxableColumn = null!;
        private DataGridViewTextBoxColumn _rateColumn = null!;
        private DataGridViewTextBoxColumn _taxColumn = null!;
        private bool _calculatingLines;
        private bool _filling;
        private readonly List<Account> _collectors;
        private readonly List<PropertyAsset> _properties;
        private bool _loading = true;
        private bool _seeding;

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
            _assessmentMethod.Items.AddRange([
                AssessmentMethods.ApplyHomesteadCap,
                AssessmentMethods.EqualMarketValue,
                AssessmentMethods.AssessmentRatio,
                AssessmentMethods.Manual
            ]);
            _assessmentMethod.SelectedIndex = 0;
            _levyMethod.Items.AddRange([
                LevyMethods.RatePerHundred,
                LevyMethods.Millage,
                LevyMethods.Manual
            ]);
            _levyMethod.SelectedIndex = 0;
            _valuation.Items.Add(ChooseValuation);
            _valuation.SelectedIndex = 0;
            _valuation.FormattingEnabled = true;
            _valuation.Format += (_, args) =>
            {
                args.Value = args.ListItem switch
                {
                    PropertyAppraisal appraisal => appraisal.Label,
                    string text => text,
                    _ => ""
                };
            };
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
                FieldRow("Assessment method", _assessmentMethod),
                FieldRow("Tax calculation", _levyMethod),
                FieldRow("Exemptions", _exemptions)
            };
            _cap.Dock = DockStyle.Fill;
            _ratio.Dock = DockStyle.Fill;
            _ratio.Visible = false;
            _ratio.Value = 100m;
            _adjustHost.Controls.Add(_ratio);
            _adjustHost.Controls.Add(_cap);
            var valueRows = new Control[]
            {
                FieldRow("Valuation", _valuation),
                FieldRow("Land market", _land),
                FieldRow("Improvement market", _improvement),
                FieldRow("Total market", _total, bold: true),
                FieldRow("Less homestead", _adjustHost),
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
            _valuesSection = (TableLayoutPanel)values;
            _capRow = valueRows[4];
            _capCaption = _capRow.Controls.OfType<Label>().First();
            _adjustRowIndex = _valuesSection.GetRow(_capRow);

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
            _assessmentMethod.SelectedIndexChanged += (_, _) => RecalculateAppraisal();
            _levyMethod.SelectedIndexChanged += (_, _) => RecalculateLines();
            _land.ValueChanged += (_, _) => RecalculateAppraisal();
            _land.Leave += (_, _) => RecalculateAppraisal();
            _improvement.ValueChanged += (_, _) => RecalculateAppraisal();
            _improvement.Leave += (_, _) => RecalculateAppraisal();
            _cap.ValueChanged += (_, _) => RecalculateAppraisal();
            _cap.Leave += (_, _) => RecalculateAppraisal();
            _ratio.ValueChanged += (_, _) => RecalculateAppraisal();
            _ratio.Leave += (_, _) => RecalculateAppraisal();
            _rows.ListChanged += (_, e) =>
            {
                if (_loading || _calculatingLines || _seeding)
                    return;
                if (e.ListChangedType == ListChangedType.ItemAdded)
                    ScheduleLineRecalculation();
                else if (e.ListChangedType == ListChangedType.ItemChanged)
                    RecalculateLines();
                else
                    RefreshTotal();
            };
            _lines.CellEndEdit += (_, _) => RefreshTotal();

            BindChoices(collector, property);
            if (existing is not null)
                LoadStatement(existing);
            else
            {
                _taxYear.Value = DateTime.Today.Year;
                _process.Value = DateTime.Today;
                _due.Value = DateTime.Today;
                if (_property.SelectedItem is PropertyAsset selected)
                    SeedFromPreviousStatement(selected);
                else
                    _rows.Add(new JurisdictionRow());
            }

            _loading = false;
            _collectorName.Enabled = IsNew(_collector);
            LockCopiedFields();
            ReloadAppraisals(_existing?.ValuationId is Guid valuationId && valuationId != Guid.Empty
                ? valuationId
                : _existing?.Id);
            RecalculateAppraisal();
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
                RecalculateAppraisal();
                RecalculateLines();
                var collector = ResolveCollector();
                var property = ResolveProperty();
                if (_valuation.SelectedItem is not PropertyAppraisal appraisal)
                    throw new InvalidOperationException("Choose the valuation for this statement.");
                var statement = _existing ?? new PropertyTaxStatement();
                statement.AccountId = collector.Id;
                statement.TaxYear = (int)_taxYear.Value;
                statement.StatementDate = _process.Value.Date;
                statement.DueDate = _due.Value.Date;
                statement.Notes = _notes.Text.Trim();
                statement.AssessmentMethod = property.AssessmentMethod;
                statement.LevyMethod = property.LevyMethod;
                statement.ValuationId = appraisal.Id;
                statement.LandMarketValue = _land.Value;
                statement.ImprovementMarketValue = _improvement.Value;
                statement.TotalMarketValue = _total.Value;
                statement.HomesteadCap = property.AssessmentMethod is AssessmentMethod.ApplyHomesteadCap or AssessmentMethod.Manual
                    ? _cap.Value
                    : 0m;
                statement.AssessmentRatioPercent = _ratio.Value;
                statement.AssessedValue = _assessed.Value;
                statement.Exemptions = _exemptions.Text.Trim();
                var levy = SelectedLevyMethod();
                statement.Jurisdictions = _rows
                    .Where(row => !row.IsBlank(levy))
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
            if (_property.SelectedItem is not PropertyAsset property)
                throw new InvalidOperationException("Choose the property.");
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
            _property.SelectedItem = propertyMatch;
            if (propertyMatch is not null)
                LoadProperty(propertyMatch);
        }

        private void LoadStatement(PropertyTaxStatement statement)
        {
            _taxYear.Value = statement.TaxYear is >= 1900 and <= 2200 ? statement.TaxYear : DateTime.Today.Year;
            _process.Value = statement.StatementDate;
            _due.Value = statement.DueDate;
            _notes.Text = statement.Notes ?? "";
            _assessmentMethod.SelectedItem = AssessmentMethods.Description(statement.AssessmentMethod);
            _levyMethod.SelectedItem = LevyMethods.Description(statement.LevyMethod);
            _land.Value = Clamp(_land, statement.LandMarketValue);
            _improvement.Value = Clamp(_improvement, statement.ImprovementMarketValue);
            _total.Value = Clamp(_total, statement.TotalMarketValue);
            _cap.Value = Clamp(_cap, statement.HomesteadCap);
            _ratio.Value = Clamp(_ratio, statement.AssessmentRatioPercent);
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
                _assessmentMethod.SelectedItem = AssessmentMethods.ApplyHomesteadCap;
                _levyMethod.SelectedItem = LevyMethods.RatePerHundred;
                ReloadAppraisals(null);
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
            _assessmentMethod.SelectedItem = AssessmentMethods.Description(asset.AssessmentMethod);
            _levyMethod.SelectedItem = LevyMethods.Description(asset.LevyMethod);
            if (_loading)
                return;
            ReloadAppraisals(null);
            if (_existing is null)
                SeedFromPreviousStatement(asset);
        }

        private void SeedFromPreviousStatement(PropertyAsset asset)
        {
            var year = (int)_taxYear.Value;
            var prior = _orchestrator.GetPropertyTaxStatements(asset.Id)
                .Where(statement => statement.TaxYear < year)
                .OrderByDescending(statement => statement.TaxYear)
                .ThenByDescending(statement => statement.StatementDate)
                .FirstOrDefault();

            _seeding = true;
            try
            {
                _rows.Clear();
                if (prior is null)
                    _rows.Add(new JurisdictionRow());
                else
                {
                    _exemptions.Text = prior.Exemptions ?? "";
                    if (_collector.SelectedItem is not Account)
                    {
                        var collector = _collectors.FirstOrDefault(account => account.Id == prior.AccountId);
                        if (collector is not null)
                        {
                            _collector.SelectedItem = collector;
                            _collectorName.Text = collector.Name;
                        }
                    }

                    foreach (var line in prior.Jurisdictions)
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
            }
            finally
            {
                _seeding = false;
            }

            if (!_loading)
                RecalculateLines();
        }

        private AssessmentMethod SelectedAssessmentMethod() =>
            AssessmentMethods.Parse(_assessmentMethod.SelectedItem as string);

        private LevyMethod SelectedLevyMethod() =>
            LevyMethods.Parse(_levyMethod.SelectedItem as string);

        private void RecalculateAppraisal()
        {
            if (_loading || _calculating || _filling)
                return;

            _calculating = true;
            try
            {
                var method = SelectedAssessmentMethod();
                SetLocked(_land, true);
                SetLocked(_improvement, true);
                SetLocked(_total, true);
                SetLocked(_cap, true);
                SetLocked(_ratio, true);
                SetLocked(_assessed, true);

                var showRatio = method == AssessmentMethod.AssessmentRatio;
                var showCap = method is AssessmentMethod.ApplyHomesteadCap or AssessmentMethod.Manual;
                _capCaption.Text = showRatio ? "Assessment ratio %" : "Less homestead";
                _cap.Visible = showCap;
                _ratio.Visible = showRatio;
                _cap.Enabled = true;
                if (showRatio)
                    _ratio.BringToFront();
                if (showCap)
                    _cap.BringToFront();
                SetAdjustRowVisible(showCap || showRatio);
            }
            finally
            {
                _calculating = false;
            }

            RecalculateLines();
        }

        private void ReloadAppraisals(Guid? preferredId)
        {
            var property = _property.SelectedItem as PropertyAsset;
            var appraisals = property is null
                ? []
                : _orchestrator.GetPropertyAppraisals(property.Id);
            PropertyAppraisal? match = null;
            if (preferredId is Guid id && id != Guid.Empty)
                match = appraisals.FirstOrDefault(item => item.Id == id || item.SourceStatementId == id);
            match ??= appraisals.Count == 1 ? appraisals[0] : null;

            _valuation.SelectedIndexChanged -= OnValuationChanged;
            _valuation.Items.Clear();
            _valuation.Items.Add(ChooseValuation);
            foreach (var appraisal in appraisals)
                _valuation.Items.Add(appraisal);
            _valuation.SelectedItem = match ?? (object)ChooseValuation;
            _valuation.SelectedIndexChanged += OnValuationChanged;
            ApplySelectedAppraisal();
        }

        private void OnValuationChanged(object? sender, EventArgs e) => ApplySelectedAppraisal();

        private void ApplySelectedAppraisal()
        {
            if (_loading || _filling || _valuation.SelectedItem is not PropertyAppraisal appraisal)
                return;

            _filling = true;
            try
            {
                _land.Value = Clamp(_land, appraisal.LandMarketValue);
                _improvement.Value = Clamp(_improvement, appraisal.ImprovementMarketValue);
                _total.Value = Clamp(_total, appraisal.TotalMarketValue);
                _cap.Value = Clamp(_cap, appraisal.HomesteadCap);
                _ratio.Value = Clamp(_ratio, appraisal.AssessmentRatioPercent);
                _assessed.Value = Clamp(_assessed, appraisal.AssessedValue);
            }
            finally
            {
                _filling = false;
            }

            RecalculateAppraisal();
        }

        private void LockCopiedFields()
        {
            foreach (var box in new[] { _propertyName, _accountNo, _cad, _address, _roll, _legal })
            {
                box.ReadOnly = true;
                box.BackColor = SystemColors.Control;
                box.TabStop = false;
            }

            SetLocked(_acres, true);
            _assessmentMethod.Enabled = false;
            _levyMethod.Enabled = false;
        }

        private void ScheduleLineRecalculation()
        {
            if (!IsHandleCreated)
            {
                RecalculateLines();
                return;
            }

            BeginInvoke(() =>
            {
                if (!IsDisposed)
                    RecalculateLines();
            });
        }

        private void RecalculateLines()
        {
            if (_loading || _calculatingLines || IsDisposed)
                return;

            _calculatingLines = true;
            try
            {
                ApplyLevyColumns();
                var method = SelectedLevyMethod();
                if (method == LevyMethod.Manual)
                    return;

                var assessed = _assessed.Value;
                foreach (var row in _rows)
                {
                    row.TaxableValue = PropertyLevy.TaxableValue(assessed, row.ExemptionAmount);
                    row.TaxAmount = PropertyLevy.Tax(method, row.TaxableValue, row.RatePerHundred);
                }
            }
            finally
            {
                _calculatingLines = false;
                RefreshTotal();
            }
        }

        private void ApplyLevyColumns()
        {
            if (_taxableColumn is null || _rateColumn is null || _taxColumn is null)
                return;

            var method = SelectedLevyMethod();
            var locked = method != LevyMethod.Manual;
            _rateColumn.HeaderText = method == LevyMethod.Millage ? "Mills" : "Rate per $100";
            LockColumn(_taxableColumn, locked);
            LockColumn(_taxColumn, locked);
        }

        private static void LockColumn(DataGridViewColumn column, bool locked)
        {
            column.ReadOnly = locked;
            column.DefaultCellStyle.BackColor = locked ? SystemColors.Control : Color.White;
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
            var growth = height - _valuesSection.Height;
            _valuesSection.Height = height;
            if (growth > 0 && IsHandleCreated && WindowState == FormWindowState.Normal)
                ClientSize = new Size(ClientSize.Width, ClientSize.Height + growth);
        }

        private static void SetLocked(NumericUpDown box, bool locked)
        {
            box.ReadOnly = locked;
            box.InterceptArrowKeys = !locked;
            box.Increment = locked ? 0 : 100;
            box.TabStop = !locked;
            box.BackColor = locked ? SystemColors.Control : SystemColors.Window;
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
            _taxableColumn = MoneyColumn(nameof(JurisdictionRow.TaxableValue), "Taxable value");
            _rateColumn = RateColumn();
            _taxColumn = MoneyColumn(nameof(JurisdictionRow.TaxAmount), "Taxes");
            _lines.Columns.Add(_taxableColumn);
            _lines.Columns.Add(_rateColumn);
            _lines.Columns.Add(_taxColumn);
            ApplyLevyColumns();
            DataGridViewUtil.EnableDoubleBuffering(_lines);
        }

        private static DataGridViewTextBoxColumn TextColumn(string property, string header, int weight) =>
            new()
            {
                Name = property,
                DataPropertyName = property,
                HeaderText = header,
                FillWeight = weight
            };

        private static DataGridViewTextBoxColumn MoneyColumn(string property, string header) =>
            new()
            {
                Name = property,
                DataPropertyName = property,
                HeaderText = header,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight }
            };

        private static DataGridViewTextBoxColumn RateColumn() =>
            new()
            {
                Name = nameof(JurisdictionRow.RatePerHundred),
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

        private static bool IsNew(ComboBox box) => box.SelectedItem is string;

        private sealed class JurisdictionRow : INotifyPropertyChanged
        {
            private string _name = "";
            private decimal _exemption;
            private decimal _taxable;
            private decimal _rate;
            private decimal _tax;

            public event PropertyChangedEventHandler? PropertyChanged;

            public string Name
            {
                get => _name;
                set => Set(ref _name, value, nameof(Name));
            }

            public decimal ExemptionAmount
            {
                get => _exemption;
                set => Set(ref _exemption, value, nameof(ExemptionAmount));
            }

            public decimal TaxableValue
            {
                get => _taxable;
                set => Set(ref _taxable, value, nameof(TaxableValue));
            }

            public decimal RatePerHundred
            {
                get => _rate;
                set => Set(ref _rate, value, nameof(RatePerHundred));
            }

            public decimal TaxAmount
            {
                get => _tax;
                set => Set(ref _tax, value, nameof(TaxAmount));
            }

            public bool IsBlank(LevyMethod method) =>
                string.IsNullOrWhiteSpace(Name)
                && ExemptionAmount == 0
                && RatePerHundred == 0
                && (method != LevyMethod.Manual || (TaxableValue == 0 && TaxAmount == 0));

            private void Set<T>(ref T field, T value, string property)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
