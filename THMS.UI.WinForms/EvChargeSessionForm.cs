using System.Globalization;
using System.Linq.Expressions;
using System.Windows.Forms.DataVisualization.Charting;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class EvChargeSessionForm : Form
    {
        private const decimal _whPerGasGallon = 33700;

        private Guid _vehicleId;
        private VehicleDetailViewModel _vm = null!;

        private VehicleEv _vehicle = null!;
        protected VehicleEv Vehicle
        {
            get => _vehicle; set => _vehicle = value;
        }

        private readonly EvChargeSessionViewModel _sessionVM = null!;

        /// <summary>Session written on Save; null if the dialog was cancelled.</summary>
        public EvChargingSession? SavedSession { get; private set; }

        protected decimal BatteryCapacityKwh => _vehicle?.BatteryCapacityKwh ?? 0;

        private decimal _lastOdometer;
        protected decimal LastOdometer
        {
            get => _lastOdometer;
            set
            {
                _lastOdometer = value;
                _textLastOdometer.Text = $"{_lastOdometer}";
            }
        }

        private decimal _lastSoc;
        protected decimal LastSoc
        {
            get => _lastSoc;
            set
            {
                _lastSoc = value;
                _textLastSoc.Text = $"{_lastSoc}";
            }
        }

        private decimal _odometer;
        protected decimal Odometer
        {
            get => _odometer;
            set
            {
                _odometer = value;
                _numOdometer.Value = value;
            }
        }

        private decimal _milesUsed;
        protected decimal MilesUsed
        {
            get => _milesUsed;
            set
            {
                _milesUsed = value;
                _textMilesUsed.Text = $"{_milesUsed:0}";
                UpdateConsumption();
            }
        }

        private decimal _startSoc;
        protected decimal StartSoc
        {
            get => _startSoc;
            set
            {
                _startSoc = value;
                _numStartSoc.Value = value;
            }
        }

        private decimal _socUsed;
        protected decimal SocUsed
        {
            get => _socUsed;
            set
            {
                _socUsed = value;
                _textSocUsed.Text = $"{_socUsed:0}";
                UpdateConsumption();
            }
        }

        private decimal _endSoc;
        protected decimal EndSoc
        {
            get => _endSoc;
            set
            {
                _endSoc = value;
                _numEndSoc.Value = value;
            }
        }

        private decimal _socAdded;
        protected decimal SocAdded
        {
            get => _socAdded;
            set
            {
                _socAdded = value;
                _textSocAdded.Text = $"{_socAdded:0}";
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
                _textGridKwh.Text = $"{_gridKwh:0.000}";
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
                _textSolarKwh.Text = $"{_solarKwh:0.000}";
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
                _textBatteryKwh.Text = $"{_batteryKwh:0.000}";
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
                _numKwhAdded.Value = ClampToNumeric(_numKwhAdded, _kwhAdded);
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

        public EvChargeSessionForm(Guid vehicleId, VehicleDetailViewModel vm, decimal lastOdometer, decimal lastSoc, EvChargingSession? existingSession = null)
            : this()
        {
            _vehicleId = vehicleId;
            _vm = vm;
            Vehicle = vm.Vehicle as VehicleEv ?? throw new ArgumentException("Vehicle is not an EV.", nameof(vm));
            _sessionVM = new EvChargeSessionViewModel(vm.Store, vehicleId, Vehicle, lastOdometer, lastSoc, existingSession);
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            Text = $"EV Charging Session for {Vehicle.Name} ({Vehicle.Year} {Vehicle.Make} {Vehicle.Model})";
            LoadControlsFromViewModel();
        }

        private void LoadControlsFromViewModel()
        {
            LastOdometer = _sessionVM.LastOdometer;
            LastSoc = _sessionVM.LastSoc;
            _numOdometer.Value = ClampToNumeric(_numOdometer, _sessionVM.Odometer);
            _dateStart.Value = ClampToPicker(_sessionVM.StartTime);
            _timeStart.Value = ClampToPicker(_sessionVM.StartTime);
            _dateEnd.Value = ClampToPicker(_sessionVM.EndTime);
            _timeEnd.Value = ClampToPicker(_sessionVM.EndTime);
            _numStartSoc.Value = ClampToNumeric(_numStartSoc, _sessionVM.StartSoc);
            _numEndSoc.Value = ClampToNumeric(_numEndSoc, _sessionVM.EndSoc);
            _checkHomeCharger.Checked = _sessionVM.IsHomeCharging;
            _numKwhAdded.Value = ClampToNumeric(_numKwhAdded, _sessionVM.KwhAdded);
            _numSessionCost.Value = ClampToNumeric(_numSessionCost, _sessionVM.SessionCost);
            GridKwh = _sessionVM.GridKwh;
            SolarKwh = _sessionVM.SolarKwh;
            BatteryKwh = _sessionVM.BatteryKwh;

            EnableHomeControls(false);
            UpdateConsumption();
        }

        private void OnCheckedChangedHomeCharger(object sender, EventArgs e)
        {
            EnableHomeControls(_checkHomeCharger.Checked);
        }

        private void EnableHomeControls(bool enable)
        {
            _btnLoadCircuitData.Enabled = enable;
            _numKwhAdded.Enabled = !enable;
            _numSessionCost.Enabled = !enable;
        }

        private void OnValueChangedOdometer(object sender, EventArgs e)
        {
            _odometer = _numOdometer.Value;
            if (0 < LastOdometer && LastOdometer < Odometer)
            {
                MilesUsed = Odometer - LastOdometer;
            }

            UpdateConsumption();
        }

        private void OnValueChangedStartSoc(object sender, EventArgs e)
        {
            _startSoc = _numStartSoc.Value;
            if (0 < LastSoc && LastSoc > StartSoc)
            {
                SocUsed = LastSoc - StartSoc;
            }

            UpdateConsumption();
        }

        private void OnValueChangedEndSoc(object sender, EventArgs e)
        {
            _endSoc = _numEndSoc.Value;
            if (EndSoc > StartSoc)
            {
                SocAdded = EndSoc - StartSoc;
            }

            UpdateConsumption();
        }

        private void OnValueChangedKwhAdded(object sender, EventArgs e)
        {
            _kwhAdded = _numKwhAdded.Value;
            UpdateConsumption();
        }

        private void OnValueChangedSessionCost(object sender, EventArgs e)
        {
            _sessionCost = _numSessionCost.Value;
            UpdateConsumption();
        }

        private void UpdateConsumption()
        {
            if (MilesUsed <= 0 || SocUsed <= 0)
                return;

            _textKwhUsed.Text = $"{KwhUsed:N1}";
            _textWhPerMile.Text = $"{WhPerMile:N1}";
            _textMpge.Text = $"{MilesPerGallonEquivalent:N1}";

            if (SocAdded <= 0)
                return;

            if (_checkHomeCharger.Checked)
            {
                KwhAdded = GridKwh + SolarKwh + BatteryKwh;
            }

            if (0 < SessionCost && 0 < KwhAdded)
            {
                CostPerMile = SessionCost / KwhAdded * WhPerMile / 1000;
            }
        }

        private void OnClickSave(object sender, EventArgs e)
        {
            ApplyControlsToViewModel();

            if (_sessionVM.EndTime < _sessionVM.StartTime)
            {
                MessageBox.Show(this, "End time must be on or after start time.");
                return;
            }

            SavedSession = _sessionVM.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyControlsToViewModel()
        {
            _sessionVM.Odometer = _numOdometer.Value;
            _sessionVM.StartTime = Combine(_dateStart.Value, _timeStart.Value);
            _sessionVM.EndTime = Combine(_dateEnd.Value, _timeEnd.Value);
            _sessionVM.StartSoc = _numStartSoc.Value;
            _sessionVM.EndSoc = _numEndSoc.Value;
            _sessionVM.IsHomeCharging = _checkHomeCharger.Checked;
            _sessionVM.KwhAdded = _numKwhAdded.Value;
            _sessionVM.SessionCost = _numSessionCost.Value;
            _sessionVM.GridKwh = ParseDecimal(_textGridKwh.Text);
            _sessionVM.SolarKwh = ParseDecimal(_textSolarKwh.Text);
            _sessionVM.BatteryKwh = ParseDecimal(_textBatteryKwh.Text);
        }

        private void OnClickCancel(object sender, EventArgs e)
        {
            SavedSession = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private static DateTime Combine(DateTime date, DateTime time) =>
            date.Date + time.TimeOfDay;

        private static decimal ParseDecimal(string? text) =>
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
                ? value
                : 0m;

        private static decimal ClampToNumeric(NumericUpDown control, decimal value)
        {
            if (value < control.Minimum) return control.Minimum;
            if (value > control.Maximum) return control.Maximum;
            return value;
        }

        private static DateTime ClampToPicker(DateTime value)
        {
            if (value < DateTimePicker.MinimumDateTime)
                return DateTimePicker.MinimumDateTime;
            if (value > DateTimePicker.MaximumDateTime)
                return DateTimePicker.MaximumDateTime;
            return value;
        }
    }
}
