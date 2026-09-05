using System.Reflection;
using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Logic.Energy;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class TimeBucketTests
    {
        [Test]
        public void GetHalfHour_MinutesUnder15_FloorsToHour()
        {
            var ts = new DateTime(2026, 3, 15, 10, 14, 59);
            Assert.That(TimeBucket.GetHalfHour(ts), Is.EqualTo(new DateTime(2026, 3, 15, 10, 0, 0)));
        }

        [Test]
        public void GetHalfHour_MinutesBetween15And44_MapsToHalfHour()
        {
            var ts = new DateTime(2026, 3, 15, 10, 15, 0);
            Assert.That(TimeBucket.GetHalfHour(ts), Is.EqualTo(new DateTime(2026, 3, 15, 10, 30, 0)));

            ts = new DateTime(2026, 3, 15, 10, 44, 59);
            Assert.That(TimeBucket.GetHalfHour(ts), Is.EqualTo(new DateTime(2026, 3, 15, 10, 30, 0)));
        }

        [Test]
        public void GetHalfHour_Minutes45OrLater_RollsToNextHour()
        {
            var ts = new DateTime(2026, 3, 15, 10, 45, 0);
            Assert.That(TimeBucket.GetHalfHour(ts), Is.EqualTo(new DateTime(2026, 3, 15, 11, 0, 0)));
        }

        [Test]
        public void GetHalfHour_LateNight_RollsToNextDay()
        {
            var ts = new DateTime(2026, 12, 31, 23, 50, 0);
            Assert.That(TimeBucket.GetHalfHour(ts), Is.EqualTo(new DateTime(2027, 1, 1, 0, 0, 0)));
        }
    }

    [TestFixture]
    public class SolarBucketTests
    {
        [Test]
        public void Properties_RoundTrip()
        {
            var bucket = new SolarBucket
            {
                Timestamp = new DateTime(2026, 1, 1, 12, 0, 0),
                SolarKwh = 1.1m,
                HomeConsumptionKwh = 2.2m,
                BatteryChargeKwh = 3.3m,
                BatteryDischargeKwh = 4.4m,
                GridImportKwh = 5.5m,
                GridExportKwh = 6.6m
            };

            Assert.That(bucket.SolarKwh, Is.EqualTo(1.1m));
            Assert.That(bucket.HomeConsumptionKwh, Is.EqualTo(2.2m));
            Assert.That(bucket.BatteryChargeKwh, Is.EqualTo(3.3m));
            Assert.That(bucket.BatteryDischargeKwh, Is.EqualTo(4.4m));
            Assert.That(bucket.GridImportKwh, Is.EqualTo(5.5m));
            Assert.That(bucket.GridExportKwh, Is.EqualTo(6.6m));
            Assert.That(bucket.Timestamp.Year, Is.EqualTo(2026));
        }
    }

    [TestFixture]
    public class EnergyAggregationEngineTests
    {
        [Test]
        public void Aggregate_SkipsAttributionWithoutMatchingCost_AndMergesHomeIntervals()
        {
            var store = new InMemoryEnergyDataStore();
            var jan = new DateTime(2026, 1, 10, 12, 0, 0);
            var feb = new DateTime(2026, 2, 5, 8, 0, 0);

            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = jan,
                EnergyProducedWh = 100,
                EnergyConsumedWh = 40,
                ImportedFromGridWh = 10,
                ExportedToGridWh = 20,
                StoredInBatteriesWh = 5,
                DischargedFromBatteriesWh = 3
            });
            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = feb,
                EnergyProducedWh = 50,
                EnergyConsumedWh = 10,
                ImportedFromGridWh = 1,
                ExportedToGridWh = 2,
                StoredInBatteriesWh = 0,
                DischargedFromBatteriesWh = 0
            });

            var engine = new EnergyAggregationEngine(store);
            var summaries = engine.Aggregate(
                [
                    new HomeCircuitAttribution
                    {
                        Timestamp = jan,
                        TotalWh = 30,
                        SolarWh = 10,
                        BatteryWh = 5,
                        GridWh = 15
                    },
                    new HomeCircuitAttribution
                    {
                        Timestamp = jan.AddHours(1),
                        TotalWh = 99,
                        SolarWh = 1,
                        BatteryWh = 1,
                        GridWh = 1
                    }
                ],
                [
                    new EnergyCostResult
                    {
                        Timestamp = jan,
                        SolarAvoidedCost = 1.25m,
                        BatteryValue = 0.50m,
                        GridCost = 2.00m,
                        CommercialChargeCost = 3.00m
                    }
                ],
                new DateTime(2026, 1, 1),
                new DateTime(2026, 2, 28));

            Assert.That(summaries, Has.Count.EqualTo(2));
            var january = summaries.First(s => s.Month == 1);
            Assert.That(january.EvChargeWh, Is.EqualTo(30m));
            Assert.That(january.EvChargeSolarWh, Is.EqualTo(10m));
            Assert.That(january.EvChargeBatteryWh, Is.EqualTo(5m));
            Assert.That(january.EvChargeGridWh, Is.EqualTo(15m));
            Assert.That(january.SolarAvoidedCost, Is.EqualTo(1.25m));
            Assert.That(january.BatteryValue, Is.EqualTo(0.50m));
            Assert.That(january.GridCost, Is.EqualTo(2.00m));
            Assert.That(january.CommercialChargeCost, Is.EqualTo(3.00m));
            Assert.That(january.SolarProducedWh, Is.EqualTo(100m));
            Assert.That(january.SolarConsumedWh, Is.EqualTo(40m));
            Assert.That(january.GridImportedWh, Is.EqualTo(10m));
            Assert.That(january.GridExportedWh, Is.EqualTo(20m));
            Assert.That(january.BatteryStoredWh, Is.EqualTo(5m));
            Assert.That(january.BatteryDischargedWh, Is.EqualTo(3m));

            var february = summaries.Last();
            Assert.That(february.Month, Is.EqualTo(2));
            Assert.That(february.SolarProducedWh, Is.EqualTo(50m));
            Assert.That(february.EvChargeWh, Is.EqualTo(0m));
        }

        [Test]
        public void Aggregate_FiltersOutsideRange_AndCreatesHomeOnlyMonths()
        {
            var store = new InMemoryEnergyDataStore();
            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = new DateTime(2025, 12, 1),
                EnergyProducedWh = 1
            });

            var engine = new EnergyAggregationEngine(store);
            var summaries = engine.Aggregate(
                [new HomeCircuitAttribution { Timestamp = new DateTime(2025, 6, 1), TotalWh = 10 }],
                [new EnergyCostResult { Timestamp = new DateTime(2025, 6, 1), GridCost = 1 }],
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31));

            Assert.That(summaries, Is.Empty);
        }
    }

    [TestFixture]
    public class HomeCircuitAttributionEngineTests
    {
        [Test]
        public void Compute_AttributesGridThenSolarThenBattery_AndSkipsZeroDraw()
        {
            var store = new InMemoryEnergyDataStore();
            var ts = new DateTime(2026, 4, 1, 12, 10, 0);

            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = ts,
                EnergyProducedWh = 4000,
                EnergyConsumedWh = 2500,
                ImportedFromGridWh = 800,
                ExportedToGridWh = 100,
                StoredInBatteriesWh = 200,
                DischargedFromBatteriesWh = 50
            });
            store.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = ts,
                KiloWattHours = 1.2m
            });
            store.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = ts.AddMinutes(1),
                KiloWattHours = 0
            });

            var engine = new HomeCircuitAttributionEngine(store);
            engine.Compute(new DateTime(2026, 4, 1), new DateTime(2026, 4, 2));

            Assert.That(engine.ResultCount, Is.EqualTo(1));
            var result = engine.Results.Single();
            Assert.That(result.Timestamp, Is.EqualTo(new DateTime(2026, 4, 1, 12, 0, 0)));
            Assert.That(result.GridWh, Is.EqualTo(800m));
            Assert.That(result.TotalWh, Is.EqualTo(1200m));
            Assert.That(result.SolarWh + result.BatteryWh + result.GridWh, Is.EqualTo(1200m));
        }

        [Test]
        public void Compute_ClampsNegativeSolarAvailable_AndJoinsCircuitOnlyBuckets()
        {
            var store = new InMemoryEnergyDataStore();
            var solarTs = new DateTime(2026, 4, 1, 8, 5, 0);
            var circuitOnly = new DateTime(2026, 4, 1, 9, 20, 0);

            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = solarTs,
                EnergyProducedWh = 100,
                EnergyConsumedWh = 5000,
                ImportedFromGridWh = 0,
                StoredInBatteriesWh = 200
            });
            store.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = solarTs,
                KiloWattHours = 0.5m
            });
            store.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = circuitOnly,
                KiloWattHours = 0.25m
            });

            var engine = new HomeCircuitAttributionEngine(store);
            engine.Compute(new DateTime(2026, 4, 1), new DateTime(2026, 4, 2));

            Assert.That(engine.ResultCount, Is.EqualTo(2));
            var solarBucket = engine.Results.First(r => r.Timestamp.Hour == 8);
            Assert.That(solarBucket.SolarWh, Is.EqualTo(0m));
            Assert.That(solarBucket.GridWh, Is.EqualTo(0m));
            Assert.That(solarBucket.BatteryWh, Is.EqualTo(500m));

            var circuitBucket = engine.Results.First(r => r.Timestamp.Hour == 9);
            Assert.That(circuitBucket.TotalWh, Is.EqualTo(250m));
            Assert.That(circuitBucket.SolarWh + circuitBucket.BatteryWh + circuitBucket.GridWh, Is.EqualTo(250m));
        }

        [Test]
        public void Compute_ClearsPreviousResults()
        {
            var store = new InMemoryEnergyDataStore();
            store.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = new DateTime(2026, 1, 1, 1, 0, 0),
                KiloWattHours = 1
            });

            var engine = new HomeCircuitAttributionEngine(store);
            engine.Compute(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            Assert.That(engine.ResultCount, Is.EqualTo(1));

            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = new DateTime(2026, 5, 1, 3, 0, 0),
                EnergyProducedWh = 100
            });
            engine.Compute(new DateTime(2026, 5, 1), new DateTime(2026, 5, 2));
            Assert.That(engine.ResultCount, Is.EqualTo(0));

            engine.Compute(new DateTime(2027, 1, 1), new DateTime(2027, 1, 2));
            Assert.That(engine.ResultCount, Is.EqualTo(0));
            Assert.That(engine.Results, Is.Empty);
        }
    }

    [TestFixture]
    public class EnergyAggregationServiceTests
    {
        private static InMemoryEnergyDataStore SeedDay(DateTime day)
        {
            var store = new InMemoryEnergyDataStore();
            for (var hour = 10; hour <= 12; hour++)
            {
                store.UpsertSolarProductionInterval(new SolarProductionInterval
                {
                    Timestamp = new DateTime(day.Year, day.Month, day.Day, hour, 0, 0),
                    EnergyProducedWh = 2000,
                    EnergyConsumedWh = 1000,
                    ImportedFromGridWh = 100,
                    ExportedToGridWh = 50,
                    StoredInBatteriesWh = 80,
                    DischargedFromBatteriesWh = 40
                });
                store.UpsertHomeCircuitAttribution(new HomeCircuitAttribution
                {
                    Timestamp = new DateTime(day.Year, day.Month, day.Day, hour, 0, 0),
                    TotalWh = 300
                });
            }

            return store;
        }

        [Test]
        public void GetPeriodSummary_CopiesPeriodTotals()
        {
            var service = new EnergyAggregationService(new InMemoryEnergyDataStore());
            var period = new EnergyPeriod
            {
                SolarKwh = 1,
                HomeConsumptionKwh = 2,
                GridImportKwh = 3,
                GridExportKwh = 4,
                BatteryChargeKwh = 5,
                BatteryDischargeKwh = 6,
                EvChargeKwh = 7
            };

            var summary = service.GetPeriodSummary(period);
            Assert.That(summary.ProducedKwh, Is.EqualTo(1));
            Assert.That(summary.ConsumedKwh, Is.EqualTo(2));
            Assert.That(summary.GridImportKwh, Is.EqualTo(3));
            Assert.That(summary.GridExportKwh, Is.EqualTo(4));
            Assert.That(summary.BatteryChargeKwh, Is.EqualTo(5));
            Assert.That(summary.BatteryDischargeKwh, Is.EqualTo(6));
            Assert.That(summary.EvChargeKwh, Is.EqualTo(7));
        }

        [Test]
        public void GetDay_ComputesAnalytics_IncludingPeakWindowAndEvBreakdown()
        {
            var day = new DateTime(2026, 6, 15);
            var service = new EnergyAggregationService(SeedDay(day));
            var period = service.GetDay(day);

            Assert.That(period.Records, Has.Count.EqualTo(1));
            Assert.That(period.SolarKwh, Is.GreaterThan(0));
            Assert.That(period.PeriodMax, Is.GreaterThan(0));
            Assert.That(period.PeriodPeakWindowStart, Is.Not.Null);
            Assert.That(period.PeriodPeakWindowEnd, Is.Not.Null);
            Assert.That(period.PeriodSolarPeakKw, Is.GreaterThan(0));
            Assert.That(period.BatteryContributionToHomeLoad, Is.GreaterThan(0));
            Assert.That(period.EvSolarKwhTotal, Is.GreaterThan(0));
            Assert.That(period.Records[0].DailyBatteryChargeDuration, Is.EqualTo(TimeSpan.FromMinutes(90)));
        }

        [Test]
        public void GetWeekMonthYearRange_ReturnEmptyWhenNoData()
        {
            var service = new EnergyAggregationService(new InMemoryEnergyDataStore());
            var date = new DateTime(2026, 3, 4);

            Assert.That(service.GetWeek(date).Records, Is.Empty);
            Assert.That(service.GetMonth(date).Records, Is.Empty);
            Assert.That(service.GetYear(date).Records, Is.Empty);
            Assert.That(service.GetRange(date, date.AddDays(1)).Records, Is.Empty);
            Assert.That(service.GetDay(date).PeriodMin, Is.EqualTo(0));
        }

        [Test]
        public void GetDay_SingleInterval_SkipsPeakWindow_AndZeroHomeLoadBatteryShare()
        {
            var store = new InMemoryEnergyDataStore();
            var ts = new DateTime(2026, 7, 1, 6, 0, 0);
            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = ts,
                EnergyProducedWh = 500,
                EnergyConsumedWh = 0,
                ImportedFromGridWh = 0,
                ExportedToGridWh = 0
            });

            var period = new EnergyAggregationService(store).GetDay(ts);
            Assert.That(period.Records, Has.Count.EqualTo(1));
            Assert.That(period.Records[0].PeakWindowStart, Is.Null);
            Assert.That(period.BatteryContributionToHomeLoad, Is.EqualTo(0));
        }

        [Test]
        public void GetDay_EvWithoutSolarInSameBucket_DoesNotCreateIntervalFromEvAlone()
        {
            var store = new InMemoryEnergyDataStore();
            var day = new DateTime(2026, 8, 1);
            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = day.AddHours(1),
                EnergyProducedWh = 100,
                EnergyConsumedWh = 50
            });
            store.UpsertHomeCircuitAttribution(new HomeCircuitAttribution
            {
                Timestamp = day.AddHours(5),
                TotalWh = 900
            });

            var period = new EnergyAggregationService(store).GetDay(day);
            Assert.That(period.EvChargeKwh, Is.EqualTo(0));
            Assert.That(period.Records[0].EvGridKwh, Is.EqualTo(0));
        }

        [Test]
        public void ComputeDailyAnalytics_EmptyIntervals_ReturnsImmediately()
        {
            var service = new EnergyAggregationService(new InMemoryEnergyDataStore());
            var record = new EnergyPeriodRecord();
            typeof(EnergyAggregationService)
                .GetMethod("ComputeDailyAnalytics", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(service, [record, new List<EnergyIntervalRecord>()]);

            Assert.That(record.HourlyMax, Is.EqualTo(0));
        }
    }
}
