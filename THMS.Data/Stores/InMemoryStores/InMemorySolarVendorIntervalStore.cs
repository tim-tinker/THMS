using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemorySolarVendorIntervalStore
    {
        private readonly List<SolarVendorInterval> _items = new();

        public void Add(SolarVendorInterval item)
        {
            _items.Add(item);
        }

        public IReadOnlyCollection<SolarVendorInterval> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                .OrderBy(i => i.Timestamp)
                .ToList()
                .AsReadOnly();
        }
    }
}
