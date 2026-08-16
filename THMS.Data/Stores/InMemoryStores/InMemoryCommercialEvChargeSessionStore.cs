using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryCommercialEvChargeSessionStore
    {
        private readonly Dictionary<Guid, CommercialEvChargeSession> _items = new();

        public void Upsert(CommercialEvChargeSession session)
        {
            _items[session.Id] = session;
        }

        public CommercialEvChargeSession? Get(Guid sessionId)
        {
            _items.TryGetValue(sessionId, out var session);
            return session;
        }
    }
}
