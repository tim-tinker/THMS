using THMS.Domain.Finance.Accounts;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class LeaseContractTests
    {
        [Test]
        public void MoneyFactor_IsRentChargeDividedByTermTimesCapCostPlusResidual()
        {
            const decimal rentCharge = 3678.72m;
            const decimal adjusted = 48522.33m;
            const decimal residual = 23744.70m;
            const int term = 39;

            var factor = LeaseAccount.MoneyFactorFromRentCharge(rentCharge, adjusted, residual, term);

            Assert.That(factor, Is.Not.Null);
            var reconstructed = factor!.Value * term * (adjusted + residual);
            Assert.That(reconstructed, Is.EqualTo(rentCharge).Within(0.0000001m));
            Assert.That(factor.Value * LeaseAccount.AprMultiplier, Is.EqualTo(3.1325871456m).Within(0.0000001m));
            Assert.That(adjusted - residual, Is.EqualTo(24777.63m));
            Assert.That(adjusted - residual + rentCharge, Is.EqualTo(28456.35m));
        }

        [Test]
        public void MoneyFactor_NeedsRentChargeAdjustedCostAndTerm()
        {
            Assert.That(LeaseAccount.MoneyFactorFromRentCharge(0m, 48522.33m, 23744.70m, 39), Is.Null);
            Assert.That(LeaseAccount.MoneyFactorFromRentCharge(3678.72m, 0m, 23744.70m, 39), Is.Null);
            Assert.That(LeaseAccount.MoneyFactorFromRentCharge(3678.72m, 48522.33m, 23744.70m, 0), Is.Null);
        }
    }
}
