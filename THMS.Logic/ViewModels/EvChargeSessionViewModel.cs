using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels.Transportation
{
    public class EvChargeSessionViewModel
    {
        private readonly VehicleEv _vehicle;

        private BaseEvChargeSession _session;
        private HomeEvChargeSession _homeSession;
        private CommercialEvChargeSession _commercialSession;

        public EvChargeSessionViewModel(
            VehicleEv vehicle,
            BaseEvChargeSession? existingSession = null)
        {
            _session = existingSession ?? throw new ArgumentNullException(nameof(existingSession));
            _homeSession = existingSession as HomeEvChargeSession;
            _commercialSession = existingSession as CommercialEvChargeSession;
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


        // ---------------------------------------------------------
        // Derived values
        // ---------------------------------------------------------
        public decimal MilesUsed => (Odometer > LastOdometer) ? (Odometer - LastOdometer) : 0;
        public decimal SocUsed => (LastSoc > StartSoc) ? (LastSoc - StartSoc) : 0;
        public decimal SocAdded => (EndSoc > StartSoc) ? (EndSoc - StartSoc) : 0;

        public decimal KwhUsed => SocUsed * _vehicle.BatteryCapacityKwh / 100;
        public decimal WhPerMile => MilesUsed > 0 ? (KwhUsed * 1000 / MilesUsed) : 0;
        public decimal Mpge => WhPerMile > 0 ? (33700 / WhPerMile) : 0;
    }
}
