using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryIceMileageStore
    {
        private readonly List<IceMileageRecord> _items = new();
        private readonly InMemoryMileageRecordStore _mileageStore;

        public InMemoryIceMileageStore(InMemoryMileageRecordStore mileageStore)
        {
            _mileageStore = mileageStore;
        }

        public void Upsert(IceMileageRecord record)
        {
            var index = _items.FindIndex(r => r.Id == record.Id);
            if (index < 0)
                _items.Add(record);
            else
                _items[index] = record;

            _mileageStore.Upsert(record);
        }

        public IceMileageRecord? GetEarliest(Guid vehicleId)
        {
            return _items
                .Where(r => r.VehicleId == vehicleId)
                .OrderBy(r => r.EndTime)
                .FirstOrDefault();
        }

        public IEnumerable<IceMileageRecord> GetRange(Guid vehicleId, DateTime start, DateTime end)
        {
            return _items
                .Where(r => r.VehicleId == vehicleId && r.EndTime >= start && r.EndTime <= end)
                .OrderBy(r => r.EndTime);
        }
    }
}
