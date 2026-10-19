using Microsoft.Data.Sqlite;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Energy;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class HomeCircuitReadingSqliteTests
    {
        [Test]
        public void Import_StoresEachInterval_AndUpdatesOnReimport()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-circuit-{Guid.NewGuid():N}.db");
            var csv = Path.Combine(Path.GetTempPath(), $"thms-circuit-{Guid.NewGuid():N}.csv");
            try
            {
                File.WriteAllText(csv,
                    "Local SPAN Panel time (America/Chicago),Energy Data (Wh)\n" +
                    "2026-07-03 11:00:00,1.85\n" +
                    "2026-07-03 11:10:00,1.72\n");

                var store = new SQLiteEnergyDataStore(path);
                var orchestrator = new HomeCircuitReadingOrchestrator(store);
                var imported = orchestrator.ImportReadings(orchestrator.LoadReadingsFromFiles([csv]));

                Assert.That(imported.Count, Is.EqualTo(2));
                var readings = store.GetHomeCircuitReadings(DateTime.MinValue, DateTime.MaxValue)
                    .OrderBy(reading => reading.Timestamp)
                    .ToList();
                Assert.That(readings.Select(reading => reading.Timestamp), Is.EqualTo(new[]
                {
                    new DateTime(2026, 7, 3, 11, 0, 0),
                    new DateTime(2026, 7, 3, 11, 10, 0)
                }));
                Assert.That(readings.Select(reading => reading.Id), Is.Unique);
                Assert.That(readings.Select(reading => reading.Id), Has.No.EqualTo(Guid.Empty));
                Assert.That(readings[0].KiloWattHours, Is.EqualTo(1.85m));

                var attribution = store.GetHomeCircuitAttribution(DateTime.MinValue, DateTime.MaxValue);
                Assert.That(attribution, Has.Count.EqualTo(1));
                Assert.That(attribution.Single().TotalWh, Is.EqualTo(3570m));

                File.WriteAllText(csv,
                    "Local SPAN Panel time (America/Chicago),Energy Data (Wh)\n" +
                    "2026-07-03 11:00:00,2.00\n" +
                    "2026-07-03 11:10:00,1.72\n");
                orchestrator.ImportReadings(orchestrator.LoadReadingsFromFiles([csv]));

                readings = store.GetHomeCircuitReadings(DateTime.MinValue, DateTime.MaxValue).ToList();
                Assert.That(readings, Has.Count.EqualTo(2));
                Assert.That(readings.Single(reading => reading.Timestamp == new DateTime(2026, 7, 3, 11, 0, 0)).KiloWattHours,
                    Is.EqualTo(2.00m));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(csv))
                    File.Delete(csv);
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                {
                    var file = path + suffix;
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        [Test]
        public void Upsert_AllowsDefaultIdsForDistinctTimestamps()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-circuit-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteEnergyDataStore(path);
                store.UpsertHomeCircuitReading(new HomeCircuitReading
                {
                    Timestamp = new DateTime(2026, 7, 3, 11, 0, 0),
                    KiloWattHours = 1.85m
                });
                store.UpsertHomeCircuitReading(new HomeCircuitReading
                {
                    Timestamp = new DateTime(2026, 7, 3, 11, 10, 0),
                    KiloWattHours = 1.72m
                });

                Assert.That(store.GetHomeCircuitReadings(DateTime.MinValue, DateTime.MaxValue).Count(), Is.EqualTo(2));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                {
                    var file = path + suffix;
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }
    }
}
