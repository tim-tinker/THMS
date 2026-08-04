using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Transportation;

namespace THMS.Logic.ViewModels
{
    public class EvChargeSessionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IVehicleDataStore _vehicleStore;
        private readonly Guid _vehicleId;
        private readonly VehicleEv _vehicle;

        // For “previous session” context
        public decimal? LastOdometer { get; }
        public decimal? LastSoc { get; }

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
        public decimal? SessionCost { get; set; }      // manual (commercial) or computed (home)

        // Derived metrics
        public decimal MilesUsed => LastOdometer.HasValue && Odometer > LastOdometer.Value
            ? Odometer - LastOdometer.Value
            : 0;

        public decimal SocUsed => LastSoc.HasValue && StartSoc > LastSoc.Value
            ? StartSoc - LastSoc.Value
            : 0;

        public decimal SocAdded => EndSoc > StartSoc
            ? EndSoc - StartSoc
            : 0;

        public decimal BatteryCapacityKwh => _vehicle.BatteryCapacityKwh;

        public decimal KwhUsed => SocUsed * BatteryCapacityKwh / 100m;

        public decimal WhPerMile => MilesUsed > 0 ? KwhUsed * 1000m / MilesUsed : 0;

        private const decimal WhPerGasGallon = 33700m;
        public decimal Mpge => WhPerMile > 0 ? WhPerGasGallon / WhPerMile : 0;

        // EV-specific energy attribution
        public decimal GridKwh { get; set; }
        public decimal SolarKwh { get; set; }
        public decimal BatteryKwh { get; set; }

        public decimal CostPerMile => MilesUsed > 0 && SessionCost.HasValue
            ? SessionCost.Value / MilesUsed
            : 0;

        public EvChargeSessionViewModel(
            IVehicleDataStore vehicleStore,
            Guid vehicleId,
            VehicleEv vehicle,
            decimal? lastOdometer,
            decimal? lastSoc,
            EvChargingSession? existingSession = null)
        {
            _vehicleStore = vehicleStore;
            _vehicleId = vehicleId;
            _vehicle = vehicle;
            LastOdometer = lastOdometer;
            LastSoc = lastSoc;

            if (existingSession != null)
                LoadFromDomain(existingSession);
            else
                InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            var now = DateTime.Now;
            StartTime = now;
            EndTime = now;
            Odometer = LastOdometer ?? 0;
            StartSoc = LastSoc ?? 0;
            EndSoc = StartSoc;
            IsHomeCharging = true;
            KwhAdded = 0;
            SessionCost = null;
        }

        private void LoadFromDomain(EvChargingSession s)
        {
            SessionId = s.Id;
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

        public void Save()
        {
            if (IsNew)
            {
                var session = new EvChargingSession
                {
                    Id = Guid.NewGuid(),
                    VehicleId = _vehicleId,
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
                var session = _vehicleStore.GetEvChargingSession(SessionId!.Value);
                if (session == null) return;

                session.OdometerMiles = Odometer;
                session.StartTime = StartTime;
                session.EndTime = EndTime;
                session.StartSoc = StartSoc;
                session.EndSoc = EndSoc;
                session.IsHomeCharging = IsHomeCharging;
                session.KwhAdded = KwhAdded;
                session.ChargingCost = SessionCost;
                session.GridKwh = GridKwh;
                session.SolarKwh = SolarKwh;
                session.BatteryKwh = BatteryKwh;

                _vehicleStore.UpdateEvChargingSession(session);
            }
        }

        private void OnChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
