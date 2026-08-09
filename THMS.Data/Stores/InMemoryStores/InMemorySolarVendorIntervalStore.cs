using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemorySolarVendorIntervalStore
    {
        private readonly List<SolarVendorInterval> _items = new();

        public void Upsert(SolarVendorInterval item)
        {
            var existing = _items.FirstOrDefault(i => i.Timestamp == item.Timestamp);
            if (existing is null)
            {
                _items.Add(item);
                return;
            }

            existing.EnergyProducedWh = item.EnergyProducedWh;
            existing.EnergyConsumedWh = item.EnergyConsumedWh;
            existing.ExportedToGridWh = item.ExportedToGridWh;
            existing.ImportedFromGridWh = item.ImportedFromGridWh;
            existing.StoredInBatteriesWh = item.StoredInBatteriesWh;
            existing.DischargedFromBatteriesWh = item.DischargedFromBatteriesWh;
        }

        public IReadOnlyCollection<SolarVendorInterval> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                .OrderBy(i => i.Timestamp)
                .ToList()
                .AsReadOnly();
        }

        public SolarVendorInterval? GetLatest()
        {
            return _items
                .OrderByDescending(i => i.Timestamp)
                .FirstOrDefault();
        }
    }
}
