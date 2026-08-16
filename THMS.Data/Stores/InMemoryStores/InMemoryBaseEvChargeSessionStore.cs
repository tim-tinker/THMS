using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryBaseEvChargeSessionStore
    {
        private readonly List<BaseEvChargeSession> _items = new();
        private readonly InMemoryMileageRecordStore _mileageStore;

        public InMemoryBaseEvChargeSessionStore(InMemoryMileageRecordStore mileageStore)
        {
            _mileageStore = mileageStore;
        }

        public void Upsert(BaseEvChargeSession session)
        {
            var existing = _items.FirstOrDefault(s => s.Id == session.Id);
            if (existing == null)
            {
                _items.Add(session);
                _mileageStore.Upsert(session);
                return;
            }

            // Update base fields
            existing.VehicleId = session.VehicleId;
            existing.VehicleName = session.VehicleName;
            existing.StartTime = session.StartTime;
            existing.EndTime = session.EndTime;
            existing.StartSoc = session.StartSoc;
            existing.EndSoc = session.EndSoc;
            existing.LastOdometer = session.LastOdometer;
            existing.LastSoc = session.LastSoc;
            existing.OdometerMiles = session.OdometerMiles;
            existing.KwhAdded = session.KwhAdded;

            _mileageStore.Upsert(existing);
        }

        public BaseEvChargeSession? Get(Guid sessionId) =>
            _items.FirstOrDefault(s => s.Id == sessionId);

        public IEnumerable<BaseEvChargeSession> GetRange(Guid vehicleId, DateTime start, DateTime end) =>
            _items.Where(s =>
                s.VehicleId == vehicleId &&
                s.StartTime >= start &&
                s.StartTime <= end)
            .OrderBy(s => s.StartTime);

        public BaseEvChargeSession? GetLatest() =>
            _items.OrderByDescending(s => s.EndTime).FirstOrDefault();

        public BaseEvChargeSession? GetLatest(Guid vehicleId) =>
            _items.Where(s => s.VehicleId == vehicleId)
                  .OrderByDescending(s => s.EndTime)
                  .FirstOrDefault();

        public void Delete(Guid sessionId)
        {
            var existing = _items.FirstOrDefault(s => s.Id == sessionId);
            if (existing == null) return;

            _items.Remove(existing);
            _mileageStore.Delete(sessionId);
        }
    }
}
