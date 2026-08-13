using System.Globalization;
using THMS.Domain.Transportation;
using THMS.Logic.ViewModels.Transportation;

namespace THMS.UI.WinForms
{
    public partial class EvChargeSessionForm : Form
    {
        private VehicleEv _vehicle = null!;
        protected VehicleEv Vehicle
        {
            get => _vehicle; set => _vehicle = value;
        }

        private readonly EvChargeSessionViewModel _sessionVM = null!;
        private bool _loadingControls;

        /// <summary>Session written on Save; null if the dialog was cancelled.</summary>
        public EvChargeSession? SavedSession { get; private set; }

        protected decimal BatteryCapacityKwh => _vehicle?.BatteryCapacityKwh ?? 0;

        public EvChargeSessionForm()
        {
            InitializeComponent();
        }

        public EvChargeSessionForm(Guid vehicleId, VehicleDetailViewModel vm, decimal lastOdometer, decimal lastSoc, EvChargeSession? existingSession = null)
            : this()
        {
            Vehicle = vm.Vehicle as VehicleEv ?? throw new ArgumentException("Vehicle is not an EV.", nameof(vm));
            _sessionVM = new EvChargeSessionViewModel(vm.VehicleStore, vm.EnergyStore, vehicleId, Vehicle, lastOdometer, lastSoc, existingSession);
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            Text = $"EV Charge Session for {Vehicle.Name} ({Vehicle.Year} {Vehicle.Make} {Vehicle.Model})";
            LoadControlsFromViewModel();
        }

        private void LoadControlsFromViewModel()
        {
            _loadingControls = true;
            try
            {
                _numLastOdometer.Value = _sessionVM.LastOdometer;
                _numLastSoc.Value = _sessionVM.LastSoc;
                _numOdometer.Value = ClampToNumeric(_numOdometer, _sessionVM.Odometer);
                _dateStart.Value = ClampToPicker(_sessionVM.StartTime);
                _timeStart.Value = ClampToPicker(_sessionVM.StartTime);
                _dateEnd.Value = ClampToPicker(_sessionVM.EndTime);
                _timeEnd.Value = ClampToPicker(_sessionVM.EndTime);

                _numStartSoc.Value = ClampToNumeric(_numStartSoc, _sessionVM.StartSoc);
                _numEndSoc.Value = ClampToNumeric(_numEndSoc, _sessionVM.EndSoc);
                _numBatteryKwhAdded.Value = ClampToNumeric(_numBatteryKwhAdded, _sessionVM.BatteryKwhAdded);

                _checkHomeCharger.Checked = _sessionVM.IsHomeCharge;
                _numKwhAdded.Value = ClampToNumeric(_numKwhAdded, _sessionVM.KwhAdded);
                _numSessionCost.Value = ClampToNumeric(_numSessionCost, _sessionVM.SessionCost);

                EnableHomeControls(_sessionVM.IsHomeCharge);
            }
            finally
            {
                _loadingControls = false;
            }
        }

        private void OnCheckedChangedHomeCharger(object sender, EventArgs e)
        {
            if (_loadingControls)
                return;

            // Persist the checkbox to the shared VM immediately; otherwise a later
            // LoadControlsFromViewModel (e.g. after circuit import) restores false.
            _sessionVM.IsHomeCharge = _checkHomeCharger.Checked;
            EnableHomeControls(_checkHomeCharger.Checked);
        }

        private void EnableHomeControls(bool enable)
        {
            _numKwhAdded.Enabled = !enable;
            _numSessionCost.Enabled = !enable;
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
            _sessionVM.LastOdometer = _numLastOdometer.Value;
            _sessionVM.LastSoc = _numLastSoc.Value;
            _sessionVM.Odometer = _numOdometer.Value;
            _sessionVM.StartTime = Combine(_dateStart.Value, _timeStart.Value);
            _sessionVM.EndTime = Combine(_dateEnd.Value, _timeEnd.Value);
            _sessionVM.StartSoc = _numStartSoc.Value;
            _sessionVM.EndSoc = _numEndSoc.Value;
            _sessionVM.BatteryKwhAdded = _numBatteryKwhAdded.Value;
            _sessionVM.IsHomeCharge = _checkHomeCharger.Checked;
            _sessionVM.KwhAdded = _numKwhAdded.Value;
            _sessionVM.SessionCost = _numSessionCost.Value;
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

        private void OnClickLoadCircuitData(object sender, EventArgs e)
        {
            // Flush all current control values into the VM before the child form
            // runs (BatteryKwhAdded / Home Charger may not have been applied yet).
            ApplyControlsToViewModel();

            using var form = new HomeCircuitDataForm(_sessionVM);

            form.ShowDialog();

            // Circuit import updates the shared session VM; push those values
            // into the controls. Do not call UpdateConsumption alone — it applies
            // stale control values back onto the VM first.
            LoadControlsFromViewModel();
        }
    }
}
