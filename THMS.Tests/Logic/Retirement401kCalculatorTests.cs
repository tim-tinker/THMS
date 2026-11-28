using THMS.Domain.Finance.Planning;
using THMS.Logic.Finance.Planning;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class Retirement401kCalculatorTests
    {
        [Test]
        public void For2026_UsesTheIrsDollarLimits()
        {
            var limits = Retirement401kLimits.For2026();

            Assert.That(limits.TaxYear, Is.EqualTo(2026));
            Assert.That(limits.ElectiveDeferral, Is.EqualTo(24_500m));
            Assert.That(limits.CatchUp, Is.EqualTo(8_000m));
            Assert.That(limits.HigherCatchUp, Is.EqualTo(11_250m));
            Assert.That(limits.RothCatchUpWageThreshold, Is.EqualTo(150_000m));
            Assert.That(limits.AnnualAdditions, Is.EqualTo(72_000m));
            Assert.That(limits.CompensationCap, Is.EqualTo(360_000m));
        }

        [Test]
        public void Under50_SharesOneLimitBetweenTraditionalAndRoth()
        {
            var result = Retirement401kCalculator.Calculate(Sample(age: 40, pay: 100_000m) with
            {
                TraditionalShare = 0.40m
            });

            Assert.That(result.CatchUpLimit, Is.EqualTo(0m));
            Assert.That(result.CatchUpMustBeRoth, Is.False);
            Assert.That(result.Maximum.TraditionalAnnual, Is.EqualTo(9_800m));
            Assert.That(result.Maximum.RothAnnual, Is.EqualTo(14_700m));
            Assert.That(result.Maximum.EmployeeAnnual, Is.EqualTo(24_500m));
            Assert.That(result.MaximumTraditional, Is.EqualTo(24_500m));
            Assert.That(result.MaximumRoth, Is.EqualTo(24_500m));
            Assert.That(result.Maximum.TraditionalPercent, Is.EqualTo(9.80m));
            Assert.That(result.Maximum.RothPercent, Is.EqualTo(14.70m));
            Assert.That(result.RuleSummary, Does.Contain("share one employee limit"));
            Assert.That(result.RuleSummary, Does.Contain("Age does not decide"));
        }

        [Test]
        public void Age50_CanKeepCatchUpPretax_WhenPriorWagesDoNotExceedTheThreshold()
        {
            var atThreshold = Retirement401kCalculator.Calculate(Sample(age: 50, pay: 200_000m, priorWages: 150_000m));
            var under = Retirement401kCalculator.Calculate(Sample(age: 50, pay: 200_000m, priorWages: 149_999.99m));

            Assert.That(atThreshold.CatchUpMustBeRoth, Is.False);
            Assert.That(atThreshold.Maximum.TraditionalAnnual, Is.EqualTo(32_500m));
            Assert.That(atThreshold.Maximum.RothAnnual, Is.EqualTo(0m));
            Assert.That(atThreshold.Maximum.TraditionalPercent, Is.EqualTo(16.25m));
            Assert.That(under.CatchUpMustBeRoth, Is.False);
            Assert.That(atThreshold.RuleSummary, Does.Contain("can be pre-tax or Roth"));
        }

        [Test]
        public void Age50_CatchUpMustBeRoth_WhenPriorWagesExceedTheThreshold()
        {
            var result = Retirement401kCalculator.Calculate(Sample(age: 50, pay: 200_000m, priorWages: 150_000.01m));

            Assert.That(result.CatchUpMustBeRoth, Is.True);
            Assert.That(result.CatchUpBlockedWithoutRoth, Is.False);
            Assert.That(result.Maximum.TraditionalAnnual, Is.EqualTo(24_500m));
            Assert.That(result.Maximum.RothAnnual, Is.EqualTo(8_000m));
            Assert.That(result.MaximumTraditional, Is.EqualTo(24_500m));
            Assert.That(result.MaximumRoth, Is.EqualTo(32_500m));
            Assert.That(result.Maximum.TraditionalPercent, Is.EqualTo(12.25m));
            Assert.That(result.Maximum.RothPercent, Is.EqualTo(4.00m));
            Assert.That(result.RuleSummary, Does.Contain("has to be Roth"));
            Assert.That(result.RuleSummary, Does.Contain("can still be pre-tax"));
        }

        [Test]
        public void HigherCatchUp_AppliesAt60Through63_AndNotAt64()
        {
            var age63 = Retirement401kCalculator.Calculate(Sample(age: 63, pay: 200_000m));
            var age64 = Retirement401kCalculator.Calculate(Sample(age: 64, pay: 200_000m));
            var age49 = Retirement401kCalculator.Calculate(Sample(age: 49, pay: 200_000m));

            Assert.That(age63.CatchUpLimit, Is.EqualTo(11_250m));
            Assert.That(age63.Maximum.EmployeeAnnual, Is.EqualTo(35_750m));
            Assert.That(age63.RuleSummary, Does.Contain("60 through 63"));
            Assert.That(age64.CatchUpLimit, Is.EqualTo(8_000m));
            Assert.That(age64.Maximum.EmployeeAnnual, Is.EqualTo(32_500m));
            Assert.That(age49.CatchUpLimit, Is.EqualTo(0m));
            Assert.That(age49.Maximum.EmployeeAnnual, Is.EqualTo(24_500m));
        }

        [Test]
        public void HighEarnerWithoutARothAccount_CannotMakeTheCatchUp()
        {
            var result = Retirement401kCalculator.Calculate(Sample(age: 50, pay: 200_000m, priorWages: 200_000m) with
            {
                PlanOffersRoth = false
            });

            Assert.That(result.CatchUpBlockedWithoutRoth, Is.True);
            Assert.That(result.Maximum.TraditionalAnnual, Is.EqualTo(24_500m));
            Assert.That(result.Maximum.RothAnnual, Is.EqualTo(0m));
            Assert.That(result.MaximumRoth, Is.EqualTo(0m));
            Assert.That(result.RuleSummary, Does.Contain("no Roth account"));
        }

        [Test]
        public void TwoTierMatch_AndTheCompensationCap()
        {
            var match = Retirement401kCalculator.Calculate(Sample(age: 40, pay: 100_000m) with
            {
                FirstMatchRate = 1m,
                FirstMatchOfPay = 0.03m,
                NextMatchRate = 0.50m,
                NextMatchOfPay = 0.02m
            });
            var capped = Retirement401kCalculator.Calculate(Sample(age: 40, pay: 500_000m) with
            {
                FirstMatchRate = 0.50m,
                FirstMatchOfPay = 0.06m
            });

            Assert.That(match.FullMatch.TraditionalAnnual, Is.EqualTo(5_000m));
            Assert.That(match.FullMatch.TraditionalPercent, Is.EqualTo(5.00m));
            Assert.That(match.FullMatch.EmployerMatch, Is.EqualTo(4_000m));
            Assert.That(match.Maximum.EmployerMatch, Is.EqualTo(4_000m));
            Assert.That(match.UnreachableMatch, Is.EqualTo(0m));

            Assert.That(capped.FullMatch.TraditionalAnnual, Is.EqualTo(21_600m));
            Assert.That(capped.FullMatch.EmployerMatch, Is.EqualTo(10_800m));
            Assert.That(capped.FullMatch.TraditionalPercent, Is.EqualTo(4.32m));
            Assert.That(capped.Maximum.TraditionalAnnual, Is.EqualTo(24_500m));
            Assert.That(capped.Maximum.TraditionalPercent, Is.EqualTo(4.90m));
            Assert.That(capped.Maximum.EmployerMatch, Is.EqualTo(10_800m));
            Assert.That(capped.RuleSummary, Does.Contain("match formula counts only"));
        }

        [Test]
        public void PayrollPercent_RoundsUpSoTheElectionReachesTheDollarLimit()
        {
            var result = Retirement401kCalculator.Calculate(Sample(age: 40, pay: 90_000m));

            Assert.That(result.Maximum.TraditionalPercent, Is.EqualTo(27.23m));
            Assert.That(result.Maximum.PercentRoundedUp, Is.True);
            Assert.That(result.RuleSummary, Does.Contain("rounded up"));
        }

        [Test]
        public void Paycheck_PretaxSavesIncomeTax_RothDoesNot_AndFicaIsUnchanged()
        {
            var pretax = Retirement401kCalculator.Calculate(Sample(age: 40, pay: 100_000m) with
            {
                FederalMarginalRate = 0.22m,
                StateMarginalRate = 0.05m
            });
            var split = Retirement401kCalculator.Calculate(Sample(age: 50, pay: 100_000m, priorWages: 200_000m) with
            {
                FederalMarginalRate = 0.24m,
                StateMarginalRate = 0m
            });

            Assert.That(pretax.Maximum.PerPayTraditional, Is.EqualTo(942.31m));
            Assert.That(pretax.Maximum.PerPayRoth, Is.EqualTo(0m));
            Assert.That(pretax.Maximum.PerPayFederalTaxSaved, Is.EqualTo(207.31m));
            Assert.That(pretax.Maximum.PerPayStateTaxSaved, Is.EqualTo(47.12m));
            Assert.That(pretax.Maximum.PerPayTakeHomeReduction, Is.EqualTo(687.88m));

            Assert.That(split.Maximum.PerPayTraditional, Is.EqualTo(942.31m));
            Assert.That(split.Maximum.PerPayRoth, Is.EqualTo(307.69m));
            Assert.That(split.Maximum.PerPayFederalTaxSaved, Is.EqualTo(226.15m));
            Assert.That(split.Maximum.PerPayTakeHomeReduction, Is.EqualTo(1_023.85m));
        }

        [Test]
        public void DeferralCannotExceedPay()
        {
            var result = Retirement401kCalculator.Calculate(Sample(age: 55, pay: 10_000m));

            Assert.That(result.Maximum.EmployeeAnnual, Is.EqualTo(10_000m));
            Assert.That(result.Maximum.TraditionalPercent, Is.EqualTo(100m));
            Assert.That(result.CatchUpLimit, Is.EqualTo(8_000m));
            Assert.That(result.RuleSummary, Does.Contain("may refuse a deferral this high"));
        }

        [Test]
        public void AnnualAdditionsLimit_ReducesTheRegularDeferral_AndLeavesCatchUpAlone()
        {
            var reduced = Retirement401kCalculator.Calculate(Sample(age: 40, pay: 200_000m) with
            {
                FirstMatchRate = 1m,
                FirstMatchOfPay = 0.10m,
                Limits = Retirement401kLimits.For2026() with { AnnualAdditions = 30_000m }
            });
            var withCatchUp = Retirement401kCalculator.Calculate(Sample(age: 50, pay: 200_000m) with
            {
                FirstMatchRate = 1m,
                FirstMatchOfPay = 0.20m,
                Limits = Retirement401kLimits.For2026() with { AnnualAdditions = 30_000m }
            });

            Assert.That(reduced.AnnualAdditionsReducedDeferral, Is.True);
            Assert.That(reduced.Maximum.TraditionalAnnual, Is.EqualTo(15_000m));
            Assert.That(reduced.Maximum.EmployerMatch, Is.EqualTo(15_000m));
            Assert.That(reduced.UnreachableMatch, Is.EqualTo(5_000m));

            Assert.That(withCatchUp.Maximum.TraditionalAnnual, Is.EqualTo(19_000m));
            Assert.That(withCatchUp.Maximum.RothAnnual, Is.EqualTo(0m));
            Assert.That(withCatchUp.Maximum.EmployerMatch, Is.EqualTo(19_000m));
            Assert.That(withCatchUp.CatchUpMustBeRoth, Is.False);
        }

        [Test]
        public void Selection_PricesThePaycheckAgainstTheCurrentContribution()
        {
            var request = Sample(age: 50, pay: 200_000m, priorWages: 150_000.01m) with
            {
                FederalMarginalRate = 0.22m,
                StateMarginalRate = 0m
            };
            var atCurrent = Retirement401kCalculator.QuoteSelection(
                request,
                periodGross: 6_630.77m,
                currentPreTaxPercent: 10m,
                currentRothPercent: 0m,
                selectedPreTaxPercent: 10m,
                selectedRothPercent: 0m,
                selectedCatchUpPercent: 0m);
            var raised = Retirement401kCalculator.QuoteSelection(
                request,
                periodGross: 6_630.77m,
                currentPreTaxPercent: 10m,
                currentRothPercent: 0m,
                selectedPreTaxPercent: 12.25m,
                selectedRothPercent: 0m,
                selectedCatchUpPercent: 4m);

            Assert.That(atCurrent.RegularMaxPercent, Is.EqualTo(12.25m));
            Assert.That(atCurrent.CatchUpMaxPercent, Is.EqualTo(4.00m));
            Assert.That(atCurrent.TakeHomeLowerThanCurrent, Is.EqualTo(0m));
            Assert.That(atCurrent.CatchUpMustBeRoth, Is.True);

            Assert.That(raised.PreTaxWithheld, Is.EqualTo(812.27m));
            Assert.That(raised.RothWithheld, Is.EqualTo(265.23m));
            Assert.That(raised.FederalTaxSaved, Is.EqualTo(178.70m));
            Assert.That(raised.TakeHomeLowerThanCurrent, Is.EqualTo(381.60m));
            Assert.That(raised.RegularExceedsMaximum, Is.False);
            Assert.That(raised.CatchUpExceedsMaximum, Is.False);
        }

        [Test]
        public void Selection_ShowsTheEmployerMatchAndTheContributionIncrease()
        {
            var request = Sample(age: 50, pay: 200_000m) with
            {
                FirstMatchRate = 1m,
                FirstMatchOfPay = 0.06m,
                NextMatchRate = 0.50m,
                NextMatchOfPay = 0.04m
            };
            var full = Retirement401kCalculator.QuoteSelection(
                request, 6_630.77m, 10m, 0m, 12.25m, 0m, 4m);
            var partial = Retirement401kCalculator.QuoteSelection(
                request, 6_630.77m, 4m, 0m, 10m, 0m, 0m);
            var capped = Retirement401kCalculator.QuoteSelection(
                Sample(age: 40, pay: 500_000m) with { FirstMatchRate = 1m, FirstMatchOfPay = 0.06m },
                10_000m, 6m, 0m, 6m, 0m, 0m);

            Assert.That(full.CurrentEmployerMatch, Is.EqualTo(530.47m));
            Assert.That(full.SelectedEmployerMatch, Is.EqualTo(530.47m));
            Assert.That(full.EmployeeContributionIncrease, Is.EqualTo(414.42m));
            Assert.That(full.EmployerContributionIncrease, Is.EqualTo(0m));

            Assert.That(partial.CurrentEmployerMatch, Is.EqualTo(265.23m));
            Assert.That(partial.SelectedEmployerMatch, Is.EqualTo(530.47m));
            Assert.That(partial.EmployeeContributionIncrease, Is.EqualTo(397.85m));
            Assert.That(partial.EmployerContributionIncrease, Is.EqualTo(265.24m));

            Assert.That(capped.CurrentEmployerMatch, Is.EqualTo(432.00m));
        }

        private static Retirement401kRequest Sample(int age, decimal pay, decimal priorWages = 0m) => new()
        {
            AgeAtYearEnd = age,
            AnnualPay = pay,
            PriorYearWages = priorWages,
            PayFrequency = PayFrequency.Biweekly,
            TraditionalShare = 1m,
            FederalMarginalRate = 0.22m,
            StateMarginalRate = 0.05m
        };
    }
}
