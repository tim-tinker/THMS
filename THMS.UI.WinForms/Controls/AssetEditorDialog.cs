using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms.Controls
{
    internal sealed class AssetEditorDialog : Form
    {
        private readonly AssetOrchestrator _orchestrator;
        private readonly Asset? _existing;
        private readonly ComboBox _kind = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _name = new();
        private readonly TextBox _category = new();
        private readonly ComboBox _howAcquired = new() { DropDownStyle = ComboBoxStyle.DropDown };
        private readonly TextBox _notes = new() { Multiline = true, Height = 48 };
        private readonly DateTimePicker _acquired = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
        private readonly NumericUpDown _cost = MoneyBox();
        private readonly ComboBox _secured = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label _keepIt = new()
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        private readonly Dictionary<Guid, Account> _securableById = [];
        private Control? _keepItRow;
        private readonly TextBox _address = new();
        private readonly TextBox _cad = new();
        private readonly TextBox _taxAccount = new();
        private readonly NumericUpDown _acres = new() { DecimalPlaces = 4, Maximum = 100000m, Width = 120 };
        private readonly TextBox _roll = new();
        private readonly TextBox _legal = new() { Multiline = true, Height = 44 };
        private readonly ComboBox _assessmentMethod = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _levyMethod = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _vehicles = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _year = new() { Minimum = 1900, Maximum = 2100, Width = 80 };
        private readonly TextBox _make = new();
        private readonly TextBox _model = new();
        private readonly TextBox _vin = new();
        private readonly ComboBox _powertrain = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _capacity = new()
        {
            DecimalPlaces = 1,
            Increment = 0.1m,
            Maximum = 10000m,
            Width = 120,
            TextAlign = HorizontalAlignment.Right
        };
        private readonly Label _capacityUnit = new()
        {
            Text = "gallons",
            AutoSize = false,
            Width = 48,
            TextAlign = ContentAlignment.MiddleLeft
        };
        private readonly Panel _capacityField = new() { Height = 28 };
        private readonly FlowLayoutPanel _fields = new();
        private readonly FlowLayoutPanel _buttons = new();
        private readonly Panel _scroll = new() { AutoScroll = true };
        private readonly List<Control> _propertyRows = [];
        private readonly List<Control> _vehicleRows = [];
        private bool _loading = true;

        public AssetEditorDialog(AssetOrchestrator orchestrator, Asset? existing)
        {
            _orchestrator = orchestrator;
            _existing = existing;
            Text = existing is null ? "Add Asset" : "Edit Asset";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            Padding = new Padding(20, 16, 20, 20);
            ClientSize = new Size(PreferredClientWidth, 760);
            MinimumSize = new Size(720, 480);

            _kind.Items.AddRange([AssetKinds.Property, AssetKinds.Vehicle, AssetKinds.Personal]);
            _howAcquired.Items.AddRange(["Purchase", "Lease", "Gift", "Inheritance", "Transfer"]);
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
            _powertrain.Items.AddRange(["ICE", "EV"]);
            _powertrain.SelectedIndex = 0;
            _capacity.Dock = DockStyle.Left;
            _capacityUnit.Dock = DockStyle.Left;
            _capacityUnit.Width = 72;
            // The last control added is docked first, so the number stays left of the unit.
            _capacityField.Controls.Add(_capacityUnit);
            _capacityField.Controls.Add(_capacity);
            _year.Value = DateTime.Today.Year;
            _notes.MinimumSize = new Size(0, 72);
            _legal.MinimumSize = new Size(0, 64);
            _notes.Height = 72;
            _legal.Height = 64;

            _fields.FlowDirection = FlowDirection.TopDown;
            _fields.WrapContents = false;
            _fields.Margin = Padding.Empty;
            _fields.Padding = Padding.Empty;

            AddRow("Kind", _kind, 0);
            AddRow("Name", _name, 0);
            AddRow("Category", _category, 0);
            AddRow("Acquired", _acquired, 0);
            AddRow("Cost basis", _cost, 0);
            AddRow("How acquired", _howAcquired, 0);
            AddRow("Notes", _notes, 72);
            AddKindRow("Address", _address, 0, _propertyRows);
            AddKindRow("CAD no.", _cad, 0, _propertyRows);
            AddKindRow("Tax account no.", _taxAccount, 0, _propertyRows);
            AddKindRow("Legal acres", _acres, 0, _propertyRows);
            AddKindRow("Roll code", _roll, 0, _propertyRows);
            AddKindRow("Legal description", _legal, 64, _propertyRows);
            AddKindRow("Assessment method", _assessmentMethod, 0, _propertyRows);
            AddKindRow("Tax calculation", _levyMethod, 0, _propertyRows);
            AddKindRow("Vehicle", _vehicles, 0, _vehicleRows);
            AddKindRow("Year", _year, 0, _vehicleRows);
            AddKindRow("Make", _make, 0, _vehicleRows);
            AddKindRow("Model", _model, 0, _vehicleRows);
            AddKindRow("VIN", _vin, 0, _vehicleRows);
            AddKindRow("Powertrain", _powertrain, 0, _vehicleRows);
            AddKindRow("Fuel capacity", _capacityField, 0, _vehicleRows);
            AddRow("Loan, lease, or mortgage", _secured, 0);
            _keepItRow = AddRow("If you keep it", _keepIt, 48);
            _keepItRow.Visible = false;

            _scroll.Controls.Add(_fields);

            var save = new ThmsButton { Text = "Save", Margin = new Padding(8, 0, 0, 0) };
            var cancel = new ThmsButton { Text = "Cancel", Margin = new Padding(8, 0, 0, 0), DialogResult = DialogResult.Cancel };
            save.Click += (_, _) => OnSave();
            _buttons.FlowDirection = FlowDirection.RightToLeft;
            _buttons.WrapContents = false;
            _buttons.Padding = new Padding(0, 12, 0, 0);
            _buttons.BackColor = Color.White;
            _buttons.Controls.Add(cancel);
            _buttons.Controls.Add(save);
            AcceptButton = save;
            CancelButton = cancel;

            Controls.Add(_scroll);
            Controls.Add(_buttons);

            _kind.SelectedIndexChanged += (_, _) => ShowKind();
            _secured.SelectedIndexChanged += (_, _) => ShowKeepIt();
            _vehicles.SelectedIndexChanged += (_, _) => LoadSelectedVehicle();
            _powertrain.SelectedIndexChanged += (_, _) => OnPowertrainChanged();
            Load += (_, _) => FitToContent();
            Resize += (_, _) => Arrange();
            LoadExisting();
            _loading = false;
            ShowKind();
        }

        private void LoadExisting()
        {
            var kind = _existing is null ? AssetKinds.Personal : AssetKinds.Of(_existing);
            _kind.SelectedItem = kind;
            if (_existing is null)
            {
                _acquired.Checked = false;
                BindSecured(null);
                BindVehicles(null);
                return;
            }

            _name.Text = _existing.Name;
            _category.Text = _existing.Category;
            _howAcquired.Text = _existing.HowAcquired;
            _notes.Text = _existing.AcquisitionNotes;
            _cost.Value = Clamp(_cost, _existing.CostBasis);
            if (_existing.AcquiredOn is DateTime acquired)
            {
                _acquired.Value = acquired;
                _acquired.Checked = true;
            }
            else
            {
                _acquired.Checked = false;
            }

            if (_existing is PropertyAsset property)
            {
                _address.Text = property.ParcelAddress;
                _cad.Text = property.CadNumber;
                _taxAccount.Text = property.TaxAccountNumber;
                _acres.Value = Clamp(_acres, property.LegalAcres);
                _roll.Text = property.RollCode;
                _legal.Text = property.LegalDescription;
                _assessmentMethod.SelectedItem = AssessmentMethods.Description(property.AssessmentMethod);
                _levyMethod.SelectedItem = LevyMethods.Description(property.LevyMethod);
            }

            VehicleBase? vehicle = null;
            if (_existing is VehicleAsset linked && linked.VehicleId != Guid.Empty)
                vehicle = _orchestrator.GetVehicle(linked.VehicleId);
            BindVehicles(vehicle);
            if (vehicle is not null)
                FillVehicle(vehicle);
            BindSecured(_orchestrator.GetSecuredAccount(_existing.Id)?.Id);
        }

        private void ShowKind()
        {
            var kind = _kind.SelectedItem as string;
            SetRows(_propertyRows, kind == AssetKinds.Property);
            SetRows(_vehicleRows, kind == AssetKinds.Vehicle);
            if (!_loading)
                BindSecured(_secured.SelectedItem is AccountChoice choice ? choice.Id : null);
            FitToContent();
        }

        private void SetRows(IReadOnlyList<Control> rows, bool show)
        {
            foreach (var row in rows)
                row.Visible = show;
        }

        private const int PreferredClientWidth = 820;

        private void FitToContent()
        {
            if (!IsHandleCreated)
                return;

            Arrange();
            var rows = VisibleRowsHeight();
            var buttonBar = ButtonBarHeight();
            var content = rows + buttonBar + Padding.Vertical + 16;
            var screen = Screen.FromControl(this).WorkingArea;
            var width = Math.Min(Math.Max(ClientSize.Width, PreferredClientWidth), screen.Width - 48);
            var height = Math.Min(Math.Max(content, 320), screen.Height - 48);
            ClientSize = new Size(width, height);

            var minimum = SizeFromClientSize(new Size(Math.Min(PreferredClientWidth, width), Math.Min(height, content)));
            MinimumSize = new Size(
                Math.Min(minimum.Width, screen.Width - 16),
                Math.Min(minimum.Height, screen.Height - 16));
            Arrange();
        }

        private void Arrange()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            var width = Math.Max(0, ClientSize.Width - Padding.Horizontal);
            var buttonBar = ButtonBarHeight();
            _buttons.SetBounds(Padding.Left, Math.Max(0, ClientSize.Height - Padding.Bottom - buttonBar), width, buttonBar);
            _scroll.SetBounds(Padding.Left, Padding.Top, width, Math.Max(0, _buttons.Top - Padding.Top - 12));

            var fieldWidth = Math.Max(0, _scroll.ClientSize.Width);
            foreach (Control row in _fields.Controls)
                row.Width = fieldWidth;
            _fields.SetBounds(0, 0, fieldWidth, VisibleRowsHeight());
            _fields.PerformLayout();
        }

        private int ButtonBarHeight()
        {
            var tallest = 36;
            foreach (Control button in _buttons.Controls)
                tallest = Math.Max(tallest, Math.Max(button.Height, button.PreferredSize.Height));
            return tallest + _buttons.Padding.Vertical + 8;
        }

        private int VisibleRowsHeight() =>
            _fields.Controls.Cast<Control>().Where(row => row.Visible).Sum(row => row.Height + row.Margin.Vertical);

        private void OnSave()
        {
            try
            {
                var asset = BuildAsset();
                var secured = _secured.SelectedItem is AccountChoice choice && choice.Id != Guid.Empty
                    ? choice.Id
                    : (Guid?)null;
                _orchestrator.SaveAsset(asset, secured, asset is VehicleAsset ? BuildVehicle() : null);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private Asset BuildAsset()
        {
            var kind = _kind.SelectedItem as string ?? AssetKinds.Personal;
            Asset asset = kind switch
            {
                AssetKinds.Property => new PropertyAsset
                {
                    ParcelAddress = _address.Text.Trim(),
                    CadNumber = _cad.Text.Trim(),
                    TaxAccountNumber = _taxAccount.Text.Trim(),
                    LegalAcres = _acres.Value,
                    RollCode = _roll.Text.Trim(),
                    LegalDescription = _legal.Text.Trim(),
                    AssessmentMethod = AssessmentMethods.Parse(_assessmentMethod.SelectedItem as string),
                    LevyMethod = LevyMethods.Parse(_levyMethod.SelectedItem as string)
                },
                AssetKinds.Vehicle => new VehicleAsset(),
                _ => new PersonalAsset()
            };
            if (_existing is not null)
            {
                asset.Id = _existing.Id;
                asset.CreatedOn = _existing.CreatedOn;
            }

            asset.Name = _name.Text.Trim();
            asset.Category = _category.Text.Trim();
            asset.HowAcquired = _howAcquired.Text.Trim();
            asset.AcquisitionNotes = _notes.Text.Trim();
            asset.CostBasis = _cost.Value;
            asset.AcquiredOn = _acquired.Checked ? _acquired.Value.Date : null;
            return asset;
        }

        private VehicleBase BuildVehicle()
        {
            var selected = _vehicles.SelectedItem as VehicleChoice;
            var existing = selected?.Vehicle;
            VehicleBase vehicle = _powertrain.SelectedItem as string == "EV"
                ? existing as VehicleEv ?? new VehicleEv()
                : existing as VehicleIce ?? new VehicleIce();
            if (existing is not null)
                vehicle.Id = existing.Id;
            vehicle.Name = _name.Text.Trim();
            vehicle.Year = (int)_year.Value;
            vehicle.Make = _make.Text.Trim();
            vehicle.Model = _model.Text.Trim();
            vehicle.Vin = _vin.Text.Trim();
            switch (vehicle)
            {
                case VehicleEv ev:
                    ev.BatteryCapacityKwh = _capacity.Value;
                    break;
                case VehicleIce ice:
                    ice.FuelTankCapacityGallons = _capacity.Value;
                    break;
            }

            return vehicle;
        }

        private void BindVehicles(VehicleBase? selected)
        {
            var choices = new List<VehicleChoice> { new(null, "(new vehicle)") };
            choices.AddRange(_orchestrator.GetVehiclesAvailableTo(_existing?.Id ?? Guid.Empty).Select(vehicle =>
                new VehicleChoice(vehicle, $"{vehicle.Year} {vehicle.Make} {vehicle.Model}".Trim())));
            _vehicles.DataSource = choices;
            _vehicles.DisplayMember = nameof(VehicleChoice.Label);
            var match = choices.FirstOrDefault(choice => choice.Vehicle?.Id == selected?.Id) ?? choices[0];
            _vehicles.SelectedItem = match;
        }

        private void LoadSelectedVehicle()
        {
            if (_loading)
                return;
            if (_vehicles.SelectedItem is VehicleChoice { Vehicle: not null } choice)
                FillVehicle(choice.Vehicle);
        }

        private void FillVehicle(VehicleBase vehicle)
        {
            _year.Value = vehicle.Year is >= 1900 and <= 2100 ? vehicle.Year : DateTime.Today.Year;
            _make.Text = vehicle.Make;
            _model.Text = vehicle.Model;
            _vin.Text = vehicle.Vin;
            _powertrain.SelectedItem = vehicle is VehicleEv ? "EV" : "ICE";
            _capacity.Value = Clamp(_capacity, vehicle switch
            {
                VehicleEv ev => ev.BatteryCapacityKwh,
                VehicleIce ice => ice.FuelTankCapacityGallons,
                _ => 0m
            });
            ShowCapacityUnit();
            if (string.IsNullOrWhiteSpace(_name.Text))
                _name.Text = vehicle.Name;
        }

        private void OnPowertrainChanged()
        {
            ShowCapacityUnit();
            if (_loading)
                return;

            _capacity.Value = Clamp(_capacity, (_powertrain.SelectedItem as string == "EV", SelectedVehicle()) switch
            {
                (true, VehicleEv ev) => ev.BatteryCapacityKwh,
                (false, VehicleIce ice) => ice.FuelTankCapacityGallons,
                _ => 0m
            });
        }

        private void ShowCapacityUnit() =>
            _capacityUnit.Text = _powertrain.SelectedItem as string == "EV" ? "kWh" : "gallons";

        private VehicleBase? SelectedVehicle() =>
            _vehicles.SelectedItem is VehicleChoice choice ? choice.Vehicle : null;

        private void BindSecured(Guid? selectedId)
        {
            var asset = BuildPreview();
            if (asset is null)
                return;
            var accounts = _orchestrator.GetSecurableAccounts(asset);
            _securableById.Clear();
            foreach (var account in accounts)
                _securableById[account.Id] = account;
            var choices = new List<AccountChoice> { new(Guid.Empty, "(none)") };
            choices.AddRange(accounts.Select(account => new AccountChoice(account.Id, account.Name)));

            _secured.DataSource = null;
            _secured.DataSource = choices;
            _secured.DisplayMember = nameof(AccountChoice.Name);
            _secured.SelectedItem = choices.FirstOrDefault(choice => choice.Id == selectedId) ?? choices[0];
            ShowKeepIt();
        }

        private void ShowKeepIt()
        {
            if (_keepItRow is null)
                return;

            var id = _secured.SelectedItem is AccountChoice choice ? choice.Id : Guid.Empty;
            if (id == Guid.Empty || !_securableById.TryGetValue(id, out var account) || account is not LeaseAccount lease)
            {
                _keepItRow.Visible = false;
                return;
            }

            _keepItRow.Visible = true;
            var amount = _existing is null || _existing.Id == Guid.Empty
                ? null
                : _orchestrator.LatestAmount(_existing.Id);
            if (amount is not decimal value)
            {
                _keepIt.Text = "Record a value for this vehicle. Net worth of keeping it is that value, minus the remaining payments, minus the residual.";
                return;
            }

            var equity = LeaseEquity.IfKept(value, lease.Principal, lease.Residual);
            var role = equity < 0 ? " That is a liability." : "";
            _keepIt.Text = $"{equity:c2}. Net worth of keeping it is the latest value of {value:c2}, minus the remaining payments, minus the residual of {lease.Residual:c2}.{role}";
        }

        private Asset? BuildPreview() => (_kind.SelectedItem as string) switch
        {
            AssetKinds.Property => new PropertyAsset(),
            AssetKinds.Vehicle => new VehicleAsset(),
            AssetKinds.Personal => new PersonalAsset(),
            _ => null
        };

        private void AddKindRow(string caption, Control field, int fieldHeight, List<Control> rows) =>
            rows.Add(AddRow(caption, field, fieldHeight));

        private Control AddRow(string caption, Control field, int fieldHeight)
        {
            var labelWidth = new[]
            {
                "How acquired", "Tax account no.", "Legal description", "Loan, lease, or mortgage", "Assessment method", "Tax calculation", "Fuel capacity"
            }.Max(text => TextRenderer.MeasureText(text, Font).Width) + 8;

            var row = new Panel
            {
                Height = Math.Max(fieldHeight, field.Height) + 12,
                Margin = new Padding(0, 4, 0, 4),
                Padding = new Padding(0, 4, 0, 4),
                Width = 560
            };
            var label = new Label
            {
                Text = caption,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left,
                Width = labelWidth,
                UseMnemonic = false
            };
            var gap = new Panel { Dock = DockStyle.Left, Width = 16 };
            field.Dock = DockStyle.Fill;
            row.Controls.Add(field);
            row.Controls.Add(gap);
            row.Controls.Add(label);
            _fields.Controls.Add(row);
            return row;
        }

        private static NumericUpDown MoneyBox() => new()
        {
            DecimalPlaces = 2,
            Maximum = 100_000_000m,
            ThousandsSeparator = true,
            TextAlign = HorizontalAlignment.Right,
            Width = 140
        };

        private static decimal Clamp(NumericUpDown box, decimal value)
        {
            var rounded = decimal.Round(value, box.DecimalPlaces, MidpointRounding.AwayFromZero);
            if (rounded < box.Minimum)
                return box.Minimum;
            if (rounded > box.Maximum)
                return box.Maximum;
            return rounded;
        }

        private sealed record VehicleChoice(VehicleBase? Vehicle, string Label);
        private sealed record AccountChoice(Guid Id, string Name);
    }
}
