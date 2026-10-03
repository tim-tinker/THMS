using System.Globalization;
using THMS.Logic.Finance.Transactions;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class LedgerEntryDateTests
    {
        [TestCase("9/28/26", 2026, 9, 28)]
        [TestCase("9/28/2026", 2026, 9, 28)]
        [TestCase("09/28/2026", 2026, 9, 28)]
        [TestCase("10/2/2026", 2026, 10, 2)]
        public void TryParse_KeepsTheTypedMonthDayAndYear(string text, int year, int month, int day)
        {
            Assert.That(
                LedgerEntryDate.TryParse(text, new CultureInfo("en-US"), out var date),
                Is.True);
            Assert.That(date, Is.EqualTo(new DateTime(year, month, day)));
        }

        [Test]
        public void TryParse_RejectsABlankDate()
        {
            Assert.That(LedgerEntryDate.TryParse("  ", out _), Is.False);
        }
    }
}
