using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryGasPurchaseStore
    {
        private readonly List<GasPurchase> _items = new();

        public void Upsert(GasPurchase purchase)
        {
            var index = _items.FindIndex(g => g.Id == purchase.Id);
            if (index < 0)
                _items.Add(purchase);
            else
                _items[index] = purchase;
        }

        public IEnumerable<GasPurchase> GetRange(Guid vehicleId, DateTime start, DateTime end)
        {
            return _items
                .Where(g => g.VehicleId == vehicleId &&
                            g.Date >= start &&
                            g.Date <= end)
                .OrderBy(g => g.Date);
        }
    }
}
