using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class HomeCircuitDaySummary
    {
        public DateTime Date { get; init; }
        public int IntervalCount { get; init; }
        public decimal KiloWattHours { get; init; }

        public static IReadOnlyList<HomeCircuitDaySummary> From(IEnumerable<HomeCircuitReading> readings)
        {
            ArgumentNullException.ThrowIfNull(readings);
            return readings
                .GroupBy(reading => reading.Timestamp.Date)
                .OrderByDescending(group => group.Key)
                .Select(group => new HomeCircuitDaySummary
                {
                    Date = group.Key,
                    IntervalCount = group.Count(),
                    KiloWattHours = group.Sum(reading => reading.KiloWattHours)
                })
                .ToList();
        }
    }
}
