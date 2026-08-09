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

        public IEnumerable<GasPurchase> GetWithMissingCost()
        {
            return _items
                .Where(g => g.FuelCost == 0)
                .OrderBy(g => g.Date);
        }

        public void UpdateCost(Guid purchaseId, decimal cost)
        {
            var purchase = _items.FirstOrDefault(g => g.Id == purchaseId);
            if (purchase != null)
                purchase.FuelCost = cost;
        }
    }
}
