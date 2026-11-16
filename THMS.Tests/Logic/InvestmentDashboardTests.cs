using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Logic.Finance.Investments;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class InvestmentDashboardTests
    {
        [Test]
        public void MonthlySummary_CarriesPricesForwardAndSumsIncomeByMonth()
        {
            var state = new InvestmentBookState
            {
                AccountId = Guid.NewGuid(),
                OpenedOn = new DateTime(2026, 10, 31)
            };
            state.Actions.Add(Applied(state.AccountId, 1, new DateTime(2026, 10, 31), InvestmentActionKind.Opening, "AGNC", null, 10));
            state.Actions.Add(Applied(state.AccountId, 2, new DateTime(2026, 10, 31), InvestmentActionKind.Opening, "OTHER", null, 5));
            state.Actions.Add(Applied(state.AccountId, 3, new DateTime(2026, 10, 31), InvestmentActionKind.OpeningCash, "", 100, null));
            state.Actions.Add(Applied(state.AccountId, 4, new DateTime(2026, 10, 30), InvestmentActionKind.Dividend, "AGNC", 1.00m, null));
            state.Actions.Add(Applied(state.AccountId, 5, new DateTime(2026, 11, 2), InvestmentActionKind.Dividend, "AGNC", 1.20m, null));
            state.Actions.Add(Applied(state.AccountId, 6, new DateTime(2026, 11, 10), InvestmentActionKind.Interest, "FDRXX", 0.50m, null));
            state.MarketQuotes.Add(Quote("AGNC", 8.65m, new DateTime(2026, 10, 31)));
            state.MarketQuotes.Add(Quote("OTHER", 4m, new DateTime(2026, 11, 14)));

            var months = InvestmentMonthlySummary.Build(state, new DateTime(2026, 11, 15));

            Assert.That(months.Select(month => month.Month), Is.EqualTo(new[]
            {
                new DateTime(2026, 10, 1),
                new DateTime(2026, 11, 1)
            }));

            var october = months[0];
            Assert.That(october.MarketValue, Is.EqualTo(86.50m));
            Assert.That(october.Cash, Is.EqualTo(100m));
            Assert.That(october.Value, Is.EqualTo(186.50m));
            Assert.That(october.Income, Is.EqualTo(1.00m));
            Assert.That(october.Unpriced, Is.EqualTo(1));
            Assert.That(october.CarriedForward, Is.EqualTo(0));

            var november = months[1];
            Assert.That(november.MarketValue, Is.EqualTo(106.50m));
            Assert.That(november.Income, Is.EqualTo(1.70m));
            Assert.That(november.Unpriced, Is.EqualTo(0));
            Assert.That(november.CarriedForward, Is.EqualTo(1));
        }

        [Test]
        public void Compose_AddsAccountsAndKeepsAnAccountWithoutHistoryInTheTotal()
        {
            var accounts = new InMemoryAccountDataStore();
            var books = new InMemoryInvestmentBookStore();
            var tracked = new InvestmentAccount
            {
                Name = "HSA",
                TracksPositions = true,
                BookOpenedOn = new DateTime(2026, 11, 1)
            };
            var stored = new InvestmentAccount
            {
                Name = "Old IRA",
                CashBalance = 20,
                PositionValue = 80
            };
            accounts.UpsertAccount(tracked);
            accounts.UpsertAccount(stored);
            books.UpsertAction(Applied(tracked.Id, 1, new DateTime(2026, 11, 1), InvestmentActionKind.Opening, "AGNC", null, 10));
            books.UpsertAction(Applied(tracked.Id, 2, new DateTime(2026, 11, 3), InvestmentActionKind.Dividend, "AGNC", 2m, null));
            books.UpsertMarketQuote(Quote("AGNC", 10m, new DateTime(2026, 11, 1)));

            var snapshot = new InvestmentDashboardComposer(accounts, books).Compose(new DateTime(2026, 11, 15));

            Assert.That(snapshot.TotalValue, Is.EqualTo(202m));
            Assert.That(snapshot.IncomeThisMonth, Is.EqualTo(2m));
            Assert.That(snapshot.IncomeLastTwelveMonths, Is.EqualTo(2m));
            Assert.That(snapshot.Months.Single().Value, Is.EqualTo(102m));
            Assert.That(snapshot.Months.Single().Prices, Is.EqualTo("Priced this month"));
            Assert.That(snapshot.Note, Does.Contain("no position history"));
        }

        private static InvestmentAction Applied(
            Guid accountId,
            int sequence,
            DateTime date,
            InvestmentActionKind kind,
            string symbol,
            decimal? amount,
            decimal? quantity) =>
            new()
            {
                AccountId = accountId,
                Sequence = sequence,
                RunDate = date,
                Kind = kind,
                Status = InvestmentActionStatus.Applied,
                Symbol = symbol,
                Amount = amount,
                Quantity = quantity,
                ImportKey = $"{accountId:N}|{sequence}"
            };

        private static SecurityMarketQuote Quote(string symbol, decimal price, DateTime date) =>
            new()
            {
                Symbol = symbol,
                UnitValue = price,
                AsOf = date,
                Origin = MarketQuoteOrigin.Import
            };
    }
}
