using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryEvChargeSessionStore
    {
        private readonly List<EvChargeSession> _items = new();
        private readonly InMemoryMileageRecordStore _mileageStore;

        public InMemoryEvChargeSessionStore(InMemoryMileageRecordStore mileageStore)
        {
            _mileageStore = mileageStore;
        }

        public void Upsert(EvChargeSession session)
        {
            var existing = _items.FirstOrDefault(s => s.Id == session.Id);
            if (existing is null)
            {
                _items.Add(session);
                _mileageStore.Upsert(session);
                return;
            }

            existing.VehicleId = session.VehicleId;
            existing.StartTime = session.StartTime;
            existing.EndTime = session.EndTime;
            existing.StartSoc = session.StartSoc;
            existing.EndSoc = session.EndSoc;
            existing.LastOdometer = session.LastOdometer;
            existing.LastSoc = session.LastSoc;
            existing.OdometerMiles = session.OdometerMiles;
            existing.KwhAdded = session.KwhAdded;
            existing.BatteryKwhAdded = session.BatteryKwhAdded;
            existing.IsHomeCharge = session.IsHomeCharge;
            existing.SessionCost = session.SessionCost;
            existing.GridKwh = session.GridKwh;
            existing.SolarKwh = session.SolarKwh;
            existing.BatteryKwh = session.BatteryKwh;

            _mileageStore.Upsert(existing);
        }

        public EvChargeSession? Get(Guid sessionId)
        {
            return _items.FirstOrDefault(s => s.Id == sessionId);
        }

        public IEnumerable<EvChargeSession> GetRange(Guid vehicleId, DateTime start, DateTime end)
        {
            return _items
                .Where(s => s.VehicleId == vehicleId &&
                            s.StartTime >= start &&
                            s.StartTime <= end)
                .OrderBy(s => s.StartTime);
        }

        public EvChargeSession? GetLatest()
        {
            return _items
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefault();
        }

        public void Delete(Guid sessionId)
        {
            var existing = _items.FirstOrDefault(s => s.Id == sessionId);
            if (existing == null)
                return;

            _items.Remove(existing);
            _mileageStore.Delete(sessionId);
        }
    }
}
