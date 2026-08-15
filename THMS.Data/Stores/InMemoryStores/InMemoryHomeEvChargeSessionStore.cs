using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryHomeEvChargeSessionStore
    {
        private readonly Dictionary<Guid, HomeEvChargeSession> _items = new();

        public void Upsert(HomeEvChargeSession session)
        {
            _items[session.Id] = session;
        }

        public HomeEvChargeSession? Get(Guid sessionId)
        {
            _items.TryGetValue(sessionId, out var session);
            return session;
        }
    }
}
