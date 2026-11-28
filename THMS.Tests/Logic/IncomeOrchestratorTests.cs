using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Income;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class IncomeOrchestratorTests
    {
        [Test]
        public void Save_ListsNewestPayDateFirst_AndDeletesIt()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            income.Save(Slip(new DateTime(2026, 1, 15), 2000m, 2000m));
            income.Save(Slip(new DateTime(2026, 1, 31), 2100m, 4100m));

            var rows = income.GetPayslipRows();

            Assert.That(rows.Select(row => row.PayDate), Is.EqualTo(new[]
            {
                new DateTime(2026, 1, 31),
                new DateTime(2026, 1, 15)
            }));
            Assert.That(rows[0].Gross, Is.EqualTo(2100m));
            Assert.That(rows[0].GrossYearToDate, Is.EqualTo(4100m));

            income.Delete(rows[0].Id);
            Assert.That(income.GetPayslipRows().Select(row => row.PayDate), Is.EqualTo(new[] { new DateTime(2026, 1, 15) }));
        }

        [Test]
        public void Save_RejectsYearToDateBelowThisPaycheck()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            var slip = Slip(new DateTime(2026, 2, 13), 2000m, 1500m);

            var error = Assert.Throws<InvalidOperationException>(() => income.Save(slip));

            Assert.That(error!.Message, Does.Contain("Year-to-date gross"));
            Assert.That(income.GetPayslipRows(), Is.Empty);
        }

        [Test]
        public void SqliteStore_RoundTripsAPayslip()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-payslips-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteIncomeDataStore(path);
                var slip = Slip(new DateTime(2026, 3, 13), 2500.25m, 7500.75m);
                slip.Traditional401k = 200m;
                slip.Traditional401kYearToDate = 600m;
                slip.Roth401k = 50m;
                slip.Roth401kYearToDate = 150m;
                slip.SocialSecurityTax = 142.50m;
                slip.SocialSecurityTaxYearToDate = 427.50m;
                slip.MedicareTax = 33.25m;
                slip.MedicareTaxYearToDate = 99.75m;
                slip.FederalIncomeTax = 410.50m;
                slip.FederalIncomeTaxYearToDate = 1231.50m;
                slip.GroupTermLifeTaxable = 12.25m;
                slip.GroupTermLifeTaxableYearToDate = 36.75m;
                slip.Hsa = 100m;
                slip.HsaYearToDate = 300m;
                store.Save(slip);

                var loaded = new SQLiteIncomeDataStore(path).Get(slip.Id);

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.PayDate, Is.EqualTo(new DateTime(2026, 3, 13)));
                Assert.That(loaded.Gross, Is.EqualTo(2500.25m));
                Assert.That(loaded.GrossYearToDate, Is.EqualTo(7500.75m));
                Assert.That(loaded.Traditional401k, Is.EqualTo(200m));
                Assert.That(loaded.Traditional401kYearToDate, Is.EqualTo(600m));
                Assert.That(loaded.Roth401kYearToDate, Is.EqualTo(150m));
                Assert.That(loaded.SocialSecurityTax, Is.EqualTo(142.50m));
                Assert.That(loaded.MedicareTaxYearToDate, Is.EqualTo(99.75m));
                Assert.That(loaded.FederalIncomeTax, Is.EqualTo(410.50m));
                Assert.That(loaded.FederalIncomeTaxYearToDate, Is.EqualTo(1231.50m));
                Assert.That(loaded.GroupTermLifeTaxable, Is.EqualTo(12.25m));
                Assert.That(loaded.GroupTermLifeTaxableYearToDate, Is.EqualTo(36.75m));
                Assert.That(loaded.HsaYearToDate, Is.EqualTo(300m));
                Assert.That(loaded.Employer, Is.EqualTo("Acme"));
                Assert.That(loaded.PersonId, Is.EqualTo(PersonId));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void Save_ListsW2sByYearThenEmployer_AndDeletesOne()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            income.Save(W2(2024, "Northwind", 70000m, 70000m));
            income.Save(W2(2025, "Contoso", 80000m, 78000m));
            income.Save(W2(2025, "Alpine", 20000m, 20000m));

            var rows = income.GetFormW2Rows();

            Assert.That(rows.Select(row => (row.TaxYear, row.Employer)), Is.EqualTo(new[]
            {
                (2025, "Alpine"),
                (2025, "Contoso"),
                (2024, "Northwind")
            }));
            Assert.That(rows[1].Wages, Is.EqualTo(80000m));
            Assert.That(rows[1].SocialSecurityWages, Is.EqualTo(78000m));

            income.DeleteFormW2(rows[1].Id);
            Assert.That(income.GetFormW2Rows().Select(row => row.Employer), Is.EqualTo(new[] { "Alpine", "Northwind" }));
            Assert.That(income.GetPayslipRows(), Is.Empty);
        }

        [Test]
        public void Save_RejectsAPayslipWithoutAPerson()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            var slip = Slip(new DateTime(2026, 2, 13), 2000m, 2000m);
            slip.PersonId = Guid.Empty;

            var error = Assert.Throws<InvalidOperationException>(() => income.Save(slip));

            Assert.That(error!.Message, Does.Contain("Person"));
            Assert.That(income.GetPayslipRows(), Is.Empty);
        }

        [Test]
        public void Save_RejectsAPayslipWithoutAnEmployer()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());
            var slip = Slip(new DateTime(2026, 2, 13), 2000m, 2000m);
            slip.Employer = "  ";

            var error = Assert.Throws<InvalidOperationException>(() => income.Save(slip));

            Assert.That(error!.Message, Does.Contain("Employer"));
            Assert.That(income.GetPayslipRows(), Is.Empty);
        }

        [Test]
        public void Save_RejectsAW2WithoutAnEmployer()
        {
            var income = new IncomeOrchestrator(new InMemoryIncomeDataStore());

            var error = Assert.Throws<InvalidOperationException>(() => income.Save(W2(2025, "  ", 1m, 1m)));

            Assert.That(error!.Message, Does.Contain("Employer"));
            Assert.That(income.GetFormW2Rows(), Is.Empty);
        }

        [Test]
        public void SqliteStore_RoundTripsAW2()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-w2-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteIncomeDataStore(path);
                var form = W2(2025, "Contoso", 91234.25m, 88000.75m);
                form.Traditional401k = 15000.50m;
                form.Roth401k = 4000.25m;
                form.Hsa = 3200.50m;
                store.Save(form);

                var loaded = new SQLiteIncomeDataStore(path).GetFormW2(form.Id);

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.TaxYear, Is.EqualTo(2025));
                Assert.That(loaded.Employer, Is.EqualTo("Contoso"));
                Assert.That(loaded.Wages, Is.EqualTo(91234.25m));
                Assert.That(loaded.SocialSecurityWages, Is.EqualTo(88000.75m));
                Assert.That(loaded.Traditional401k, Is.EqualTo(15000.50m));
                Assert.That(loaded.Roth401k, Is.EqualTo(4000.25m));
                Assert.That(loaded.Hsa, Is.EqualTo(3200.50m));
                Assert.That(loaded.PersonId, Is.EqualTo(PersonId));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static readonly Guid PersonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        private static Payslip Slip(DateTime payDate, decimal gross, decimal grossYearToDate) => new()
        {
            Employer = "Acme",
            PersonId = PersonId,
            PayDate = payDate,
            Gross = gross,
            GrossYearToDate = grossYearToDate
        };

        private static FormW2 W2(int year, string employer, decimal wages, decimal socialSecurityWages) => new()
        {
            TaxYear = year,
            Employer = employer,
            PersonId = PersonId,
            Wages = wages,
            SocialSecurityWages = socialSecurityWages
        };
    }
}
