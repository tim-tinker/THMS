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
            Assert.That(portfolio.MarketValue, Is.EqualTo(0m));
            Assert.That(portfolio.AccountValue, Is.EqualTo(112.70m));
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
