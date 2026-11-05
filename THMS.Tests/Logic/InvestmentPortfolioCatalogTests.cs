using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Logic.Finance.Investments;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class InvestmentPortfolioCatalogTests
    {
        [Test]
        public void Portfolios_ListInvestmentAccountsAndTheirSecurities()
        {
            var accounts = new InMemoryAccountDataStore();
            var book = new InMemoryInvestmentBookStore();
            var hsa = new InvestmentAccount
            {
                Name = "HSA",
                Institution = "Fidelity",
                WebsiteUrl = "https://www.fidelity.com",
                AccountNumber = "257460702",
                Type = AccountType.Investment,
                TracksPositions = true,
                BookOpenedOn = new DateTime(2026, 10, 31)
            };
            accounts.UpsertAccount(hsa);
            accounts.UpsertAccount(new BankAccount { Name = "Checking" });

            book.UpsertSecurity(new InvestmentSecurity { Symbol = "AGNC", Name = "AGNC INVT CORP" });
            book.UpsertSecurity(new InvestmentSecurity { Symbol = "XYZ", Name = "Sold Co" });
            book.UpsertSecurity(new InvestmentSecurity { Symbol = "FDRXX", Name = "HELD IN MONEY MARKET", IsSweep = true });
            book.UpsertQuote(new SecurityQuote
            {
                AccountId = hsa.Id,
                Symbol = "AGNC",
                AsOf = new DateTime(2026, 10, 30),
                AnnualIncomePerShare = 1.44m
            });

            Add(book, hsa.Id, 1, new DateTime(2026, 10, 31), InvestmentActionKind.OpeningCash, "", 100m, null, null);
            Add(book, hsa.Id, 2, new DateTime(2026, 10, 31), InvestmentActionKind.Opening, "AGNC", null, 10m, 9.83m);
            Add(book, hsa.Id, 3, new DateTime(2026, 10, 31), InvestmentActionKind.Opening, "XYZ", null, 2m, 5m);
            Add(book, hsa.Id, 4, new DateTime(2026, 11, 2), InvestmentActionKind.Sell, "XYZ", 11m, 2m, 5.5m);
            Add(book, hsa.Id, 5, new DateTime(2026, 11, 2), InvestmentActionKind.Dividend, "AGNC", 1.20m, null, null);
            Add(book, hsa.Id, 6, new DateTime(2026, 11, 2), InvestmentActionKind.Dividend, "FDRXX", 0.50m, null, null);
            Add(book, hsa.Id, 7, new DateTime(2026, 11, 3), InvestmentActionKind.Unknown, "AGNC", null, null, null, InvestmentActionStatus.Review, "Mystery");

            var catalog = new InvestmentPortfolioCatalog(accounts, book);
            var portfolio = catalog.Portfolios().Single();
            Assert.That(portfolio.Name, Is.EqualTo("HSA"));
            Assert.That(portfolio.WebsiteUrl, Is.EqualTo("https://www.fidelity.com"));
            Assert.That(portfolio.Cash, Is.EqualTo(112.70m));
            Assert.That(portfolio.MarketValue, Is.Null);
            Assert.That(portfolio.AccountValue, Is.Null);
            Assert.That(portfolio.PricesIncomplete, Is.True);
            Assert.That(portfolio.PricesAsOfText, Is.EqualTo("No price"));
            Assert.That(portfolio.EstimatedAnnualIncome, Is.EqualTo(14.40m));

            var securities = catalog.Securities(hsa.Id);
            Assert.That(securities.Select(row => row.Symbol), Is.EqualTo(new[] { "AGNC", "XYZ" }));
            Assert.That(securities[0].Shares, Is.EqualTo(10m));
            Assert.That(securities[0].Name, Is.EqualTo("AGNC INVT CORP"));
            Assert.That(securities[1].Shares, Is.EqualTo(0m));
            Assert.That(securities[1].Name, Is.EqualTo("Sold Co"));

            var actions = catalog.Actions(hsa.Id, "AGNC");
            Assert.That(actions.Select(action => action.Kind), Is.EqualTo(new[]
            {
                InvestmentActionKind.Opening,
                InvestmentActionKind.Dividend,
                InvestmentActionKind.Unknown
            }));
            Assert.That(actions[^1].Status, Is.EqualTo(InvestmentActionStatus.Review));
        }

        [Test]
        public void SharedQuote_PricesEveryPortfolioAndShowsTheOldestAsOf()
        {
            var accounts = new InMemoryAccountDataStore();
            var book = new InMemoryInvestmentBookStore();
            var hsa = Investment("HSA", new DateTime(2026, 10, 31));
            hsa.QuoteAccess = QuoteAccess.ImportOnly;
            var ira = Investment("IRA", new DateTime(2026, 11, 1));
            accounts.UpsertAccount(hsa);
            accounts.UpsertAccount(ira);
            book.UpsertSecurity(new InvestmentSecurity { Symbol = "AGNC", Name = "AGNC", LookupKey = "AGNC" });
            book.UpsertSecurity(new InvestmentSecurity { Symbol = "999999999", Name = "Stable value" });
            book.UpsertMarketQuote(new SecurityMarketQuote
            {
                Symbol = "AGNC",
                UnitValue = 8m,
                AsOf = new DateTime(2026, 11, 2),
                Origin = MarketQuoteOrigin.Import
            });
            book.UpsertMarketQuote(new SecurityMarketQuote
            {
                Symbol = "AGNC",
                UnitValue = 9m,
                AsOf = new DateTime(2026, 11, 2),
                Origin = MarketQuoteOrigin.Service
            });
            book.UpsertMarketQuote(new SecurityMarketQuote
            {
                Symbol = "999999999",
                UnitValue = 10m,
                AsOf = new DateTime(2026, 11, 2),
                Origin = MarketQuoteOrigin.Import
            });
            book.UpsertMarketQuote(new SecurityMarketQuote
            {
                Symbol = "AGNC",
                UnitValue = 11m,
                AsOf = new DateTime(2026, 12, 1),
                Origin = MarketQuoteOrigin.Service
            });
            Add(book, hsa.Id, 1, new DateTime(2026, 10, 31), InvestmentActionKind.Opening, "AGNC", null, 10m, 9m);
            Add(book, hsa.Id, 2, new DateTime(2026, 10, 31), InvestmentActionKind.Opening, "999999999", null, 2m, 10m);
            Add(book, ira.Id, 1, new DateTime(2026, 11, 1), InvestmentActionKind.Opening, "AGNC", null, 4m, 9m);

            var catalog = new InvestmentPortfolioCatalog(accounts, book);
            var portfolios = catalog.Portfolios();
            var hsaRow = portfolios.Single(row => row.Name == "HSA");
            var iraRow = portfolios.Single(row => row.Name == "IRA");
            Assert.That(hsaRow.PriceSource, Is.EqualTo("Import only"));
            Assert.That(hsaRow.MarketValue, Is.EqualTo(100m));
            Assert.That(hsaRow.PricesAsOf, Is.EqualTo(new DateTime(2026, 11, 2)));
            Assert.That(hsaRow.PricesIncomplete, Is.False);
            Assert.That(iraRow.PriceSource, Is.EqualTo("Queryable"));
            Assert.That(iraRow.MarketValue, Is.EqualTo(44m));
            Assert.That(iraRow.PricesAsOf, Is.EqualTo(new DateTime(2026, 12, 1)));

            var securities = catalog.Securities(hsa.Id);
            Assert.That(securities.Single(row => row.Symbol == "AGNC").Price, Is.EqualTo(8m));
            Assert.That(securities.Single(row => row.Symbol == "999999999").Price, Is.EqualTo(10m));
            Assert.That(book.GetMarketQuotes(), Has.Count.EqualTo(4));
        }

        private static InvestmentAccount Investment(string name, DateTime opened) => new()
        {
            Name = name,
            Type = AccountType.Investment,
            TracksPositions = true,
            BookOpenedOn = opened
        };

        private static void Add(
            InMemoryInvestmentBookStore book,
            Guid accountId,
            int sequence,
            DateTime date,
            InvestmentActionKind kind,
            string symbol,
            decimal? amount,
            decimal? quantity,
            decimal? price,
            InvestmentActionStatus status = InvestmentActionStatus.Applied,
            string? note = null)
        {
            book.UpsertAction(new InvestmentAction
            {
                AccountId = accountId,
                Sequence = sequence,
                RunDate = date,
                ActionText = kind.ToString(),
                Symbol = symbol,
                Kind = kind,
                Status = status,
                Amount = amount,
                Quantity = quantity,
                Price = price,
                Note = note,
                ImportKey = $"{sequence}|{symbol}|{kind}"
            });
        }
    }
}
