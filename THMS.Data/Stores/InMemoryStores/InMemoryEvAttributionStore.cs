using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryEvAttributionStore
    {
        private readonly List<EnergyAttributionResult> _items = new();

        public void Upsert(EnergyAttributionResult item)
        {
            var existing = _items.FirstOrDefault(i => i.Timestamp == item.Timestamp);
            if (existing is null)
            {
                _items.Add(item);
                return;
            }

            existing.EvChargeWh = item.EvChargeWh;
            existing.SolarWh = item.SolarWh;
            existing.BatteryWh = item.BatteryWh;
            existing.GridWh = item.GridWh;
            existing.IsPartial = item.IsPartial;
        }

        public IReadOnlyCollection<EnergyAttributionResult> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(i => i.Timestamp >= start && i.Timestamp <= end)
                .OrderBy(i => i.Timestamp)
                .ToList()
                .AsReadOnly();
        }

        public EnergyAttributionResult? GetLatest()
        {
            return _items
                .OrderByDescending(i => i.Timestamp)
                .FirstOrDefault();
        }
    }
}
