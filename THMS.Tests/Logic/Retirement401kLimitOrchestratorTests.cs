using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class Retirement401kLimitOrchestratorTests
    {
        [Test]
        public void Save_ReplacesTheSameYear_AndLatestIsTheHighestYear()
        {
            var limits = new Retirement401kLimitOrchestrator(new InMemoryRetirement401kLimitStore());
            limits.Save(Retirement401kLimits.For2026() with { ElectiveDeferral = 24_500m });
            limits.Save(Retirement401kLimits.For2026() with { TaxYear = 2027, ElectiveDeferral = 26_000m });
            limits.Save(Retirement401kLimits.For2026() with { ElectiveDeferral = 25_000m });

            Assert.That(limits.Get(2026)!.ElectiveDeferral, Is.EqualTo(25_000m));
            Assert.That(limits.Get(2027)!.ElectiveDeferral, Is.EqualTo(26_000m));
            Assert.That(limits.GetLatest()!.TaxYear, Is.EqualTo(2027));
        }

        [Test]
        public void SqliteStore_RoundTripsAYear()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-401k-limits-{Guid.NewGuid():N}.db");
            try
            {
                var limits = Retirement401kLimits.For2026() with
                {
                    ElectiveDeferral = 25_000.50m,
                    CatchUp = 8_000m,
                    HigherCatchUp = 11_250m,
                    RothCatchUpWageThreshold = 150_000m,
                    AnnualAdditions = 72_000m,
                    CompensationCap = 360_000m
                };
                new SQLiteRetirement401kLimitStore(path).Save(limits);

                var loaded = new SQLiteRetirement401kLimitStore(path).Get(2026);

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.ElectiveDeferral, Is.EqualTo(25_000.50m));
                Assert.That(loaded.CatchUp, Is.EqualTo(8_000m));
                Assert.That(loaded.HigherCatchUp, Is.EqualTo(11_250m));
                Assert.That(loaded.RothCatchUpWageThreshold, Is.EqualTo(150_000m));
                Assert.That(loaded.AnnualAdditions, Is.EqualTo(72_000m));
                Assert.That(loaded.CompensationCap, Is.EqualTo(360_000m));
                Assert.That(new SQLiteRetirement401kLimitStore(path).GetLatest()!.TaxYear, Is.EqualTo(2026));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
