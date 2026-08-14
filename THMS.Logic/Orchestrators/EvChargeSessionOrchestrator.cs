using THMS.Data.Stores;
using THMS.Domain.Transportation;

namespace THMS.Logic.Orchestrators
{
    public class EvChargeSessionOrchestrator : BaseOrchestrator
    {
        private IVehicleDataStore _vehicleStore;

        public Guid VehicleId { get; set; }

        public EvChargeSessionOrchestrator(IVehicleDataStore vehicleStore)
        {
            _vehicleStore = vehicleStore;
        }

        public EvChargeSession GetLastSession()
        {
            return _vehicleStore.GetLatestEvChargeSession();
        }

        public void Update(EvChargeSession session)
        {
            _vehicleStore.UpsertEvChargeSession(session);
        }

        public IEnumerable<EvChargeSession> GetEvChargeSessions(string period)
        {
            var readings = Array.Empty<EvChargeSession>();
            var latest = _vehicleStore.GetLatestEvChargeSession(VehicleId);
            if (latest is not null)
            {
                var end = latest.EndTime;
                var start = GetStartDate(end, period);
                readings = _vehicleStore.GetEvChargeSessions(VehicleId, start, end).ToArray();
            }

            return readings;
        }

        public void Save(EvChargeSession session)
        {
            _vehicleStore.UpsertEvChargeSession(session);
        }
    }
}
