using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryHomeEvChargeBillingStore
    {
        private readonly Dictionary<Guid, HomeEvChargeBilling> _items = new();

        public void Upsert(Guid sessionId, HomeEvChargeBilling billing)
        {
            _items[sessionId] = billing;
        }

        public HomeEvChargeBilling? Get(Guid sessionId)
        {
            _items.TryGetValue(sessionId, out var billing);
            return billing;
        }
    }
}
