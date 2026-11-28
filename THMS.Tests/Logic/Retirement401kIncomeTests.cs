using THMS.Domain.Finance.Income;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Planning;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class Retirement401kIncomeTests
    {
        private static readonly DateTime AsOf = new(2026, 11, 27);

        [Test]
        public void AnnualPay_ProjectsTheLatestGrossAndReadsBox3()
        {
            var latest = Slip(new DateTime(2026, 11, 27), 6_630.77m, 187_250.88m);
            latest.MedicareTax = 190.84m;

            var figures = Retirement401kIncome.Estimate(
                [
                    Slip(new DateTime(2026, 3, 6), 38_233.54m, 63_979.70m),
                    latest
                ],
                [W2(2025, "Schlumberger Technology Corp", 176_100m)],
                PayFrequency.Biweekly,
                AsOf.Year);

            Assert.That(figures.PlanYear, Is.EqualTo(2026));
            Assert.That(figures.AnnualPay, Is.EqualTo(200_512.42m));
            Assert.That(figures.PriorYearWages, Is.EqualTo(176_100m));
            Assert.That(figures.AnnualPayDetail, Does.Contain("2 biweekly checks"));
            Assert.That(figures.AnnualPayDetail, Does.Contain(6_630.77m.ToString("c2")));
            Assert.That(figures.PriorYearWagesDetail, Does.Contain("box 3"));
            Assert.That(figures.PriorYearWagesDetail, Does.Contain("Schlumberger Technology Corp"));
        }

        [Test]
        public void AnnualPay_UsesOnlyThePlanYear()
        {
            var figures = Retirement401kIncome.Estimate(
                [
                    Slip(new DateTime(2025, 12, 25), 9_999m, 9_999m),
                    Slip(new DateTime(2026, 11, 27), 100m, 1_000m)
                ],
                [],
                PayFrequency.Biweekly,
                AsOf.Year);

            Assert.That(figures.AnnualPay, Is.EqualTo(1_200m));
        }

        [Test]
        public void AnnualPay_CountsWeeklyAndMonthlyChecksStillInTheYear()
        {
            var slips = new[] { Slip(new DateTime(2026, 11, 27), 100m, 1_000m) };

            var weekly = Retirement401kIncome.Estimate(slips, [], PayFrequency.Weekly, AsOf.Year);
            var monthly = Retirement401kIncome.Estimate(slips, [], PayFrequency.Monthly, AsOf.Year);

            Assert.That(weekly.AnnualPay, Is.EqualTo(1_400m));
            Assert.That(weekly.AnnualPayDetail, Does.Contain("4 weekly"));
            Assert.That(monthly.AnnualPay, Is.EqualTo(1_100m));
            Assert.That(monthly.AnnualPayDetail, Does.Contain("1 monthly check"));
        }

        [Test]
        public void AnnualPay_IsYearToDateWhenNoChecksRemain()
        {
            var figures = Retirement401kIncome.Estimate(
                [Slip(new DateTime(2026, 12, 25), 100m, 2_600m)],
                [],
                PayFrequency.Biweekly,
                2026);

            Assert.That(figures.AnnualPay, Is.EqualTo(2_600m));
            Assert.That(figures.AnnualPayDetail, Does.Not.Contain("plus"));
        }

        [Test]
        public void AnnualPay_CountsThe15thAndTheLastDayForSemimonthly()
        {
            var figures = Retirement401kIncome.Estimate(
                [Slip(new DateTime(2026, 11, 15), 100m, 1_000m)],
                [],
                PayFrequency.Semimonthly,
                2026);

            Assert.That(figures.AnnualPay, Is.EqualTo(1_300m));
            Assert.That(figures.AnnualPayDetail, Does.Contain("3 semimonthly"));
        }

        [Test]
        public void PriorYearWages_UsesTheLatestFormForThatEmployer()
        {
            var older = W2(2025, "Acme", 100m);
            older.ModifiedOn = new DateTime(2026, 1, 1);
            var newer = W2(2025, "acme", 176_100m);
            newer.ModifiedOn = new DateTime(2026, 2, 1);

            var figures = Retirement401kIncome.Estimate(
                [Slip(AsOf, 100m, 100m)],
                [older, newer, W2(2024, "Acme", 50_000m)],
                PayFrequency.Biweekly,
                AsOf.Year);

            Assert.That(figures.PriorYearWages, Is.EqualTo(176_100m));
        }

        [Test]
        public void PriorYearWages_StaysEmptyWhenEmployersDifferOrTheFormIsMissing()
        {
            var slips = new[] { Slip(AsOf, 100m, 1_000m) };
            var ambiguous = Retirement401kIncome.Estimate(
                slips,
                [W2(2025, "Acme", 10_000m), W2(2025, "Other", 176_100m)],
                PayFrequency.Biweekly,
                AsOf.Year);
            var missing = Retirement401kIncome.Estimate(slips, [], PayFrequency.Biweekly, AsOf.Year);
            var wagesOnly = Retirement401kIncome.Estimate(
                [],
                [W2(2025, "Acme", 176_100m)],
                PayFrequency.Biweekly,
                AsOf.Year);

            Assert.That(ambiguous.PriorYearWages, Is.Null);
            Assert.That(ambiguous.AnnualPay, Is.EqualTo(1_200m));
            Assert.That(ambiguous.PriorYearWagesDetail, Does.Contain("More than one employer"));
            Assert.That(missing.PriorYearWages, Is.Null);
            Assert.That(wagesOnly.AnnualPay, Is.Null);
            Assert.That(wagesOnly.PriorYearWages, Is.EqualTo(176_100m));
            Assert.That(wagesOnly.PlanYear, Is.EqualTo(2026));
        }

        [Test]
        public void FutureYear_ProjectsTheLatestGrossAndUsesThatYearsPriorWages()
        {
            var latest = Slip(new DateTime(2026, 11, 27), 6_630.77m, 187_250.88m);
            latest.Traditional401k = 663.08m;

            var figures = Retirement401kIncome.Estimate(
                [latest],
                [
                    W2(2026, "Schlumberger Technology Corp", 184_500m),
                    W2(2025, "Schlumberger Technology Corp", 176_100m)
                ],
                PayFrequency.Biweekly,
                2027);

            Assert.That(figures.AnnualPay, Is.EqualTo(172_400.02m));
            Assert.That(figures.PriorYearWages, Is.EqualTo(184_500m));
            Assert.That(figures.CurrentPreTaxPercent, Is.EqualTo(10.00m));
            Assert.That(figures.CurrentRothPercent, Is.EqualTo(0m));
            Assert.That(figures.PeriodGross, Is.EqualTo(6_630.77m));
            Assert.That(figures.CurrentContributionDetail, Does.Contain("Catch-up is not listed separately"));
        }

        private static Payslip Slip(DateTime payDate, decimal gross, decimal yearToDate) => new()
        {
            PayDate = payDate,
            Gross = gross,
            GrossYearToDate = yearToDate
        };

        private static FormW2 W2(int year, string employer, decimal socialSecurityWages) => new()
        {
            TaxYear = year,
            Employer = employer,
            SocialSecurityWages = socialSecurityWages
        };
    }
}
