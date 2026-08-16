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

        public EvChargeSessionOrchestrator(
            IVehicleDataStore vehicleStore,
            IEnergyDataStore energyStore,
            IFinanceDataStore financeStore)
        {
            _vehicleStore = vehicleStore;
            _energyStore = energyStore;
            _financeStore = financeStore;
        }

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
                        if (home.Attribution is null || home.Billing is null)
                        {
                            CompleteHomeSession(home); 
                        }
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
            if (existingAttrib is not null)
            {
                session.Attribution = existingAttrib;
            }
            else
            {
                ComputeAndStoreAttribution(session);
            }

            // IMPORTANT:
            // Re-check attribution AFTER attempting to compute it.
            if (session.Attribution is not null)
            {
                // 2. Load existing billing (if any)
                var existingBilling = _vehicleStore.GetHomeEvChargeBilling(session.Id);
                if (existingBilling is not null)
                {
                    session.Billing = existingBilling;
                }
                else if (session.Attribution is not null)
                {
                    ComputeAndStoreBilling(session);
                }
            }
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
            var bill = _financeStore.GetElectricUtilityBillForDate(session.StartTime);
            if (bill == null)
                return;

            // EV-specific cost attribution:
            // ✔ EnergyChargeRate applies to GridKwh
            // ✔ DeliveryCharge is proportional to usage
            // ✘ BaseCharge excluded
            // ✘ ExportCredit excluded

            var gridKwh = session.Attribution!.GridKwh;

            var energyCost = gridKwh * bill.EnergyChargeRate;

            var deliveryCost =
                bill.KwhUsage > 0
                    ? gridKwh * (bill.DeliveryCharge / bill.KwhUsage)
                    : 0m;

            var billing = new HomeEvChargeBilling
            {
                BillingCycleId = bill.Id,
                EnergyChargeRate = bill.EnergyChargeRate,
                DeliveryChargeRate = bill.KwhUsage > 0
                    ? bill.DeliveryCharge / bill.KwhUsage
                    : 0m,
                SessionCost = energyCost + deliveryCost
            };

            session.Billing = billing;
            _vehicleStore.UpsertHomeEvChargeBilling(session.Id, billing);
        }
    }
}
