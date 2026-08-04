using THMS.Domain.Transportation;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class EvChargeSessionForm : Form
    {
        private const decimal _whPerGasGallon = 33700;
        private decimal _lastOdometer = -1;
        private decimal _lastSoc = -1;

        private Guid _vehicleId;
        private VehicleDetailViewModel _vm;

        private VehicleEv _vehicle;
        protected VehicleEv Vehicle
        {
            get => _vehicle;
            set
            {
                _vehicle = value;
                if (_vehicle != null)
                {
                    _textVehicle.Text = $"{_vehicle.Name} ({_vehicle.Year} {_vehicle.Make} {_vehicle.Model})";
                }
            }
        }

        private readonly EvChargeSessionViewModel _sessionVM;

        protected decimal BatteryCapacityKwh => _vehicle?.BatteryCapacityKwh ?? 0;

        private decimal _milesUsed;
        protected decimal MilesUsed
        {
            get => _milesUsed;
            set
            {
                _milesUsed = value;
                _textMilesUsed.Text = $"{_milesUsed:0.0}";
                UpdateConsumption();
            }
        }

        private decimal _socUsed;
        protected decimal SocUsed
        {
            get => _socUsed;
            set
            {
                _socUsed = value;
                _textSocUsed.Text = $"{_socUsed:0.0}";
                UpdateConsumption();
            }
        }

        private decimal _socAdded;
        protected decimal SocAdded
        {
            get => _socAdded;
            set
            {
                _socAdded = value;
                _textSocAdded.Text = $"{_socAdded:0.0}";
                UpdateConsumption();
            }
        }

        private decimal _gridKwh;
        protected decimal GridKwh
        {
            get => _gridKwh;
            set
            {
                _gridKwh = value;
                _textGridKwh.Text = $"{_gridKwh:0.00}";
                UpdateConsumption();
            }
        }

        private decimal _solarKwh;
        protected decimal SolarKwh
        {
            get => _solarKwh;
            set
            {
                _solarKwh = value;
                _textSolarKwh.Text = $"{_solarKwh:0.00}";
                UpdateConsumption();
            }
        }

        private decimal _batteryKwh;
        protected decimal BatteryKwh
        {
            get => _batteryKwh;
            set
            {
                _batteryKwh = value;
                _textBatteryKwh.Text = $"{_batteryKwh:0.00}";
                UpdateConsumption();
            }
        }

        private decimal _sessionCost;
        protected decimal SessionCost
        {
            get => _sessionCost;
            set
            {
                _sessionCost = value;
                _numSessionCost.Value = _sessionCost;
            }
        }

        private decimal _kwhAdded;
        protected decimal KwhAdded
        {
            get => _kwhAdded;
            set
            {
                _kwhAdded = value;
                _numKwhAdded.Value = _kwhAdded;
                UpdateConsumption();
            }
        }

        private decimal _costPerMile;
        protected decimal CostPerMile
        {
            get => _costPerMile;
            set
            {
                _costPerMile = value;
                _textCostPerMile.Text = $"{_costPerMile:0.00}";
            }
        }

        protected decimal KwhUsed
        {
            get => SocUsed * BatteryCapacityKwh / 100;
        }

        protected decimal WhPerMile
        {
            get => 0 < _milesUsed ? KwhUsed * 1000 / _milesUsed : 0;
        }

        protected decimal MilesPerGallonEquivalent
        {
            get => 0 < WhPerMile ? _whPerGasGallon / WhPerMile : 0;
        }

        public EvChargeSessionForm()
        {
            InitializeComponent();
        }

        public EvChargeSessionForm(Guid vehicleId, VehicleDetailViewModel vm, EvChargingSession? existingSession = null)
            : this()
        {
            _vehicleId = vehicleId;
            _vm = vm;
            Vehicle = _vm.Vehicle as VehicleEv;
            _sessionVM = new EvChargeSessionViewModel(vm.Store, vehicleId, Vehicle, 0, 0, existingSession);
        }

        private void OnCheckedChangedHomeCharger(object sender, EventArgs e)
        {
            _btnLoadCircuitData.Enabled = _checkHomeCharger.Checked;
            _numKwhAdded.Enabled = !_checkHomeCharger.Checked;
            _numSessionCost.Enabled = !_checkHomeCharger.Checked;
        }

        private void OnValueChangedOdometer(object sender, EventArgs e)
        {
            if (-1 < _lastOdometer && _lastOdometer < _numOdometer.Value)
            {
                _milesUsed = _numOdometer.Value - _lastOdometer;
                _textMilesUsed.Text = $"{_milesUsed:N1}";
            }

            UpdateConsumption();
        }

        private void OnValueChangedStartSoc(object sender, EventArgs e)
        {
            if (-1 < _lastSoc && _lastSoc < _numStartSoc.Value)
            {
                _socUsed = _numStartSoc.Value - _lastSoc;
                _textSocUsed.Text = $"{_socUsed:N1}";
            }
        }

        private void OnValueChangedEndSoc(object sender, EventArgs e)
        {
            if (_numEndSoc.Value > _numStartSoc.Value)
            {
                _socAdded = _numEndSoc.Value - _numStartSoc.Value;
                _textSocAdded.Text = $"{_socAdded:N1}";
            }
        }

        private void UpdateConsumption()
        {
            if (MilesUsed > 0 && SocUsed > 0) return;

            _textKwhUsed.Text = $"{KwhUsed:N1}";
            _textWhPerMile.Text = $"{WhPerMile:N1}";
            _textMpge.Text = $"{MilesPerGallonEquivalent:N1}";
            if (SocAdded > 0) return;

            if (_checkHomeCharger.Checked)
            {
                KwhAdded = GridKwh + SolarKwh + BatteryKwh;
            }

            if (0 < SessionCost)
            {
                CostPerMile = MilesUsed / SessionCost;
            }
        }

        private void OnClickSave(object sender, EventArgs e)
        {
            // need to store this charge session to the data store.  If new, add it.  If existing, update it.  Then close the form.
        }
    }
}