using System.Globalization;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;
using THMS.Logic.Transportation;

namespace THMS.UI.WinForms
{
    public partial class EvChargeSessionForm : Form
    {
        protected VehicleEv Vehicle { get; set; }

        private readonly EvChargeSessionOrchestrator _orchestrator;
        private readonly BaseEvChargeSession? _existing;
        private bool _loadingControls;
        private bool _applyingCalculated;

        /// <summary>Session written on Save; null if the dialog was cancelled.</summary>
        public BaseEvChargeSession? SavedSession { get; private set; }

        public EvChargeSessionForm()
        {
            InitializeComponent();
        }

        public EvChargeSessionForm(VehicleEv vehicle)
            : this()
        {
            Vehicle = vehicle;
            _orchestrator = new EvChargeSessionOrchestrator { VehicleId = vehicle.Id };
        }

        public EvChargeSessionForm(VehicleEv vehicle, BaseEvChargeSession session)
            : this(vehicle)
        {
            _existing = session;
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            Text = $"EV Charge Session for {Vehicle.Name} ({Vehicle.Year} {Vehicle.Make} {Vehicle.Model})";
            if (_existing is not null)
            {
                LoadExistingSession(_existing);
                return;
            }

            _loadingControls = true;
            try
            {
                var lastSession = _orchestrator.GetLastSession();
                if (lastSession is not null)
                {
                    _numLastOdometer.Value = lastSession.OdometerMiles;
                    _numLastSoc.Value = lastSession.EndSoc;
                    _numOdometer.Value = lastSession.OdometerMiles;
                    _numStartSoc.Value = lastSession.EndSoc;
                }

                _dateStart.Value = DateTime.Now;
                _timeStart.Value = DateTime.Now;
                _dateEnd.Value = DateTime.Now;
                _timeEnd.Value = DateTime.Now;
                _checkKwhCalculated.Checked = Vehicle.ChargeEnergy is not null;
            }
            finally
            {
                _loadingControls = false;
            }

            if (_checkKwhCalculated.Checked)
                ApplyCalculatedKwh();
        }

        private void OnCheckedChangedHomeCharger(object sender, EventArgs e)
        {
            if (_loadingControls)
                return;

            // Persist the checkbox to the shared VM immediately; otherwise a later
            // LoadControlsFromViewModel (e.g. after circuit import) restores false.
            EnableHomeControls(_checkHomeCharger.Checked);
        }

        private void EnableHomeControls(bool enable)
        {
            _numKwhAdded.Enabled = !enable;
            _numSessionCost.Enabled = !enable;
        }

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

        private static DateTime Combine(DateTime date, DateTime time) => date.Date + time.TimeOfDay;

        private void OnClickCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void LoadExistingSession(BaseEvChargeSession session)
        {
            _loadingControls = true;
            try
            {
                _checkHomeCharger.Checked = session.IsHomeCharge;
                EnableHomeControls(session.IsHomeCharge);
                _numLastOdometer.Value = ClampToNumeric(_numLastOdometer, session.LastOdometer);
                _numLastSoc.Value = ClampToNumeric(_numLastSoc, session.LastSoc);
                _numOdometer.Value = ClampToNumeric(_numOdometer, session.OdometerMiles);
                _numStartSoc.Value = ClampToNumeric(_numStartSoc, session.StartSoc);
                _numEndSoc.Value = ClampToNumeric(_numEndSoc, session.EndSoc);
                _numBatteryKwhAdded.Value = ClampToNumeric(_numBatteryKwhAdded, session.KwhAdded);
                _checkKwhCalculated.Checked = session.KwhAddedIsCalculated;
                _numKwhAdded.Value = ClampToNumeric(_numKwhAdded, session.KwhDrawn ?? 0);
                _numSessionCost.Value = ClampToNumeric(_numSessionCost, session.SessionCost);
                var start = ClampToPicker(session.StartTime);
                var end = ClampToPicker(session.EndTime);
                _dateStart.Value = start;
                _timeStart.Value = start;
                _dateEnd.Value = end;
                _timeEnd.Value = end;
            }
            finally
            {
                _loadingControls = false;
            }
        }

