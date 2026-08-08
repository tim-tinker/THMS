using System.ComponentModel;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Transportation;
using THMS.Ingestion.Importers.Energy;
using THMS.Logic.Energy;

namespace THMS.Logic.ViewModels.Transportation
{
    public class EvChargeSessionViewModel
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IEnergyDataStore _energyStore;

        private readonly VehicleEv _vehicle;

        private EvChargeSession _session;

        public IEnergyDataStore EnergyStore => _energyStore;

        public EvChargeSessionViewModel(
            IVehicleDataStore vehicleStore,
            IEnergyDataStore energyStore,
            Guid vehicleId,
            VehicleEv vehicle,
            decimal lastOdometer,
            decimal lastSoc,
            EvChargeSession? existingSession = null)
        {
            _vehicleStore = vehicleStore;
            _energyStore = energyStore;
            _vehicle = vehicle;

            if (existingSession != null)
            {
                _session = existingSession;
            }
            else
            {
                _session = new EvChargeSession
                {
                    Id = Guid.NewGuid(),
                    VehicleId = vehicleId,
                    LastOdometer = lastOdometer,
                    LastSoc = lastSoc,
                    OdometerMiles = lastOdometer,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    StartSoc = lastSoc,
                    EndSoc = lastSoc,
                    BatteryKwhAdded = 0,
                    IsHomeCharge = false
                };
            }

            // Load circuit segments if editing an existing session
            LoadCircuitSegments();
            RecalculateFromSegments();
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

        // ---------------------------------------------------------
        // Circuit segment handling
        // ---------------------------------------------------------
        public BindingList<EvCircuitSegment> CircuitSegments { get; } = new();

        public void LoadCircuitData(string filename)
        {
            var importer = new HomeEvCircuitImporter(EnergyStore);
            var readings = importer.Import(filename);
            LoadCircuitReadings(readings);
            CalculateEvAttribution(importer.StartDate, importer.EndDate);
        }

        public void LoadCircuitReadings(IEnumerable<EvCircuitReading> readings)
        {
            // Convert raw readings → segments
            var segments = ConvertReadingsToSegments(readings);

            // Store segments
            SaveCircuitSegments(segments);

            // Update session totals
            RecalculateFromSegments();

            // Save updated session
            Save();
        }

        private void CalculateEvAttribution(DateTime start, DateTime end)
        {
            if (_energyStore.GetSolarVendorIntervals(start, end).Any())
            {
                var engine = new EvAttributionEngine(_energyStore);
                engine.Compute(start, end);
            }
        }

        private List<EvCircuitSegment> ConvertReadingsToSegments(IEnumerable<EvCircuitReading> readings)
        {
            var rawList = (from rawData in readings
                           where rawData.Timestamp >= StartTime.AddMinutes(-10) && rawData.Timestamp <= EndTime.AddMinutes(10)
                           orderby rawData.Timestamp
                           select rawData).ToList();
            var segments = new List<EvCircuitSegment>();

            for (int i = 0; i < rawList.Count; i++)
            {
                var r = rawList[i];
                var next = (i < rawList.Count - 1) ? rawList[i + 1] : null;

                int durationSeconds = next != null
                    ? (int)(next.Timestamp - r.Timestamp).TotalSeconds
                    : 0;

                segments.Add(new EvCircuitSegment
                {
                    Id = Guid.NewGuid(),
                    SessionId = SessionId,
                    Timestamp = r.Timestamp,
                    DurationSeconds = durationSeconds,
                    Kwh = r.KiloWattHours,
                    GridKwh = r.KiloWattHours,   // default attribution
                    SolarKwh = 0,
                    BatteryKwh = 0
                });
            }

            return segments;
        }

        private void LoadCircuitSegments()
        {
            ReplaceSegments(_energyStore.GetEvCircuitSegments(SessionId));
        }

        public void SaveCircuitSegments(IEnumerable<EvCircuitSegment> segments)
        {
            ReplaceSegments(segments);
            _energyStore.SaveEvCircuitSegments(SessionId, CircuitSegments);
            RecalculateFromSegments();
        }

        public void DeleteCircuitSegments()
        {
            CircuitSegments.Clear();
            _energyStore.DeleteEvCircuitSegments(SessionId);
            RecalculateFromSegments();
        }

        private void ReplaceSegments(IEnumerable<EvCircuitSegment> segments)
        {
            // Clear/Add keeps the same BindingList instance so a bound grid updates.
            CircuitSegments.Clear();
            foreach (var segment in segments)
            {
                CircuitSegments.Add(segment);
            }
        }

        // ---------------------------------------------------------
        // Recalculate session totals from segments
        // ---------------------------------------------------------
        public void RecalculateFromSegments()
        {
            if (CircuitSegments.Count == 0)
            {
                // No circuit data → KwhAdded stays manual
                return;
            }

            KwhAdded = CircuitSegments.Sum(s => s.Kwh);
            GridKwh = CircuitSegments.Sum(s => s.GridKwh);
            SolarKwh = CircuitSegments.Sum(s => s.SolarKwh);
            BatteryKwh = CircuitSegments.Sum(s => s.BatteryKwh);
        }

        // ---------------------------------------------------------
        // Save session
        // ---------------------------------------------------------
        public EvChargeSession Save()
        {
            if (_vehicleStore.GetEvChargeSession(_session.Id) == null)
            {
                _vehicleStore.AddEvChargeSession(_session);
            }
            else
            {
                _vehicleStore.UpdateEvChargeSession(_session);
            }

            return _session;
        }
    }
}
