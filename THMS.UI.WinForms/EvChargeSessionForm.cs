using System.Globalization;
using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Orchestrators;

namespace THMS.UI.WinForms
{
    public partial class EvChargeSessionForm : Form
    {
        protected VehicleEv Vehicle { get; set; }

        private readonly EvChargeSessionOrchestrator _orchestrator;
        private bool _loadingControls;

        /// <summary>Session written on Save; null if the dialog was cancelled.</summary>
        public BaseEvChargeSession? SavedSession { get; private set; }

        public EvChargeSessionForm()
        {
            InitializeComponent();
        }

        public EvChargeSessionForm(IVehicleDataStore vehicleStore, VehicleEv vehicle, IEnergyDataStore energyStore, IFinanceDataStore financeStore)
            : this()
        {
            Vehicle = vehicle;
            _orchestrator = new EvChargeSessionOrchestrator(vehicleStore, energyStore, financeStore) { VehicleId = vehicle.Id };
        }

        private void OnLoadForm(object sender, EventArgs e)
        {
            Text = $"EV Charge Session for {Vehicle.Name} ({Vehicle.Year} {Vehicle.Make} {Vehicle.Model})";
            var lastSession = _orchestrator.GetLastSession();
            if (lastSession is null) return;

            _numLastOdometer.Value = lastSession.OdometerMiles;
            _numLastSoc.Value = lastSession.EndSoc;
            _numOdometer.Value = lastSession.OdometerMiles;
            _numStartSoc.Value = lastSession.EndSoc;
            _dateStart.Value = DateTime.Now;
            _timeStart.Value = DateTime.Now;
            _dateEnd.Value = DateTime.Now;
            _timeEnd.Value = DateTime.Now;
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

        private void OnClickSave(object sender, EventArgs e)
        {
            BaseEvChargeSession session;
            if (_checkHomeCharger.Checked)
            {
                session = new HomeEvChargeSession
                {
                    Id = Guid.NewGuid(),
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
                };
            }
            else
            {
                session = new CommercialEvChargeSession
                {
                    Id = Guid.NewGuid(),
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
                    KwhDrawn = _numKwhAdded.Value,
                    SessionCost = _numSessionCost.Value
                };
            }

            if (ValidateSession(session))
            {
                _orchestrator.Save(session);
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
    }
}