        private void OnClickSave(object sender, EventArgs e)
        {
            var sessionId = _existing?.Id ?? Guid.NewGuid();
            BaseEvChargeSession session;
            if (_checkHomeCharger.Checked)
            {
                session = new HomeEvChargeSession
                {
                    Id = sessionId,
                    VehicleId = Vehicle.Id,
                    VehicleName = Vehicle.Name,
                    LastOdometer = _numLastOdometer.Value,
                    LastSoc = _numLastSoc.Value,
                    OdometerMiles = _numOdometer.Value,
                    StartTime = Combine(_dateStart.Value, _timeStart.Value),
                    EndTime = Combine(_dateEnd.Value, _timeEnd.Value),
                    StartSoc = _numStartSoc.Value,
                    EndSoc = _numEndSoc.Value,
                    KwhAdded = _numBatteryKwhAdded.Value,
                    KwhAddedIsCalculated = _checkKwhCalculated.Checked,
                    KwhDrawn = _existing?.KwhDrawn,
                };
            }
            else
            {
                session = new CommercialEvChargeSession
                {
                    Id = sessionId,
                    VehicleId = Vehicle.Id,
                    VehicleName = Vehicle.Name,
                    LastOdometer = _numLastOdometer.Value,
                    LastSoc = _numLastSoc.Value,
                    OdometerMiles = _numOdometer.Value,
                    StartTime = Combine(_dateStart.Value, _timeStart.Value),
                    EndTime = Combine(_dateEnd.Value, _timeEnd.Value),
                    StartSoc = _numStartSoc.Value,
                    EndSoc = _numEndSoc.Value,
                    KwhAdded = _numBatteryKwhAdded.Value,
                    KwhAddedIsCalculated = _checkKwhCalculated.Checked,
                    KwhDrawn = _numKwhAdded.Value,
                    SessionCost = _numSessionCost.Value
                };
            }

            if (ValidateSession(session))
            {
                SavedSession = session;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private bool ValidateSession(BaseEvChargeSession session)
        {
            var isValid = true;
            if (session.EndTime < session.StartTime)
            {
                MessageBox.Show("End time must be on or after start time.");
                isValid = false;
            }
            else if (session.LastOdometer > session.OdometerMiles)
            {
                MessageBox.Show("Odometer must not be less than last odometer.");
                isValid = false;
            }

            return isValid;
        }

        private void OnSocChanged(object? sender, EventArgs e)
        {
            if (_loadingControls || !_checkKwhCalculated.Checked)
                return;

            ApplyCalculatedKwh();
        }

        private void OnBatteryKwhChanged(object? sender, EventArgs e)
        {
            if (_loadingControls || _applyingCalculated)
                return;

            _loadingControls = true;
            try
            {
                _checkKwhCalculated.Checked = false;
            }
            finally
            {
                _loadingControls = false;
            }
        }

        private void OnKwhCalculatedChanged(object? sender, EventArgs e)
        {
            if (_loadingControls || !_checkKwhCalculated.Checked)
                return;

            // The click that checks the box also validates the kWh field. Applying
            // during that click is overwritten by the field's current text, so wait
            // until the click has finished.
            if (IsHandleCreated)
                BeginInvoke(ApplyCheckedKwh);
            else
                ApplyCheckedKwh();
        }

        private void ApplyCheckedKwh()
        {
            if (IsDisposed || !_checkKwhCalculated.Checked)
                return;

            EnsureChargeEnergy();
            if (BatterySocEnergy.EstimateKwh(_numStartSoc.Value, _numEndSoc.Value, Vehicle.ChargeEnergy) is null)
            {
                _loadingControls = true;
                try
                {
                    _checkKwhCalculated.Checked = false;
                }
                finally
                {
                    _loadingControls = false;
                }

                MessageBox.Show(
                    this,
                    Vehicle.ChargeEnergy is null
                        ? "No measured battery kWh values are available to calculate from."
                        : "End SOC must be above start SOC to calculate battery kWh.",
                    "Battery kWh",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ApplyCalculatedKwh();
        }

        private void EnsureChargeEnergy()
        {
            if (Vehicle.ChargeEnergy is not null)
                return;

            var fit = _orchestrator.RecalculateChargeEnergy(Vehicle.Id);
            if (fit is not null)
                Vehicle.ChargeEnergy = fit.Value.Coefficients;
        }

        private void ApplyCalculatedKwh()
        {
            var estimate = BatterySocEnergy.EstimateKwh(
                _numStartSoc.Value,
                _numEndSoc.Value,
                Vehicle.ChargeEnergy);
            if (estimate is null)
                return;

            _applyingCalculated = true;
            _loadingControls = true;
            try
            {
                _numBatteryKwhAdded.Value = ClampToNumeric(_numBatteryKwhAdded, estimate.Value);
            }
            finally
            {
                _loadingControls = false;
                _applyingCalculated = false;
            }
        }
    }
}
