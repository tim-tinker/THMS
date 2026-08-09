using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    /// <summary>
    /// Shared mileage base records (ICE + EV). Holds references pushed by the ICE/EV stores.
    /// </summary>
    public class InMemoryMileageRecordStore
    {
        private readonly List<MileageRecordBase> _items = new();

        public void Upsert(MileageRecordBase record)
        {
            var existing = _items.FirstOrDefault(r => r.Id == record.Id);
            if (existing is null)
            {
                _items.Add(record);
                return;
            }

            if (!ReferenceEquals(existing, record))
            {
                var index = _items.IndexOf(existing);
                _items[index] = record;
            }
        }

        public void Delete(Guid id)
        {
            _items.RemoveAll(r => r.Id == id);
        }

        public decimal GetMilesDrivenInPeriod(Guid vehicleId, DateTime start, DateTime end)
        {
            var iceRecords = _items
                .OfType<IceMileageRecord>()
                .Where(r => r.VehicleId == vehicleId && r.EndTime >= start && r.EndTime <= end);

            var evRecords = _items
                .OfType<EvChargeSession>()
                .Where(r => r.VehicleId == vehicleId
                            && r.StartTime >= start
                            && r.EndTime <= end
                            && r.OdometerMiles > 0);

            var allRecords = iceRecords
                .Cast<MileageRecordBase>()
                .Concat(evRecords)
                .OrderBy(r => r.EndTime)
                .ToList();

            if (allRecords.Count < 2)
                return 0m;

            return allRecords.Last().OdometerMiles - allRecords.First().OdometerMiles;
        }
    }
}
