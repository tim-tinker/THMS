using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Planning;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class PromotionPaymentPlannerTests
    {
        [Test]
        public void RemainingPeriods_CountsElapsedTimeUntilDeadline()
        {
            var asOf = new DateTime(2026, 9, 20);
            var inSixMonths = asOf.AddMonths(6);

            Assert.That(PromotionPaymentPlanner.RemainingPeriods(asOf, inSixMonths, RecurrenceFrequency.Weekly), Is.EqualTo(26));
            Assert.That(PromotionPaymentPlanner.RemainingPeriods(asOf, inSixMonths, RecurrenceFrequency.BiWeekly), Is.EqualTo(13));
            Assert.That(PromotionPaymentPlanner.RemainingPeriods(asOf, inSixMonths, RecurrenceFrequency.Monthly), Is.EqualTo(6));
            Assert.That(PromotionPaymentPlanner.RemainingPeriods(asOf, asOf.AddDays(-1), RecurrenceFrequency.Monthly), Is.EqualTo(1));
        }

        [Test]
        public void RequiredThisPayment_SplitsByFrequencyAndKeepsEqualPaymentsMonthly()
        {
            var asOf = new DateTime(2026, 9, 20);
            var deadline = asOf.AddMonths(6);
            var lump = new PromotionalBalance
            {
                CurrentBalance = 1000m,
                Deadline = deadline,
                Type = PromoType.LumpSum
            };
            var equal = new PromotionalBalance
            {
                CurrentBalance = 1000m,
                Deadline = deadline,
                Type = PromoType.EqualPayments
            };

            Assert.That(PromotionPaymentPlanner.RequiredThisPayment(lump, asOf, RecurrenceFrequency.Weekly), Is.EqualTo(38.47m));
            Assert.That(PromotionPaymentPlanner.RequiredThisPayment(lump, asOf, RecurrenceFrequency.BiWeekly), Is.EqualTo(76.93m));
            Assert.That(PromotionPaymentPlanner.RequiredThisPayment(lump, asOf, RecurrenceFrequency.Monthly), Is.EqualTo(166.67m));
            Assert.That(PromotionPaymentPlanner.RequiredThisPayment(equal, asOf, RecurrenceFrequency.Weekly), Is.EqualTo(166.67m));
        }

        [Test]
        public void Plan_DefaultsToMonthlyAndMakesLumpSumFrequencyEditable()
        {
            var asOf = new DateTime(2026, 9, 20);
            var planned = PromotionPaymentPlanner.Plan(
                [
                    new PromotionalBalance
                    {
                        CurrentBalance = 1000m,
                        Deadline = asOf.AddMonths(6),
                        Type = PromoType.LumpSum
                    },
                    new PromotionalBalance
                    {
                        CurrentBalance = 600m,
                        Deadline = asOf.AddMonths(6),
                        Type = PromoType.EqualPayments
                    }
                ],
                asOf);

            Assert.That(planned[0].Frequency, Is.EqualTo(RecurrenceFrequency.Monthly));
            Assert.That(planned[0].FrequencyEditable, Is.True);
            Assert.That(planned[0].RequiredAmount, Is.EqualTo(166.67m));
            Assert.That(planned[1].Frequency, Is.EqualTo(RecurrenceFrequency.Monthly));
            Assert.That(planned[1].FrequencyEditable, Is.False);
            Assert.That(planned[1].RequiredAmount, Is.EqualTo(100.00m));
        }

        [Test]
        public void NonPromotionBalance_SubtractsPromoBalancesFromStatement()
        {
            var promotions = new[]
            {
                new PromotionalBalance { CurrentBalance = 400m },
                new PromotionalBalance { CurrentBalance = 150m }
            };

            Assert.That(PromotionPaymentPlanner.NonPromotionBalance(1000m, promotions), Is.EqualTo(450m));
            Assert.That(PromotionPaymentPlanner.StatementBalanceOf(new CreditCardStatement { StatementBalance = 17672.62m }), Is.EqualTo(17672.62m));
            Assert.That(PromotionPaymentPlanner.StatementBalanceOf(new LoanStatement { StatementBalance = 5000m }), Is.EqualTo(5000m));
            Assert.That(PromotionPaymentPlanner.StatementBalanceOf(null), Is.Null);
        }
    }
}
