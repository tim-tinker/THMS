using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryEvCommercialChargeSessionStore
    {
        private readonly List<EvCommercialChargeSession> _items = new();

        public void Upsert(EvCommercialChargeSession item)
        {
            var existing = _items.FirstOrDefault(s => s.EndTime == item.EndTime);
            if (existing is null)
            {
                _items.Add(item);
                return;
            }

            existing.StartTime = item.StartTime;
            existing.KwhAdded = item.KwhAdded;
            existing.ChargeCost = item.ChargeCost;
            existing.VendorSessionId = item.VendorSessionId;
            existing.Location = item.Location;
            if (existing.Id == Guid.Empty && item.Id != Guid.Empty)
                existing.Id = item.Id;
        }

        public IReadOnlyCollection<EvCommercialChargeSession> GetRange(DateTime start, DateTime end)
        {
            return _items
                .Where(s => s.StartTime >= start && s.EndTime <= end)
                .OrderBy(s => s.StartTime)
                .ToList()
                .AsReadOnly();
        }
    }
}
