using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Logic.Finance.Investments;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class InvestmentFileMappingTests
    {
        private const string SlbName = "SLB Savings & Retirement Plan_01-01-2026_11-06-2026.csv";

        private const string Slb = """
            "Plan","Category","Asset Class","Fund Name","Current Mix","Future Investments","Date Range","Opening Balance","Gains/Losses","Other Activity","Closing Balance","Units","Fund Price","Fund Return"
            "SLB Savings & Retirement Plan","Schlumberger Plan Total","Target Date","2030 Target Date Fund","29.0%","0.0%","January 1, 2026 - November 6, 2026","$132,287.00","$7,087.67","$4,928.23","$144,302.90","6216.145573","$23.214208","5.38%"
            "SLB Savings & Retirement Plan","Schlumberger Plan Total","Equity","U.S. Equity Fund","30.0%","0.0%","January 1, 2026 - November 6, 2026","$135,443.82","$12,756.28","$3,387.23","$151,587.33","179.249516","$845.677803","9.39%"
            "SLB Savings & Retirement Plan","Schlumberger Plan Total","Equity","Global Equity Fund","38.0%","0.0%","January 1, 2026 - November 6, 2026","$169,760.07","$15,514.26","$4,958.05","$190,232.38","362.198328","$525.216071","9.17%"
            "SLB Savings & Retirement Plan","Schlumberger Plan Total","Bond","Intermediate Fixed Income Fund","3.0%","0.0%","January 1, 2026 - November 6, 2026","$16,776.47","$5.73","$838.78","$17,620.98","104.324666","$168.905177","0.06%"
            """;

        [Test]
        public void SlbPositions_UseTheEndOfTheDateRangeAndTheFundPrice()
        {
            var sheet = FidelityInvestmentFile.ParseSheet(Slb);
            var map = InvestmentPositionColumnMapping.Suggest(Guid.Empty, sheet.Headers);

            Assert.That(map.SymbolColumn, Is.EqualTo("Fund Name"));
            Assert.That(map.QuantityColumn, Is.EqualTo("Units"));
            Assert.That(map.PriceColumn, Is.EqualTo("Fund Price"));
            Assert.That(map.ValueColumn, Is.EqualTo("Closing Balance"));
            Assert.That(map.AsOfColumn, Is.EqualTo("Date Range"));
            Assert.That(map.AccountNumberColumn, Is.Empty);

            var file = InvestmentPositionMapper.Parse(sheet, map, SlbName).Single();
            Assert.That(file.FileDate, Is.EqualTo(new DateTime(2026, 11, 6)));
            Assert.That(file.AccountNumber, Is.Empty);
            Assert.That(file.Holdings.Select(row => row.Symbol), Is.EqualTo(new[]
            {
                "2030 Target Date Fund",
                "U.S. Equity Fund",
                "Global Equity Fund",
                "Intermediate Fixed Income Fund"
            }));
            var target = file.Holdings[0];
            Assert.That(target.Quantity, Is.EqualTo(6216.145573m));
            Assert.That(target.LastPrice, Is.EqualTo(23.214208m));
            Assert.That(target.CurrentValue, Is.EqualTo(144302.90m));
            Assert.That(target.Description, Is.EqualTo("2030 Target Date Fund"));
        }

        [Test]
        public void SlbPositions_ImportIntoTheChosenPortfolio()
        {
            var accounts = new InMemoryAccountDataStore();
            var plan = new InvestmentAccount { Name = "SLB 401(k)", AccountNumber = "", Type = AccountType.Investment };
            accounts.UpsertAccount(plan);
            var books = new InMemoryInvestmentBookStore();
            var orchestrator = new InvestmentImportOrchestrator(
                accounts,
                books,
                new InMemoryTransactionDataStore(),
                new InMemoryTransactionDataStore());
            var directory = Path.Combine(Path.GetTempPath(), "thms-slb-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, SlbName);
                File.WriteAllText(path, Slb);
                var sheet = FidelityInvestmentFile.ReadSheet(path);
                var map = InvestmentPositionColumnMapping.Suggest(plan.Id, sheet.Headers);
                var preview = orchestrator.PreviewPositions(path, map, plan.Id);
                Assert.That(preview.AccountNames.Keys, Is.EqualTo(new[] { plan.Id }));
                orchestrator.CommitPositions();
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }

            var state = new InvestmentBookState { AccountId = plan.Id, OpenedOn = new DateTime(2026, 11, 6) };
            state.Actions.AddRange(books.GetActions(plan.Id));
            state.MarketQuotes.AddRange(books.GetMarketQuotes());
            state.Securities.AddRange(books.GetSecurities());
            var snapshot = InvestmentBookkeeper.Replay(state);
            Assert.That(snapshot.PricesAsOf, Is.EqualTo(new DateTime(2026, 11, 6)));
            Assert.That(snapshot.PricesIncomplete, Is.False);
            var target = snapshot.Positions.Single(row => row.Symbol == "2030 Target Date Fund");
            Assert.That(target.Shares, Is.EqualTo(6216.145573m));
            Assert.That(target.Price, Is.EqualTo(23.214208m));
            Assert.That(target.MarketValue, Is.EqualTo(6216.145573m * 23.214208m));
            Assert.That(snapshot.Positions, Has.Count.EqualTo(4));
            var saved = accounts.GetInvestmentPositionColumnMap(plan.Id);
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.SymbolColumn, Is.EqualTo("Fund Name"));
            Assert.That(saved.QuantityColumn, Is.EqualTo("Units"));
        }

        [Test]
        public void FidelityPositions_MappedFileMatchesTheBuiltInParser()
        {
            const string csv = """
                Account number,Account name,Symbol,Description,Quantity,Current value,Last price,Cost basis total,Average cost basis,Account type,Amount per share,Est. annual income,Dist. rate, Distribution rate as of,Ex-date,Pay date
                257460702,HSA,FDRXX**,HELD IN MONEY MARKET,,$100.00,,,,Cash,,--,--,--,,,
                257460702,HSA,AGNC,AGNC INVT CORP COM,10,$86.50,$8.65,$98.30,$9.83,Cash,$0.12,$14.40,16.31%,Oct-30-2026,Oct-30-2026,Nov-10-2026,
                257460702,HSA,Pending activity,,,$5.00,,,,,,,,,,,

                "The data and information in this spreadsheet is provided to you solely for your use."
                """;
            const string name = "Portfolio_Positions_Oct-31-2026.csv";
            var expected = FidelityInvestmentFile.ParsePositions(csv, name);
            var sheet = FidelityInvestmentFile.ParseSheet(csv);
            var map = InvestmentPositionColumnMapping.Suggest(Guid.Empty, sheet.Headers);
            var actual = InvestmentPositionMapper.Parse(sheet, map, name).Single();

            Assert.That(actual.FileDate, Is.EqualTo(expected.FileDate));
            Assert.That(actual.AccountNumber, Is.EqualTo(expected.AccountNumber));
            Assert.That(actual.Cash, Is.EqualTo(expected.Cash));
            Assert.That(actual.CashSymbol, Is.EqualTo(expected.CashSymbol));
            var holding = actual.Holdings.Single();
            var expectedHolding = expected.Holdings.Single();
            Assert.That(holding.Symbol, Is.EqualTo(expectedHolding.Symbol));
            Assert.That(holding.Quantity, Is.EqualTo(expectedHolding.Quantity));
            Assert.That(holding.LastPrice, Is.EqualTo(expectedHolding.LastPrice));
            Assert.That(holding.CurrentValue, Is.EqualTo(expectedHolding.CurrentValue));
            Assert.That(holding.CostBasisTotal, Is.EqualTo(expectedHolding.CostBasisTotal));
            Assert.That(holding.AmountPerShare, Is.EqualTo(expectedHolding.AmountPerShare));
            Assert.That(holding.EstimatedAnnualIncome, Is.EqualTo(expectedHolding.EstimatedAnnualIncome));
            Assert.That(holding.DistributionRate, Is.EqualTo(expectedHolding.DistributionRate));
            Assert.That(holding.ExDate, Is.EqualTo(expectedHolding.ExDate));
            Assert.That(holding.PayDate, Is.EqualTo(expectedHolding.PayDate));
        }

        [Test]
        public void FidelityHistory_MappedFileMatchesTheBuiltInParser()
        {
            const string csv = """
                Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
                11/16/2026,MYSTERY FEE,,No Description,Cash,"",0,"","","",-1.00,120.00,""
                11/14/2026,STOCK SPLIT AGNC 2 FOR 1 (AGNC) (Cash),AGNC,AGNC 2 FOR 1,Cash,"","","","","",0,86.40,""
                11/02/2026,DIVIDEND RECEIVED AGNC (AGNC) (Cash),AGNC,AGNC,Cash,"",0,"","","",1.20,Processing,""
                """;
            var expected = FidelityInvestmentFile.ParseHistory(csv);
            var sheet = FidelityInvestmentFile.ParseSheet(csv);
            var map = InvestmentHistoryColumnMapping.Suggest(Guid.Empty, sheet.Headers);
            var actual = InvestmentHistoryMapper.Parse(sheet, map);

            Assert.That(actual.Rows.Select(row => row.RunDate), Is.EqualTo(expected.Rows.Select(row => row.RunDate)));
            Assert.That(actual.Rows.Select(row => row.Action), Is.EqualTo(expected.Rows.Select(row => row.Action)));
            Assert.That(actual.Rows.Select(row => row.Symbol), Is.EqualTo(expected.Rows.Select(row => row.Symbol)));
            Assert.That(actual.Rows.Select(row => row.Amount), Is.EqualTo(expected.Rows.Select(row => row.Amount)));
            Assert.That(actual.Rows.Select(row => row.CashBalanceText), Is.EqualTo(expected.Rows.Select(row => row.CashBalanceText)));
            Assert.That(actual.Rows.Select(row => row.CashBalance), Is.EqualTo(expected.Rows.Select(row => row.CashBalance)));
        }

        [Test]
        public void Sqlite_RemembersThePositionMapUntilTheAccountIsDeleted()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-position-map-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteAccountDataStore(path);
                var account = new InvestmentAccount
                {
                    Name = "SLB 401(k)",
                    Institution = "SLB",
                    AccountNumber = "401k",
                    Type = AccountType.Investment
                };
                store.UpsertAccount(account);
                store.UpsertInvestmentPositionColumnMap(new InvestmentPositionColumnMap
                {
                    AccountId = account.Id,
                    SymbolColumn = "Fund Name",
                    QuantityColumn = "Units",
                    PriceColumn = "Fund Price",
                    ValueColumn = "Closing Balance",
                    AsOfColumn = "Date Range"
                });

                var loaded = new SQLiteAccountDataStore(path).GetInvestmentPositionColumnMap(account.Id);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.SymbolColumn, Is.EqualTo("Fund Name"));
                Assert.That(loaded.QuantityColumn, Is.EqualTo("Units"));
                Assert.That(loaded.AsOfColumn, Is.EqualTo("Date Range"));

                store.DeleteAccount(account.Id);
                Assert.That(store.GetInvestmentPositionColumnMap(account.Id), Is.Null);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
