using THMS.Domain.Finance.Income;
using THMS.Logic.Finance.Income;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class IncomeEmployerTabsTests
    {
        [Test]
        public void Build_UsesTheLatestYearToDate_AndLeavesWagesBlank()
        {
            var pages = IncomeEmployerTabs.Build(
                [],
                [
                    Slip("Contoso", new DateTime(2026, 1, 15), 2000m, 2000m, 100m),
                    Slip("Contoso", new DateTime(2026, 1, 31), 2100m, 4100m, 250m)
                ]);

            var year = pages.Single().Years.Single();
            Assert.That(pages.Single().Caption, Is.EqualTo("Contoso"));
            Assert.That(year.IsPlaceholder, Is.True);
            Assert.That(year.Wages, Is.Null);
            Assert.That(year.SocialSecurityWages, Is.Null);
            Assert.That(year.Traditional401k, Is.EqualTo(250m));
            Assert.That(year.GrossSoFar, Is.EqualTo(4100m));
        }

        [Test]
        public void Build_KeepsTheFiledW2_AndParksUnassignedSlipsWithTheOnlyEmployer()
        {
            var form = new FormW2
            {
                TaxYear = 2025,
                Employer = "Contoso",
                Wages = 80000m,
                SocialSecurityWages = 78000m,
                Traditional401k = 12000m
            };
            var pages = IncomeEmployerTabs.Build(
                [form],
                [Slip("", new DateTime(2026, 3, 1), 1000m, 1000m, 50m)]);

            var page = pages.Single();
            Assert.That(page.Caption, Is.EqualTo("Contoso"));
            Assert.That(page.Years.Select(year => (year.Year, year.IsPlaceholder, year.Wages, year.Traditional401k)), Is.EqualTo(new (int, bool, decimal?, decimal?)[]
            {
                (2026, true, null, 50m),
                (2025, false, 80000m, 12000m)
            }));
        }

        [Test]
        public void Build_GivesUnassignedPayslipsTheirOwnTabWhenEmployersDiffer()
        {
            var pages = IncomeEmployerTabs.Build(
                [
                    new FormW2 { TaxYear = 2025, Employer = "Contoso", Wages = 1m },
                    new FormW2 { TaxYear = 2025, Employer = "Northwind", Wages = 2m }
                ],
                [Slip("", new DateTime(2026, 1, 1), 10m, 10m, 1m)]);

            Assert.That(pages.Select(page => page.Caption), Is.EqualTo(new[] { "Contoso", "Northwind", "No employer" }));
            Assert.That(pages[^1].Years.Single().GrossSoFar, Is.EqualTo(10m));
        }

        private static Payslip Slip(string employer, DateTime payDate, decimal gross, decimal grossYearToDate, decimal traditionalYearToDate) => new()
        {
            Employer = employer,
            PayDate = payDate,
            Gross = gross,
            GrossYearToDate = grossYearToDate,
            Traditional401kYearToDate = traditionalYearToDate
        };
    }
}
