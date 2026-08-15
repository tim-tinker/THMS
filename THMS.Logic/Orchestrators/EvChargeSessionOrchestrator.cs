using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Energy;

namespace THMS.Logic.Orchestrators
{
    public class EvChargeSessionOrchestrator : BaseOrchestrator
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IEnergyDataStore _energyStore;
        //private readonly IUtilityBillingStore _billingStore;

        public Guid VehicleId { get; set; }

        public EvChargeSessionOrchestrator(
            IVehicleDataStore vehicleStore,
            IEnergyDataStore energyStore)
            //IUtilityBillingStore billingStore)
        {
            _vehicleStore = vehicleStore;
            _energyStore = energyStore;
            //_billingStore = billingStore;
        }

        // ---------------------------------------------------------
        // LATEST SESSION
        // ---------------------------------------------------------
        public BaseEvChargeSession? GetLastSession()
        {
            // If a specific vehicle is set, use that; otherwise, pick the latest across all.
            if (VehicleId != Guid.Empty)
                return _vehicleStore.GetLatestBaseEvChargeSession(VehicleId);

            var latest = _vehicleStore.GetAllVehicles()
                .Select(v => _vehicleStore.GetLatestBaseEvChargeSession(v.Id))
                .Where(s => s != null)
                .OrderByDescending(s => s!.EndTime)
                .FirstOrDefault();

            return latest;
        }

        // ---------------------------------------------------------
        // SAVE NEW SESSION (raw from UI)
        // ---------------------------------------------------------
        public void Save(BaseEvChargeSession session)
        {
            // Save base
            _vehicleStore.UpsertBaseEvChargeSession(session);

            // Save subtype
            switch (session)
            {
                case CommercialEvChargeSession commercial:
                    _vehicleStore.UpsertCommercialEvChargeSession(commercial);
                    break;

                case HomeEvChargeSession home:
                    _vehicleStore.UpsertHomeEvChargeSession(home);
                    break;

                default:
                    throw new InvalidOperationException("Unknown EV charge session type.");
            }
        }

        public void Update(BaseEvChargeSession session)
        {
            Save(session);
        }

        // ---------------------------------------------------------
        // GET SESSIONS (period-based)
        // ---------------------------------------------------------
        public IEnumerable<BaseEvChargeSession> GetEvChargeSessions(string period)
        {
            var latest = GetLastSession();
            if (latest is null)
                return Array.Empty<BaseEvChargeSession>();

            var end = latest.EndTime;
            var start = GetStartDate(end, period);

            if (VehicleId != Guid.Empty)
                return GetAndCompleteSessions(VehicleId, start, end);

            return _vehicleStore.GetAllVehicles()
                .SelectMany(v => GetAndCompleteSessions(v.Id, start, end))
                .OrderBy(s => s.StartTime)
                .ToArray();
        }

        private IEnumerable<BaseEvChargeSession> GetAndCompleteSessions(Guid vehicleId, DateTime start, DateTime end)
        {
            var baseSessions = _vehicleStore.GetBaseEvChargeSessions(vehicleId, start, end);

            foreach (var baseSession in baseSessions)
            {
                switch (baseSession)
                {
                    case CommercialEvChargeSession commercial:
                        yield return commercial;
                        break;

                    case HomeEvChargeSession home:
                        CompleteHomeSession(home);
                        yield return home;
                        break;

                    default:
                        yield return baseSession;
                        break;
                }
            }
        }

        // ---------------------------------------------------------
        // COMPLETE HOME SESSION (incremental)
        // ---------------------------------------------------------
        private void CompleteHomeSession(HomeEvChargeSession session)
        {
            // 1. Load existing attribution (if any)
            var existingAttrib = _vehicleStore.GetHomeEvChargeAttribution(session.Id);
            if (existingAttrib != null)
            {
                session.Attribution = existingAttrib;
                return;
            }

            // 2. Compute attribution using your engine
            var engine = new HomeCircuitAttributionEngine(_energyStore);
            engine.Compute(session.StartTime, session.EndTime);

            if (engine.ResultCount == 0)
                return;

            var attrib = new HomeEvChargeAttribution
            {
                GridKwh = engine.Results.Sum(r => r.GridWh) / 1000m,
                SolarKwh = engine.Results.Sum(r => r.SolarWh) / 1000m,
                BatteryKwh = engine.Results.Sum(r => r.BatteryWh) / 1000m
            };

            session.Attribution = attrib;

            // 3. Persist attribution
            _vehicleStore.UpsertHomeEvChargeAttribution(session.Id, attrib);
        }
    }
}
