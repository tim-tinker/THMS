using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Energy;

namespace THMS.Logic.Orchestrators
{
    public class EvChargeSessionOrchestrator : BaseOrchestrator
    {
        private readonly IVehicleDataStore _vehicleStore;
        private readonly IEnergyDataStore _energyStore;
        private readonly IFinanceDataStore _financeStore;

        public Guid VehicleId { get; set; }

        public EvChargeSessionOrchestrator()
            : this(
                new DataStoreFactory().GetVehicleStore(),
                new DataStoreFactory().GetEnergyStore(),
                new DataStoreFactory().GetFinanceStore())
        {
        }

        public EvChargeSessionOrchestrator(
            IVehicleDataStore vehicleStore,
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore)
        {
            _vehicleStore = vehicleStore;
            _energyStore = energyStore;
            _financeStore = financeStore;
        }

        public IEnumerable<VehicleEv> GetEvVehicles() =>
            _vehicleStore.GetAllVehicles().OfType<VehicleEv>();

        // ---------------------------------------------------------
        // LATEST SESSION
        // ---------------------------------------------------------
        public BaseEvChargeSession? GetLastSession()
        {
            // If a specific vehicle is set, use that; otherwise, pick the latest across all.
            if (VehicleId != Guid.Empty)
                return _vehicleStore.GetLatestBaseEvChargeSession(VehicleId);

            return _vehicleStore.GetLatestBaseEvChargeSession();
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

            IEnumerable<BaseEvChargeSession> sessions = VehicleId != Guid.Empty
                ? GetAndCompleteSessions(VehicleId, start, end)
                : _vehicleStore.GetAllVehicles()
                    .SelectMany(v => GetAndCompleteSessions(v.Id, start, end));

            return sessions.OrderByDescending(s => s.StartTime).ToArray();
        }

        private IEnumerable<BaseEvChargeSession> GetAndCompleteSessions(Guid vehicleId, DateTime start, DateTime end)
        {
            var baseSessions = _vehicleStore.GetBaseEvChargeSessions(vehicleId, start, end);

            foreach (var baseSession in baseSessions)
            {
                if (_vehicleStore.GetCommercialEvChargeSession(baseSession.Id) is { } commercial)
                {
                    yield return commercial;
                    continue;
                }

                var home = baseSession as HomeEvChargeSession
                    ?? _vehicleStore.GetHomeEvChargeSession(baseSession.Id);
                if (home is not null)
                {
                    CompleteHomeSession(home);
                    yield return home;
                    continue;
                }

                yield return baseSession;
            }
        }

        // ---------------------------------------------------------
        // COMPLETE HOME SESSION (incremental)
        // ---------------------------------------------------------
        private void CompleteHomeSession(HomeEvChargeSession session)
        {
            // 1. Load existing attribution (if any)
            var existingAttrib = _vehicleStore.GetHomeEvChargeAttribution(session.Id);
            if (existingAttrib is not null)
            {
                session.Attribution = existingAttrib;
            }
            else
            {
                ComputeAndStoreAttribution(session);
            }

            if (session.Attribution is null)
                return;

            var existingBilling = _vehicleStore.GetHomeEvChargeBilling(session.Id);
            if (existingBilling is not null && existingBilling.SessionCost != 0)
                session.Billing = existingBilling;
            else
                ComputeAndStoreBilling(session);
        }

        // ---------------------------------------------------------
        // ENERGY ATTRIBUTION
        // ---------------------------------------------------------
        private void ComputeAndStoreAttribution(HomeEvChargeSession session)
        {
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
            _vehicleStore.UpsertHomeEvChargeAttribution(session.Id, attrib);
        }

        // ---------------------------------------------------------
        // BILLING ATTRIBUTION
        // ---------------------------------------------------------
        private void ComputeAndStoreBilling(HomeEvChargeSession session)
        {
            var contract = _financeStore.GetElectricContractForDate(session.StartTime.Date);
            if (contract == null)
                return;

            var gridKwh = session.Attribution!.GridKwh;

            var energyCost = gridKwh * contract.EnergyChargeRate;
            var deliveryCost = gridKwh * contract.DeliveryChargeRate;

            var sessionCost = energyCost + deliveryCost;

            var billing = new HomeEvChargeBilling
            {
                SessionCost = sessionCost
            };

            session.Billing = billing;
            _vehicleStore.UpsertHomeEvChargeBilling(session.Id, billing);
        }
    }
}
