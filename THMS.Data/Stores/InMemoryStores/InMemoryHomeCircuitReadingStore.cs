using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryHomeCircuitReadingStore
    {
        private readonly List<HomeCircuitReading> _items = new();

        public void Upsert(HomeCircuitReading item)
        {
            var existing = _items.FirstOrDefault(r => r.Timestamp == item.Timestamp);
            if (existing is null)
            {
                _items.Add(item);
                return;
            }

            existing.KiloWattHours = item.KiloWattHours;
            existing.CircuitId = item.CircuitId;
            if (existing.Id == Guid.Empty && item.Id != Guid.Empty)
                existing.Id = item.Id;
        }

        public IReadOnlyCollection<HomeCircuitReading> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .OrderBy(r => r.Timestamp)
                .ToList()
                .AsReadOnly();
        }

        public HomeCircuitReading? GetLatest()
        {
            return _items
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefault();
        }
    }
}
