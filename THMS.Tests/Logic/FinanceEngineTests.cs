using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Finance.Billing;
using THMS.Domain.Finance.Loans;
using THMS.Logic.Finance;
using THMS.Logic.Finance.Model;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class EvRoiEngineTests
    {
        [Test]
        public void ComputeAll_JoinsLoanWhenPresent_AndMarksPartialWhenMissing()
        {
            var summaries = new[]
            {
                new MonthlyEnergySummary
                {
                    Year = 2026,
                    Month = 1,
                    SolarAvoidedCost = 10,
                    BatteryValue = 4,
                    GridCost = 2,
                    CommercialChargeCost = 1
                },
                new MonthlyEnergySummary { Year = 2026, Month = 2 }
            };
            var loans = new[]
            {
                new LoanCashFlow
                {
                    Date = new DateTime(2026, 1, 15),
                    PaymentAmount = 100,
                    InterestPaid = 20,
                    PrincipalPaid = 80
                }
            };

            var results = new EvRoiEngine(summaries, loans).ComputeAll();
            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results.First().LoanPayment, Is.EqualTo(100));
            Assert.That(results.First().IsPartial, Is.False);
            Assert.That(results.Last().IsPartial, Is.True);
            Assert.That(results.Last().LoanPayment, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class SolarRoiEngineTests
    {
        [Test]
        public void ComputeAll_UsesBillingExportCredit_AndPartialWhenEitherMissing()
        {
            var summaries = new[]
            {
                new MonthlyEnergySummary { Year = 2026, Month = 3, SolarAvoidedCost = 8, BatteryValue = 2 },
                new MonthlyEnergySummary { Year = 2026, Month = 4, SolarAvoidedCost = 1 }
            };
            var loans = new[]
            {
                new LoanCashFlow
                {
                    Date = new DateTime(2026, 3, 1),
                    PaymentAmount = 50,
                    InterestPaid = 5,
                    PrincipalPaid = 45
                }
            };
            var bills = new[]
            {
                new ElectricUtilityBill
                {
                    StartDate = new DateTime(2026, 3, 1),
                    ExportCredit = 7.5m
                }
            };

            var results = new SolarRoiEngine(summaries, loans, bills).ComputeAll();
            Assert.That(results.First().GridExportCredit, Is.EqualTo(7.5m));
            Assert.That(results.First().IsPartial, Is.False);
            Assert.That(results.Last().IsPartial, Is.True);
            Assert.That(results.Last().GridExportCredit, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class LoanAmortizationEngineTests
    {
        [Test]
        public void GenerateSchedule_PaysDownPrincipalOverTerm()
        {
            var schedule = new LoanAmortizationEngine().GenerateSchedule(
                principal: 1200m,
                annualInterestRate: 0.12m,
                termMonths: 12,
                monthlyPayment: 106.62m);

            Assert.That(schedule, Is.Not.Empty);
            Assert.That(schedule.First().InterestPaid, Is.GreaterThan(0));
            Assert.That(schedule.Last().RemainingPrincipal, Is.EqualTo(0m).Within(1.00m));
        }

        [Test]
        public void GenerateSchedule_ClampsNegativePrincipal_WhenPaymentIsTooSmall()
        {
            var schedule = new LoanAmortizationEngine().GenerateSchedule(
                principal: 1000m,
                annualInterestRate: 0.24m,
                termMonths: 2,
                monthlyPayment: 1m);

            Assert.That(schedule, Has.Count.EqualTo(2));
            Assert.That(schedule.First().PrincipalPaid, Is.EqualTo(0));
            Assert.That(schedule.First().RemainingPrincipal, Is.EqualTo(1000m));
        }

        [Test]
        public void GenerateSchedule_AppliesLumpSum_AndStopsWhenPaidOff()
        {
            var firstPaymentDate = DateTime.Today.AddMonths(1);
            var lumps = new Dictionary<DateTime, decimal>
            {
                [firstPaymentDate] = 5000m
            };

            var schedule = new LoanAmortizationEngine().GenerateSchedule(
                principal: 1000m,
                annualInterestRate: 0.06m,
                termMonths: 24,
                monthlyPayment: 50m,
                lumps);

            Assert.That(schedule, Has.Count.EqualTo(1));
            Assert.That(schedule.Single().HasLumpSumPayment, Is.True);
            Assert.That(schedule.Single().RemainingPrincipal, Is.EqualTo(0));
            Assert.That(schedule.Single().PaymentAmount, Is.GreaterThan(schedule.Single().InterestPaid));
        }

        [Test]
        public void GenerateSchedule_IgnoresLumpSumOnOtherDates()
        {
            var lumps = new Dictionary<DateTime, decimal>
            {
                [DateTime.Today] = 100m
            };

            var schedule = new LoanAmortizationEngine().GenerateSchedule(
                200m, 0m, 2, 100m, lumps);

            Assert.That(schedule.All(s => !s.HasLumpSumPayment), Is.True);
            Assert.That(schedule.Last().RemainingPrincipal, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class HomeChargeCostAttributionEngineTests
    {
        [Test]
        public void ComputeMonthlyCost_ThrowsWhenNoBill()
        {
            var engine = new HomeChargeCostAttributionEngine(
                new InMemoryEnergyDataStore(),
                new InMemoryFinanceDataStore());

            Assert.That(
                () => engine.ComputeMonthlyCost(new DateTime(2020, 1, 1), new DateTime(2020, 1, 31)),
                Throws.InvalidOperationException.With.Message.Contain("No utility bill"));
        }

        [Test]
        public void ComputeMonthlyCost_AttributesEvShareOfGrid()
        {
            var energy = new InMemoryEnergyDataStore();
            var finance = new InMemoryFinanceDataStore();
            var start = new DateTime(2026, 9, 1);
            var end = new DateTime(2026, 9, 30);
            var ts = new DateTime(2026, 9, 10, 12, 0, 0);

            finance.UpsertElectricUtilityBill(new ElectricUtilityBill
            {
                Id = Guid.NewGuid(),
                StartDate = start,
                EndDate = end,
                BaseCharge = 10,
                EnergyCharge = 90,
                DeliveryCharge = 0,
                ExportCredit = 0,
                KwhUsage = 100
            });
            energy.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = ts,
                KiloWattHours = 2000
            });
            energy.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = ts.AddHours(1),
                KiloWattHours = 100
            });
            energy.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = ts,
                ImportedFromGridWh = 1500
            });

            var engine = new HomeChargeCostAttributionEngine();
            engine.SetStores(finance, energy);
            var summary = engine.ComputeMonthlyCost(start, end);

            Assert.That(summary.EvGridKwh, Is.EqualTo(1.5m));
            Assert.That(summary.EvCost, Is.EqualTo(100m));
            Assert.That(summary.Start, Is.EqualTo(start));
        }

        [Test]
        public void ComputeMonthlyCost_ZeroHomeGrid_YieldsZeroShare()
        {
            var energy = new InMemoryEnergyDataStore();
            var finance = new InMemoryFinanceDataStore();
            var start = new DateTime(2026, 10, 1);
            var end = new DateTime(2026, 10, 31);

            finance.UpsertElectricUtilityBill(new ElectricUtilityBill
            {
                Id = Guid.NewGuid(),
                StartDate = start,
                EndDate = end,
                EnergyCharge = 10
            });
            energy.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = start.AddDays(1),
                KiloWattHours = 5
            });

            var summary = new HomeChargeCostAttributionEngine(energy, finance)
                .ComputeMonthlyCost(start, end);

            Assert.That(summary.EvGridKwh, Is.EqualTo(0));
            Assert.That(summary.EvCost, Is.EqualTo(0));
        }
    }
}
