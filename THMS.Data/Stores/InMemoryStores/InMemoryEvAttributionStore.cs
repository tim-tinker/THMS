using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryEvAttributionStore
    {
        private readonly List<EnergyAttributionResult> _items = new();

        public void Add(EnergyAttributionResult item)
        {
            _items.Add(item);
        }

        public IReadOnlyCollection<EnergyAttributionResult> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                .OrderBy(i => i.Timestamp)
                .ToList()
                .AsReadOnly();
        }
    }
}
