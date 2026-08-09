using THMS.Domain.Transportation;

namespace THMS.Data.Stores.InMemoryStores
{
    /// <summary>
    /// Finance-side view of EV charge session costs (separate from IVehicleDataStore ownership).
    /// </summary>
    public class InMemoryEvChargeSessionCostStore
    {
        private readonly List<EvChargeSession> _items = new();

        public void Upsert(EvChargeSession session)
        {
            var index = _items.FindIndex(s => s.Id == session.Id);
            if (index < 0)
                _items.Add(session);
            else
                _items[index] = session;
        }

        public IEnumerable<EvChargeSession> GetWithMissingCost()
        {
            return _items
                .Where(s => s.SessionCost == 0)
                .OrderBy(s => s.StartTime);
        }

        public void UpdateCost(Guid sessionId, decimal cost)
        {
            var session = _items.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
                session.SessionCost = cost;
        }
    }
}
