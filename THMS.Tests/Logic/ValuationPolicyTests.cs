using THMS.Domain.Assets;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class ValuationPolicyTests
    {
        [Test]
        public void Choose_UsesTheMiddlePrivatePartyAndReplacesAnOlderQuoteFromTheSameSite()
        {
            var car = new VehicleAsset();
            var chosen = ValuationPolicy.Choose(car,
            [
                Quote(ValuationTypes.PrivateParty, 10000, new DateTime(2026, 10, 1), "kbb"),
                Quote(ValuationTypes.PrivateParty, 28000, new DateTime(2026, 11, 20), "KBB"),
                Quote(ValuationTypes.PrivateParty, 30000, new DateTime(2026, 11, 20), "Edmunds"),
                Quote(ValuationTypes.PrivateParty, 36000, new DateTime(2026, 11, 20), "NADA"),
                Quote(ValuationTypes.TradeIn, 20000, new DateTime(2026, 11, 24), "KBB")
            ], new DateTime(2026, 11, 24));

            Assert.That(chosen, Is.Not.Null);
            Assert.That(chosen!.ValueType, Is.EqualTo(ValuationTypes.PrivateParty));
            Assert.That(chosen.Amount, Is.EqualTo(30000m));
            Assert.That(chosen.Figures, Has.Count.EqualTo(3));
        }

        [Test]
        public void Choose_AveragesTheTwoMiddleQuotes()
        {
            var car = new VehicleAsset();
            var chosen = ValuationPolicy.Choose(car,
            [
                Quote(ValuationTypes.PrivateParty, 28000, new DateTime(2026, 11, 20), "Kelley Blue Book"),
                Quote(ValuationTypes.PrivateParty, 30000, new DateTime(2026, 11, 20), "Edmunds"),
                Quote(ValuationTypes.PrivateParty, 32000, new DateTime(2026, 11, 20), "NADA"),
                Quote(ValuationTypes.PrivateParty, 34000, new DateTime(2026, 11, 18), "CarGurus")
            ], new DateTime(2026, 11, 24));

            Assert.That(chosen!.Amount, Is.EqualTo(31000m));
            Assert.That(chosen.Describe(new DateTime(2026, 11, 24)), Does.Contain("Private party median of 4"));
        }

        [Test]
        public void Choose_KeepsTheLatestPropertyAppraisalInsteadOfAMedian()
        {
            var house = new PropertyAsset();
            var chosen = ValuationPolicy.Choose(house,
            [
                Quote(ValuationTypes.TotalMarket, 400000, new DateTime(2026, 8, 1), "Fort Bend CAD"),
                Quote(ValuationTypes.TotalMarket, 100000, new DateTime(2026, 7, 1), "Tax notice"),
                Quote(ValuationTypes.Assessed, 350000, new DateTime(2026, 11, 1), "Fort Bend CAD")
            ], new DateTime(2026, 11, 24));

            Assert.That(chosen!.ValueType, Is.EqualTo(ValuationTypes.TotalMarket));
            Assert.That(chosen.Amount, Is.EqualTo(400000m));
        }

        private static AssetValuation Quote(string type, decimal amount, DateTime asOf, string source) =>
            new()
            {
                ValueType = type,
                Amount = amount,
                AsOf = asOf,
                Source = source
            };
    }
}
