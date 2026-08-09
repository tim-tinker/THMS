using THMS.Domain.Energy;

namespace THMS.Data.Stores.InMemoryStores
{
    public class InMemoryEvCircuitSegmentsStore
    {
        private readonly Dictionary<Guid, List<EvCircuitSegment>> _segments = new();

        public void Save(Guid sessionId, IEnumerable<EvCircuitSegment> segments)
        {
            _segments[sessionId] = segments.ToList();
        }

        public IEnumerable<EvCircuitSegment> Get(Guid sessionId)
        {
            return _segments.TryGetValue(sessionId, out var list)
                ? list
                : Enumerable.Empty<EvCircuitSegment>();
        }

        public void Delete(Guid sessionId)
        {
            _segments.Remove(sessionId);
        }

        public EvCircuitSegmentSummary GetSummary(Guid sessionId)
        {
            var segs = Get(sessionId).ToList();

            if (!segs.Any())
            {
                return new EvCircuitSegmentSummary
                {
                    SessionId = sessionId,
                    TotalKwh = 0,
                    GridKwh = 0,
                    SolarKwh = 0,
                    BatteryKwh = 0,
                    SegmentCount = 0
                };
            }

            return new EvCircuitSegmentSummary
            {
                SessionId = sessionId,
                TotalKwh = segs.Sum(s => s.Kwh),
                GridKwh = segs.Sum(s => s.GridKwh),
                SolarKwh = segs.Sum(s => s.SolarKwh),
                BatteryKwh = segs.Sum(s => s.BatteryKwh),
                SegmentCount = segs.Count,
                StartTime = segs.Min(s => s.Timestamp),
                EndTime = segs.Max(s => s.Timestamp)
            };
        }
    }
}
