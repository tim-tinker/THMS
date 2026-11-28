using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class Retirement401kMatchOrchestratorTests
    {
        [Test]
        public void Save_ReplacesTheStoredFormula()
        {
            var match = new Retirement401kMatchOrchestrator(new InMemoryRetirement401kMatchStore());
            Assert.That(match.Get(), Is.Null);

            match.Save(new Retirement401kMatchFormula
            {
                FirstMatchRate = 1m,
                FirstMatchOfPay = 0.06m,
                NextMatchRate = 0.50m,
                NextMatchOfPay = 0.04m
            });
            match.Save(new Retirement401kMatchFormula
            {
                FirstMatchRate = 0.50m,
                FirstMatchOfPay = 0.06m,
                PlanOffersRoth = false
            });

            var loaded = match.Get();
            Assert.That(loaded!.FirstMatchRate, Is.EqualTo(0.50m));
            Assert.That(loaded.FirstMatchOfPay, Is.EqualTo(0.06m));
            Assert.That(loaded.NextMatchRate, Is.EqualTo(0m));
            Assert.That(loaded.PlanOffersRoth, Is.False);
        }

        [Test]
        public void Save_RejectsABandWiderThanPay()
        {
            var match = new Retirement401kMatchOrchestrator(new InMemoryRetirement401kMatchStore());

            Assert.Throws<InvalidOperationException>(() => match.Save(new Retirement401kMatchFormula
            {
                FirstMatchOfPay = 1.5m
            }));
        }

        [Test]
        public void Save_KeepsASeparateFormulaForEachPersonAndEmployer()
        {
            var match = new Retirement401kMatchOrchestrator(new InMemoryRetirement401kMatchStore());
            var alex = Guid.NewGuid();
            var blair = Guid.NewGuid();
            match.Save(new Retirement401kMatchFormula { FirstMatchRate = 1m, FirstMatchOfPay = 0.03m });
            match.Save(alex, "Contoso", new Retirement401kMatchFormula { FirstMatchRate = 1m, FirstMatchOfPay = 0.06m });
            match.Save(blair, "contoso", new Retirement401kMatchFormula { FirstMatchRate = 0.50m, FirstMatchOfPay = 0.06m });

            Assert.That(match.Get(alex, "Contoso")!.FirstMatchOfPay, Is.EqualTo(0.06m));
            Assert.That(match.Get(alex, "Contoso")!.FirstMatchRate, Is.EqualTo(1m));
            Assert.That(match.Get(blair, "Contoso")!.FirstMatchRate, Is.EqualTo(0.50m));
            Assert.That(match.Get(blair, "Northwind")!.FirstMatchOfPay, Is.EqualTo(0.03m));
        }

        [Test]
        public void SqliteStore_RoundTripsTheFormula()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-401k-match-{Guid.NewGuid():N}.db");
            try
            {
                new SQLiteRetirement401kMatchStore(path).Save(new Retirement401kMatchFormula
                {
                    FirstMatchRate = 1m,
                    FirstMatchOfPay = 0.50m,
                    NextMatchRate = 0.25m,
                    NextMatchOfPay = 0.125m,
                    PlanOffersRoth = false
                });

                var loaded = new SQLiteRetirement401kMatchStore(path).Get();

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.FirstMatchRate, Is.EqualTo(1m));
                Assert.That(loaded.FirstMatchOfPay, Is.EqualTo(0.50m));
                Assert.That(loaded.NextMatchRate, Is.EqualTo(0.25m));
                Assert.That(loaded.NextMatchOfPay, Is.EqualTo(0.125m));
                Assert.That(loaded.PlanOffersRoth, Is.False);
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
