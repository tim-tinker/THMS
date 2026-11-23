using THMS.Domain.Transportation;
using THMS.Logic.ViewModels;

namespace THMS.UI.WinForms
{
    public partial class AddVehicleForm : Form
    {
        private readonly VehicleListViewModel _vm;
        private readonly VehicleBase? _existing;

        public AddVehicleForm(VehicleListViewModel vm, VehicleBase? vehicle = null)
        {
            InitializeComponent();
            _vm = vm;
            _existing = vehicle;
            if (vehicle is not null)
                ShowVehicle(vehicle);
        }

        private void ShowVehicle(VehicleBase vehicle)
        {
            Text = "Edit Vehicle";
            txtName.Text = vehicle.Name;
            txtMake.Text = vehicle.Make;
            txtModel.Text = vehicle.Model;
            SetNumeric(numYear, vehicle.Year);
            switch (vehicle)
            {
                case VehicleEv ev:
                    _checkEv.Checked = true;
                    SetNumeric(_numericFuelCapacity, ev.BatteryCapacityKwh);
                    break;
                case VehicleIce ice:
                    _checkEv.Checked = false;
                    SetNumeric(_numericFuelCapacity, ice.FuelTankCapacityGallons);
                    break;
            }

            OnCheckedChangedEv(_checkEv, EventArgs.Empty);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vehicle name is required.");
                return;
            }

            var vehicle = BuildVehicle();
            if (_existing is null)
                _vm.AddVehicle(vehicle);
            else
                _vm.UpdateVehicle(vehicle);

            DialogResult = DialogResult.OK;
            Close();
        }

        private VehicleBase BuildVehicle()
        {
            var id = _existing?.Id ?? Guid.NewGuid();
            var name = txtName.Text.Trim();
            var make = txtMake.Text.Trim();
            var model = txtModel.Text.Trim();
            var year = (int)numYear.Value;
            var capacity = _numericFuelCapacity.Value;
            if (_checkEv.Checked)
            {
                var existingEv = _existing as VehicleEv;
                return new VehicleEv
                {
                    Id = id,
                    Name = name,
                    Make = make,
                    Model = model,
                    Year = year,
                    Vin = _existing?.Vin ?? string.Empty,
                    BatteryCapacityKwh = capacity,
                    ChargePortType = existingEv?.ChargePortType ?? "J1772",
                    ChargeEnergy = existingEv?.ChargeEnergy
                };
            }

            return new VehicleIce
            {
                Id = id,
                Name = name,
                Make = make,
                Model = model,
                Year = year,
                Vin = _existing?.Vin ?? string.Empty,
                FuelTankCapacityGallons = capacity,
                FuelType = _existing is VehicleIce ice ? ice.FuelType : "Gasoline"
            };
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnCheckedChangedEv(object sender, EventArgs e)
        {
            _labelFuelType.Text = _checkEv.Checked ? "kiloWatt-hours" : "gallons";
        }

        private static void SetNumeric(NumericUpDown control, decimal value)
        {
            if (value > control.Maximum)
                control.Maximum = value;
            if (value < control.Minimum)
                control.Minimum = value;

            var scale = (decimal.GetBits(value)[3] >> 16) & 0x7F;
            if (scale > control.DecimalPlaces)
                control.DecimalPlaces = Math.Min(scale, 6);

            var rounded = decimal.Round(value, control.DecimalPlaces, MidpointRounding.AwayFromZero);
            if (rounded > control.Maximum)
                control.Maximum = rounded;
            if (rounded < control.Minimum)
                control.Minimum = rounded;
            control.Value = rounded;
        }
    }
}
