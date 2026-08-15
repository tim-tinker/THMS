using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryHomeEvChargeAttributionStore
    {
        private readonly Dictionary<Guid, HomeEvChargeAttribution> _items = new();

        public void Upsert(Guid sessionId, HomeEvChargeAttribution attribution)
        {
            _items[sessionId] = attribution;
        }

        public HomeEvChargeAttribution? Get(Guid sessionId)
        {
            _items.TryGetValue(sessionId, out var attrib);
            return attrib;
        }
    }
}
