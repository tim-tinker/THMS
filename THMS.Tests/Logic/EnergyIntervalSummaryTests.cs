using THMS.Domain.Energy;
using THMS.Logic.ViewModels.Energy;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class EnergyIntervalSummaryTests
    {
        [Test]
        public void HomeCircuitDaySummary_GroupsByCalendarDayAndSumsEnergy()
        {
            var readings = new[]
            {
                new HomeCircuitReading { Timestamp = new DateTime(2026, 3, 15, 0, 15, 0), KiloWattHours = 1.25m },
                new HomeCircuitReading { Timestamp = new DateTime(2026, 3, 15, 23, 45, 0), KiloWattHours = 0.75m },
                new HomeCircuitReading { Timestamp = new DateTime(2026, 3, 16, 0, 0, 0), KiloWattHours = 2m },
                new HomeCircuitReading { Timestamp = new DateTime(2026, 3, 14, 18, 0, 0), KiloWattHours = 0.5m }
            };

            var days = HomeCircuitDaySummary.From(readings);

            Assert.That(days.Select(day => day.Date), Is.EqualTo(new[]
            {
                new DateTime(2026, 3, 16),
                new DateTime(2026, 3, 15),
                new DateTime(2026, 3, 14)
            }));
            Assert.That(days[1].IntervalCount, Is.EqualTo(2));
            Assert.That(days[1].KiloWattHours, Is.EqualTo(2m));
            Assert.That(days[0].IntervalCount, Is.EqualTo(1));
            Assert.That(days[0].KiloWattHours, Is.EqualTo(2m));
        }

        [Test]
        public void HomeCircuitDaySummary_EmptyInput_ReturnsNoDays()
        {
            Assert.That(HomeCircuitDaySummary.From([]), Is.Empty);
        }

        [Test]
        public void HomeCircuitAttributionDaySummary_GroupsByCalendarDayAndSumsEachSource()
        {
            var attributions = new[]
            {
                Attribution(new DateTime(2026, 3, 15, 8, 0, 0), 10, 4, 3, 3),
                Attribution(new DateTime(2026, 3, 15, 18, 30, 0), 5, 1, 2, 2),
                Attribution(new DateTime(2026, 3, 16, 0, 0, 0), 8, 8, 0, 0)
            };

            var days = HomeCircuitAttributionDaySummary.From(attributions);

            Assert.That(days.Select(day => day.Date), Is.EqualTo(new[]
            {
                new DateTime(2026, 3, 16),
                new DateTime(2026, 3, 15)
            }));
            Assert.That(days[1].TotalWh, Is.EqualTo(15m));
            Assert.That(days[1].SolarWh, Is.EqualTo(5m));
            Assert.That(days[1].BatteryWh, Is.EqualTo(5m));
            Assert.That(days[1].GridWh, Is.EqualTo(5m));
            Assert.That(days[0].TotalWh, Is.EqualTo(8m));
            Assert.That(days[0].SolarWh, Is.EqualTo(8m));
        }

        [Test]
        public void HomeCircuitAttributionDaySummary_EmptyInput_ReturnsNoDays()
        {
            Assert.That(HomeCircuitAttributionDaySummary.From([]), Is.Empty);
        }

        [Test]
        public void SolarMonthSummary_GroupsByCalendarMonthAndSumsEachEnergyColumn()
        {
            var intervals = new[]
            {
                Interval(new DateTime(2026, 1, 1, 8, 0, 0), 10, 4, 1, 2, 3, 5),
                Interval(new DateTime(2026, 1, 1, 12, 0, 0), 1, 1, 1, 1, 1, 1),
                Interval(new DateTime(2026, 1, 31, 18, 30, 0), 20, 6, 2, 1, 4, 7),
                Interval(new DateTime(2026, 2, 1, 0, 0, 0), 8, 3, 0, 9, 1, 2)
            };

            var months = SolarMonthSummary.From(intervals);

            Assert.That(months.Select(month => month.Month), Is.EqualTo(new[]
            {
                new DateTime(2026, 2, 1),
                new DateTime(2026, 1, 1)
            }));
            Assert.That(months[1].DayCount, Is.EqualTo(2));
            Assert.That(months[1].EnergyProducedWh, Is.EqualTo(31m));
            Assert.That(months[1].EnergyConsumedWh, Is.EqualTo(11m));
            Assert.That(months[1].ExportedToGridWh, Is.EqualTo(4m));
            Assert.That(months[1].ImportedFromGridWh, Is.EqualTo(4m));
            Assert.That(months[1].StoredInBatteriesWh, Is.EqualTo(8m));
            Assert.That(months[1].DischargedFromBatteriesWh, Is.EqualTo(13m));
            Assert.That(months[0].DayCount, Is.EqualTo(1));
            Assert.That(months[0].EnergyProducedWh, Is.EqualTo(8m));
        }

        [Test]
        public void SolarMonthSummary_EmptyInput_ReturnsNoMonths()
        {
            Assert.That(SolarMonthSummary.From([]), Is.Empty);
        }

        private static HomeCircuitAttribution Attribution(
            DateTime timestamp,
            decimal total,
            decimal solar,
            decimal battery,
            decimal grid) =>
            new()
            {
                Timestamp = timestamp,
                TotalWh = total,
                SolarWh = solar,
                BatteryWh = battery,
                GridWh = grid
            };

        private static SolarProductionInterval Interval(
            DateTime timestamp,
            decimal produced,
            decimal consumed,
            decimal exported,
            decimal imported,
            decimal charged,
            decimal discharged) =>
            new()
            {
                Timestamp = timestamp,
                EnergyProducedWh = produced,
                EnergyConsumedWh = consumed,
                ExportedToGridWh = exported,
                ImportedFromGridWh = imported,
                StoredInBatteriesWh = charged,
                DischargedFromBatteriesWh = discharged
            };
    }
}
