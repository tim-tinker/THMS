using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class HomeCircuitAttributionDaySummary
    {
        public DateTime Date { get; init; }
        public decimal TotalWh { get; init; }
        public decimal SolarWh { get; init; }
        public decimal BatteryWh { get; init; }
        public decimal GridWh { get; init; }

        public static IReadOnlyList<HomeCircuitAttributionDaySummary> From(IEnumerable<HomeCircuitAttribution> attributions)
        {
            ArgumentNullException.ThrowIfNull(attributions);
            return attributions
                .GroupBy(attribution => attribution.Timestamp.Date)
                .OrderByDescending(group => group.Key)
                .Select(group => new HomeCircuitAttributionDaySummary
                {
                    Date = group.Key,
                    TotalWh = group.Sum(attribution => attribution.TotalWh),
                    SolarWh = group.Sum(attribution => attribution.SolarWh),
                    BatteryWh = group.Sum(attribution => attribution.BatteryWh),
                    GridWh = group.Sum(attribution => attribution.GridWh)
                })
                .ToList();
        }
    }
}
