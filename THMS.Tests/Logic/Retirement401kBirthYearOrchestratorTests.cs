using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class Retirement401kBirthYearOrchestratorTests
    {
        [Test]
        public void AgeAtYearEnd_IsTheTaxYearMinusTheBirthYear()
        {
            Assert.That(Retirement401kBirthYearOrchestrator.AgeAtYearEnd(1976, 2026), Is.EqualTo(50));
            Assert.That(Retirement401kBirthYearOrchestrator.AgeAtYearEnd(1976, 2027), Is.EqualTo(51));
            Assert.That(Retirement401kBirthYearOrchestrator.AgeAtYearEnd(null, 2026), Is.EqualTo(0));
            Assert.That(Retirement401kBirthYearOrchestrator.AgeAtYearEnd(2026, 2026), Is.EqualTo(0));
        }

        [Test]
        public void Save_ReplacesTheBirthYear()
        {
            var birthYear = new Retirement401kBirthYearOrchestrator(new InMemoryRetirement401kBirthYearStore());
            Assert.That(birthYear.Get(), Is.Null);

            birthYear.Save(new Retirement401kBirthYear { Year = 1970 });
            birthYear.Save(new Retirement401kBirthYear { Year = 1976 });

            Assert.That(birthYear.Get()!.Year, Is.EqualTo(1976));
        }

        [Test]
        public void Save_RejectsAYearOutside1900Through2100()
        {
            var birthYear = new Retirement401kBirthYearOrchestrator(new InMemoryRetirement401kBirthYearStore());

            Assert.Throws<InvalidOperationException>(() => birthYear.Save(new Retirement401kBirthYear { Year = 1899 }));
        }

        [Test]
        public void SqliteStore_RoundTripsTheBirthYear()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-401k-birth-{Guid.NewGuid():N}.db");
            try
            {
                new SQLiteRetirement401kBirthYearStore(path).Save(new Retirement401kBirthYear { Year = 1976 });

                var loaded = new SQLiteRetirement401kBirthYearStore(path).Get();

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.Year, Is.EqualTo(1976));
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
