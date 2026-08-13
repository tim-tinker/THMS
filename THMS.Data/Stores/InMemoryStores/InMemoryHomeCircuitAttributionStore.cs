using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryHomeCircuitAttributionStore
    {
        private readonly List<HomeCircuitAttribution> _items = new();

        public void Upsert(HomeCircuitAttribution item)
        {
            var existing = _items.FirstOrDefault(i => i.Timestamp == item.Timestamp);
            if (existing is null)
            {
                _items.Add(item);
                return;
            }

            existing.TotalWh = item.TotalWh;
            existing.SolarWh = item.SolarWh;
            existing.BatteryWh = item.BatteryWh;
            existing.GridWh = item.GridWh;
        }

        public IReadOnlyCollection<HomeCircuitAttribution> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                .OrderBy(i => i.Timestamp)
                .ToList()
                .AsReadOnly();
        }

        public HomeCircuitAttribution? GetLatest()
        {
            return _items
                .OrderByDescending(i => i.Timestamp)
                .FirstOrDefault();
        }
    }
}
