using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class EvChargeSessionViewModel
    {
        private readonly VehicleEv _vehicle;

        private EvChargeSession _session;

        public EvChargeSessionViewModel(
            VehicleEv vehicle,
            EvChargeSession? existingSession = null)
        {
            _session = existingSession ?? throw new ArgumentNullException(nameof(existingSession));
            _vehicle = vehicle;
        }

        // ---------------------------------------------------------
        // Properties exposed to the form
        // ---------------------------------------------------------
        public Guid SessionId => _session.Id;

        public decimal LastOdometer { get => _session.LastOdometer; set => _session.LastOdometer = value; }
        public decimal LastSoc { get => _session.LastSoc; set => _session.LastSoc = value; }

        public decimal Odometer { get => _session.OdometerMiles; set => _session.OdometerMiles = value; }
        public DateTime StartTime { get => _session.StartTime; set => _session.StartTime = value; }
        public DateTime EndTime { get => _session.EndTime; set => _session.EndTime = value; }

        public decimal StartSoc { get => _session.StartSoc; set => _session.StartSoc = value; }
        public decimal EndSoc { get => _session.EndSoc; set => _session.EndSoc = value; }

        public bool IsHomeCharge { get => _session.IsHomeCharge; set => _session.IsHomeCharge = value; }

        public decimal KwhAdded { get => _session.KwhAdded; set => _session.KwhAdded = value; }
        public decimal BatteryKwhAdded { get => _session.BatteryKwhAdded; set => _session.BatteryKwhAdded = value; }
        public decimal SessionCost { get => _session.SessionCost; set => _session.SessionCost = value; }

        public decimal GridKwh { get => _session.GridKwh; set => _session.GridKwh = value; }
        public decimal SolarKwh { get => _session.SolarKwh; set => _session.SolarKwh = value; }
        public decimal BatteryKwh { get => _session.BatteryKwh; set => _session.BatteryKwh = value; }

        // ---------------------------------------------------------
        // Derived values
        // ---------------------------------------------------------
        public decimal MilesUsed => (Odometer > LastOdometer) ? (Odometer - LastOdometer) : 0;
        public decimal SocUsed => (LastSoc > StartSoc) ? (LastSoc - StartSoc) : 0;
        public decimal SocAdded => (EndSoc > StartSoc) ? (EndSoc - StartSoc) : 0;
        public decimal ChargeLossKwh =>
            (BatteryKwhAdded > 0 && KwhAdded > 0)
                ? KwhAdded - BatteryKwhAdded
                : 0;
        public decimal ChargeEfficiency =>
            (BatteryKwhAdded > 0 && KwhAdded > 0)
                ? BatteryKwhAdded / KwhAdded
                : 0;

        public decimal KwhUsed => SocUsed * _vehicle.BatteryCapacityKwh / 100;
        public decimal WhPerMile => MilesUsed > 0 ? (KwhUsed * 1000 / MilesUsed) : 0;
        public decimal Mpge => WhPerMile > 0 ? (33700 / WhPerMile) : 0;
        public decimal CostPerMile => (MilesUsed > 0 && SessionCost > 0) ? (SessionCost / MilesUsed) : 0;
    }
}
