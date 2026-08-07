using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryBatterySocStore
    {
        private readonly List<BatterySocRecord> _items = new();

        public void Add(BatterySocRecord item)
        {
            _items.Add(item);
        }

        public IReadOnlyCollection<BatterySocRecord> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                .OrderBy(i => i.Timestamp)
                .ToList()
                .AsReadOnly();
        }
    }
}
