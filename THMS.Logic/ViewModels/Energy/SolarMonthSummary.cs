using THMS.Domain.Energy;

namespace THMS.Logic.ViewModels.Energy
{
    public class SolarMonthSummary
    {
        public DateTime Month { get; init; }
        public int DayCount { get; init; }
        public decimal EnergyProducedWh { get; init; }
        public decimal EnergyConsumedWh { get; init; }
        public decimal ExportedToGridWh { get; init; }
        public decimal ImportedFromGridWh { get; init; }
        public decimal StoredInBatteriesWh { get; init; }
        public decimal DischargedFromBatteriesWh { get; init; }

        public static IReadOnlyList<SolarMonthSummary> From(IEnumerable<SolarProductionInterval> intervals)
        {
            ArgumentNullException.ThrowIfNull(intervals);
            return intervals
                .GroupBy(interval => new DateTime(interval.Timestamp.Year, interval.Timestamp.Month, 1))
                .OrderByDescending(group => group.Key)
                .Select(group => new SolarMonthSummary
                {
                    Month = group.Key,
                    DayCount = group.Select(interval => interval.Timestamp.Date).Distinct().Count(),
                    EnergyProducedWh = group.Sum(interval => interval.EnergyProducedWh),
                    EnergyConsumedWh = group.Sum(interval => interval.EnergyConsumedWh),
                    ExportedToGridWh = group.Sum(interval => interval.ExportedToGridWh),
                    ImportedFromGridWh = group.Sum(interval => interval.ImportedFromGridWh),
                    StoredInBatteriesWh = group.Sum(interval => interval.StoredInBatteriesWh),
                    DischargedFromBatteriesWh = group.Sum(interval => interval.DischargedFromBatteriesWh)
                })
                .ToList();
        }
    }
}
