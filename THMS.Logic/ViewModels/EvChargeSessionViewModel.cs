using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels
{
    public class EvChargeSessionViewModel
    {
        private const decimal WhPerGasGallon = 33700m;

        private readonly IVehicleDataStore _vehicleStore;
        private readonly Guid _vehicleId;
        private readonly VehicleEv _vehicle;

        public decimal BatteryCapacityKwh => _vehicle.BatteryCapacityKwh;

        // For “previous session” context
        public decimal LastOdometer { get; private set; }
        public decimal LastSoc { get; private set; }

        // Session identity
        public Guid? SessionId { get; private set; }
        public bool IsNew => SessionId == null;

        // Editable fields
        public decimal Odometer { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal StartSoc { get; set; }
        public decimal EndSoc { get; set; }
        public bool IsHomeCharging { get; set; }
        public decimal KwhAdded { get; set; }          // manual or from circuit file
        public decimal SessionCost { get; set; }       // manual (commercial) or computed (home)

        // Derived metrics
        public decimal MilesUsed => 0 > LastOdometer && Odometer > LastOdometer
            ? Odometer - LastOdometer
            : 0;

        public decimal SocUsed => 0 < LastSoc && StartSoc < LastSoc
            ? LastSoc - StartSoc
            : 0;

        public decimal KwhUsed => SocUsed * BatteryCapacityKwh / 100m;

        public decimal WhPerMile => MilesUsed > 0 ? KwhUsed * 1000m / MilesUsed : 0;

        public decimal Mpge => WhPerMile > 0 ? WhPerGasGallon / WhPerMile : 0;

        public decimal SocAdded => EndSoc > StartSoc
            ? EndSoc - StartSoc
            : 0;

        public decimal CostPerMile => 0 < SessionCost && 0 < KwhAdded && 0 < WhPerMile
            ? SessionCost / KwhAdded * WhPerMile / 1000
            : 0;

        // EV-specific energy attribution
        public decimal GridKwh { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryKwh { get; set; }

        public EvChargeSessionViewModel(
            IVehicleDataStore vehicleStore,
            Guid vehicleId,
            VehicleEv vehicle,
            decimal lastOdometer,
            decimal lastSoc,
            EvChargingSession? existingSession = null)
        {
            _vehicleStore = vehicleStore;
            _vehicleId = vehicleId;
            _vehicle = vehicle;

            if (existingSession is null)
            {
                InitializeDefaults(lastOdometer, lastSoc);
            }
            else
            {
                LoadFromDomain(existingSession);
            }
        }

        private void InitializeDefaults(decimal lastOdometer, decimal lastSoc)
        {
            var now = DateTime.Now;
            LastOdometer = lastOdometer;
            LastSoc = lastSoc;
            Odometer = LastOdometer;
            StartTime = now;
            EndTime = now;
            StartSoc = LastSoc;
            EndSoc = StartSoc;
            IsHomeCharging = false;
            KwhAdded = 0;
            SessionCost = 0;
        }

        private void LoadFromDomain(EvChargingSession s)
        {
            SessionId = s.Id;
            LastOdometer = s.LastOdometer;
            LastSoc = s.LastSoc;
            Odometer = s.OdometerMiles;
            StartTime = s.StartTime;
            EndTime = s.EndTime;
            StartSoc = s.StartSoc;
            EndSoc = s.EndSoc;
            IsHomeCharging = s.IsHomeCharging;
            KwhAdded = s.KwhAdded;
            SessionCost = s.ChargingCost;

            // Grid/Solar/Battery attribution would be loaded from energy analytics
            GridKwh = s.GridKwh;
            SolarKwh = s.SolarKwh;
            BatteryKwh = s.BatteryKwh;
        }

        public void ApplyCircuitReading(EvCircuitReadingSummary summary)
        {
            GridKwh = summary.GridKwh; 
            SolarKwh = summary.SolarKwh; 
            BatteryKwh = summary.BatteryKwh;
            KwhAdded = summary.TotalKwh;
        }

        public EvChargingSession Save()
        {
            EvChargingSession session;
            if (IsNew)
            {
                session = new EvChargingSession
                {
                    Id = Guid.NewGuid(),
                    VehicleId = _vehicleId,
                    LastOdometer = LastOdometer,
                    LastSoc = LastSoc,
                    OdometerMiles = Odometer,
                    StartTime = StartTime,
                    EndTime = EndTime,
                    StartSoc = StartSoc,
                    EndSoc = EndSoc,
                    IsHomeCharging = IsHomeCharging,
                    KwhAdded = KwhAdded,
                    ChargingCost = SessionCost,
                    GridKwh = GridKwh,
                    SolarKwh = SolarKwh,
                    BatteryKwh = BatteryKwh
                };

                _vehicleStore.AddEvChargingSession(session);
                SessionId = session.Id;
            }
            else
            {
                var existing = _vehicleStore.GetEvChargingSession(SessionId!.Value)
                    ?? throw new InvalidOperationException("Charging session was not found.");

                // user cannot edit last odometer or last soc, so we don't update those
                existing.OdometerMiles = Odometer;
                existing.StartTime = StartTime;
                existing.EndTime = EndTime;
                existing.StartSoc = StartSoc;
                existing.EndSoc = EndSoc;
                existing.IsHomeCharging = IsHomeCharging;
                existing.KwhAdded = KwhAdded;
                existing.ChargingCost = SessionCost;
                existing.GridKwh = GridKwh;
                existing.SolarKwh = SolarKwh;
                existing.BatteryKwh = BatteryKwh;

                _vehicleStore.UpdateEvChargingSession(existing);
                session = existing;
            }
            return session;
        }
    }
}
