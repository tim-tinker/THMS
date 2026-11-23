using THMS.Data.Stores;
using THMS.Domain.Transportation;
using THMS.Logic.Energy;
using THMS.Logic.Transportation;

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

            // Save subtype. Drop the other subtype so a corrected Home Charger
            // choice replaces the previous charge type instead of leaving both.
            switch (session)
            {
                case CommercialEvChargeSession commercial:
                    _vehicleStore.DeleteHomeEvChargeSession(commercial.Id);
                    _vehicleStore.UpsertCommercialEvChargeSession(commercial);
                    break;

                case HomeEvChargeSession home:
                    _vehicleStore.DeleteCommercialEvChargeSession(home.Id);
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

        public void Delete(Guid sessionId)
        {
            _vehicleStore.DeleteCommercialEvChargeSession(sessionId);
            _vehicleStore.DeleteHomeEvChargeSession(sessionId);
            _vehicleStore.DeleteBaseEvChargeSession(sessionId);
        }

        /// <summary>
        /// Fits E(s) = a1·s + a2·s² + a3·s³ from sessions whose battery kWh was measured.
        /// Calculated and zero kWh values are ignored. Returns null and leaves the stored
        /// coefficients unchanged when no measured session has a SOC change.
        /// </summary>
        public SocEnergyFit? RecalculateChargeEnergy(Guid vehicleId)
        {
            if (_vehicleStore.GetVehicle(vehicleId) is not VehicleEv vehicle)
                return null;

            var sessions = _vehicleStore.GetBaseEvChargeSessions(vehicleId, DateTime.MinValue, DateTime.MaxValue);
            var fit = BatterySocEnergy.Fit(sessions);
            if (fit is not SocEnergyFit value)
                return null;

            vehicle.ChargeEnergy = value.Coefficients;
            _vehicleStore.UpsertVehicle(vehicle);
            var (refreshed, unchanged) = RefreshCalculatedBatteryCharges(sessions, value.Coefficients);
            return value with
            {
                RefreshedCalculatedSessions = refreshed,
                UnchangedCalculatedSessions = unchanged
            };
        }

        private (int Refreshed, int Unchanged) RefreshCalculatedBatteryCharges(
            IEnumerable<BaseEvChargeSession> sessions,
            SocEnergyCoefficients coefficients)
        {
            var refreshed = 0;
            var unchanged = 0;
            foreach (var session in sessions)
            {
                if (!session.KwhAddedIsCalculated)
                    continue;

                var estimate = BatterySocEnergy.EstimateKwh(session.StartSoc, session.EndSoc, coefficients);
                if (estimate is null)
                {
                    unchanged++;
                    continue;
                }

                if (estimate.Value == session.KwhAdded)
                    continue;

                _vehicleStore.UpsertEvBatteryCharge(session.Id, new EvBatteryCharge
                {
                    KwhAdded = estimate.Value,
                    IsCalculated = true
                });
                refreshed++;
            }

            return (refreshed, unchanged);
        }

        public IReadOnlyList<(VehicleEv Vehicle, SocEnergyFit Fit)> RecalculateChargeEnergy()
        {
            var results = new List<(VehicleEv Vehicle, SocEnergyFit Fit)>();
            foreach (var vehicle in GetEvVehicles())
            {
                var fit = RecalculateChargeEnergy(vehicle.Id);
                if (fit is not SocEnergyFit value)
                    continue;

                if (_vehicleStore.GetVehicle(vehicle.Id) is VehicleEv stored)
                    results.Add((stored, value));
            }

            return results;
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

        public IReadOnlyList<BaseEvChargeSession> GetCompletedSessions(
            Guid vehicleId,
            DateTime start,
            DateTime end) =>
            GetAndCompleteSessions(vehicleId, start, end).ToList();

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
            var engine = new HomeCircuitAttributionEngine(_energyStore);
            // Circuit rows are stamped at the start of each 10-minute interval.
            // A session that begins during an interval still draws that interval,
            // so the query has to open at the interval start rather than at StartTime.
            var queryStart = HomeCircuitInterval.StartContaining(session.StartTime);
            engine.Compute(queryStart, session.EndTime);
            session.HasCircuitData = engine.HadPositiveCircuitDraw;
            session.HasSolarData = engine.HadSolarIntervals;
            session.HasElectricContract =
                _financeStore.GetElectricContractForDate(session.StartTime.Date) is not null;

            if (session.HasCircuitData)
            {
                StoreAttribution(session, engine);
                ComputeAndStoreBilling(session);
                return;
            }

            var existingAttrib = _vehicleStore.GetHomeEvChargeAttribution(session.Id);
            if (existingAttrib is null)
                return;

            session.Attribution = existingAttrib;
            var existingBilling = _vehicleStore.GetHomeEvChargeBilling(session.Id);
            if (existingBilling is not null && existingBilling.SessionCost != 0)
                session.Billing = existingBilling;
            else
                ComputeAndStoreBilling(session);
        }

        private void StoreAttribution(HomeEvChargeSession session, HomeCircuitAttributionEngine engine)
        {
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

            var sessionCost = decimal.Round(energyCost + deliveryCost, 2, MidpointRounding.AwayFromZero);

            var billing = new HomeEvChargeBilling
            {
                SessionCost = sessionCost
            };

            session.Billing = billing;
            _vehicleStore.UpsertHomeEvChargeBilling(session.Id, billing);
        }
    }
}
