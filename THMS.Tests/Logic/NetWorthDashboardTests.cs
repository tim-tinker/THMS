using THMS.Data.Stores;
using THMS.Domain.Assets;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.NetWorth;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class NetWorthDashboardTests
    {
        [Test]
        public void Compose_SumsTodayAcrossCashInvestmentsAssetsAndDebt()
        {
            var accounts = new InMemoryAccountDataStore();
            var house = new PropertyAsset { Name = "House" };
            var assets = new InMemoryAssetDataStore();
            assets.Save(house);
            var checking = new BankAccount { Name = "Checking", PostedBalance = 1200 };
            var savings = new BankAccount { Name = "Savings", PostedBalance = 300 };
            var visa = new CreditAccount { Name = "Visa", PostedBalance = -400 };
            var mortgage = new MortgageAccount { Name = "Mortgage", Principal = -200000, SecuredAssetId = house.Id };
            var hsa = new InvestmentAccount { Name = "HSA", CashBalance = 20, PositionValue = 80 };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(savings);
            accounts.UpsertAccount(visa);
            accounts.UpsertAccount(mortgage);
            accounts.UpsertAccount(hsa);
            accounts.UpsertAccount(new InternalAccount { Name = "Offset", Purpose = "" });
            SaveValue(assets, house.Id, ValuationTypes.LandMarket, 100000, new DateTime(2026, 11, 1));
            SaveValue(assets, house.Id, ValuationTypes.ImprovementMarket, 300000, new DateTime(2026, 11, 1));
            SaveValue(assets, house.Id, ValuationTypes.TotalMarket, 400000, new DateTime(2026, 11, 1));
            SaveValue(assets, house.Id, ValuationTypes.Assessed, 350000, new DateTime(2026, 11, 1));
            var car = new VehicleAsset { Name = "Car" };
            assets.Save(car);
            SaveValue(assets, car.Id, ValuationTypes.Market, 15000, new DateTime(2026, 11, 1));
            accounts.UpsertAccount(new LeaseAccount
            {
                Name = "Car lease",
                Type = AccountType.Lease,
                Principal = -9000,
                LeaseAmount = 15000,
                Residual = 4000,
                SecuredAssetId = car.Id
            });
            assets.Save(new PersonalAsset { Name = "Piano" });

            var snapshot = Compose(accounts, assets, new DateTime(2026, 11, 15));

            Assert.That(snapshot.Cash, Is.EqualTo(1500m));
            Assert.That(snapshot.Investments, Is.EqualTo(100m));
            Assert.That(snapshot.OtherAssets, Is.EqualTo(402000m));
            Assert.That(snapshot.Debt, Is.EqualTo(-200400m));
            Assert.That(snapshot.NetWorth, Is.EqualTo(203200m));
            AssertAddsUp(snapshot);
            Assert.That(snapshot.Months, Has.Count.EqualTo(1));
            Assert.That(snapshot.Months[0].Change, Is.Null);
            Assert.That(snapshot.Lines.Select(line => line.Name), Is.EqualTo(new[]
            {
                "Checking", "Savings", "HSA", "House", "Car", "Piano", "Mortgage", "Visa"
            }));
            Assert.That(snapshot.Lines.Single(line => line.Name == "House").Amount, Is.EqualTo(400000m));
            Assert.That(snapshot.Lines.Single(line => line.Name == "House").Source, Does.StartWith("Total market"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "HSA").Source, Is.EqualTo("No position history"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Piano").Amount, Is.Null);
            Assert.That(snapshot.Lines.Single(line => line.Name == "Mortgage").Source, Is.EqualTo("Current balance, secured by House"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Car").Group, Is.EqualTo("Vehicles"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Car").Amount, Is.EqualTo(2000m));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Car").Source, Does.Contain("Keeping it"));
            Assert.That(snapshot.Lines.Any(line => line.Name == "Car lease"), Is.False);
            Assert.That(snapshot.Lines.Any(line => line.Name == "Offset"), Is.False);
            Assert.That(snapshot.Note, Does.Contain("1 asset has no valuation"));
            Assert.That(snapshot.Note, Does.Contain("no position history"));
        }

        [Test]
        public void Compose_ValuesAKeptLeaseAndShowsANegativeOneAsDebt()
        {
            var accounts = new InMemoryAccountDataStore();
            var assets = new InMemoryAssetDataStore();
            var kept = new VehicleAsset { Name = "Kept" };
            var underwater = new VehicleAsset { Name = "Underwater" };
            assets.Save(kept);
            assets.Save(underwater);
            SaveValue(assets, kept.Id, ValuationTypes.Market, 30000, new DateTime(2026, 11, 1));
            SaveValue(assets, underwater.Id, ValuationTypes.Market, 10000, new DateTime(2026, 11, 1));
            accounts.UpsertAccount(new LeaseAccount
            {
                Name = "Kept lease",
                Type = AccountType.Lease,
                Principal = -5000,
                Residual = 20000,
                SecuredAssetId = kept.Id
            });
            accounts.UpsertAccount(new LeaseAccount
            {
                Name = "Underwater lease",
                Type = AccountType.Lease,
                Principal = -5000,
                Residual = 20000,
                SecuredAssetId = underwater.Id
            });

            var snapshot = Compose(accounts, assets, new DateTime(2026, 11, 15));

            Assert.That(snapshot.Lines.Single(line => line.Name == "Kept").Group, Is.EqualTo("Vehicles"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Kept").Amount, Is.EqualTo(5000m));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Underwater").Group, Is.EqualTo("Debt"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Underwater").Amount, Is.EqualTo(-15000m));
            Assert.That(snapshot.Lines.Any(line => line.Name.EndsWith("lease", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(snapshot.OtherAssets, Is.EqualTo(5000m));
            Assert.That(snapshot.Debt, Is.EqualTo(-15000m));
            Assert.That(snapshot.NetWorth, Is.EqualTo(-10000m));
            AssertAddsUp(snapshot);
        }

        [Test]
        public void Compose_CarriesValuationsAndPrefersTotalMarket()
        {
            var assets = new InMemoryAssetDataStore();
            var house = new PropertyAsset { Name = "House" };
            var lot = new PropertyAsset { Name = "Lot" };
            var car = new VehicleAsset { Name = "Car" };
            assets.Save(house);
            assets.Save(lot);
            assets.Save(car);
            assets.Save(new PersonalAsset { Name = "Piano" });
            SaveValue(assets, house.Id, ValuationTypes.LandMarket, 100000, new DateTime(2026, 8, 1));
            SaveValue(assets, house.Id, ValuationTypes.ImprovementMarket, 300000, new DateTime(2026, 8, 1));
            SaveValue(assets, house.Id, ValuationTypes.TotalMarket, 400000, new DateTime(2026, 8, 1));
            SaveValue(assets, house.Id, ValuationTypes.Assessed, 350000, new DateTime(2026, 10, 1));
            SaveValue(assets, lot.Id, ValuationTypes.Assessed, 10000, new DateTime(2026, 9, 15));
            SaveValue(assets, lot.Id, ValuationTypes.TotalMarket, 99999, new DateTime(2026, 12, 1));
            SaveValue(assets, car.Id, ValuationTypes.Market, 20000, new DateTime(2026, 11, 2));

            var snapshot = Compose(new InMemoryAccountDataStore(), assets, new DateTime(2026, 11, 15));

            Assert.That(snapshot.Months.Select(month => month.Label), Is.EqualTo(new[]
            {
                "Aug 2026", "Sep 2026", "Oct 2026", "Nov 2026"
            }));
            Assert.That(snapshot.Months.Select(month => month.Property), Is.EqualTo(new[]
            {
                400000m, 410000m, 410000m, 410000m
            }));
            Assert.That(snapshot.Months.Select(month => month.Vehicles), Is.EqualTo(new[] { 0m, 0m, 0m, 20000m }));
            Assert.That(snapshot.OtherAssets, Is.EqualTo(430000m));
            Assert.That(snapshot.Lines.Single(line => line.Name == "House").Source, Does.Contain("carried forward"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Lot").Source, Does.StartWith("Assessed"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Car").Source, Does.Not.Contain("carried forward"));
            AssertAddsUp(snapshot);
        }

        [Test]
        public void Compose_RebuildsEarlierMonthsFromStatementsAndUsesStoredBalanceNow()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var assets = new InMemoryAssetDataStore();
            var house = new PropertyAsset { Name = "House" };
            assets.Save(house);
            SaveValue(assets, house.Id, ValuationTypes.TotalMarket, 400000, new DateTime(2026, 8, 1));

            var checking = new BankAccount { Name = "Checking", PostedBalance = 1600 };
            var visa = new CreditAccount { Name = "Visa", PostedBalance = -250 };
            var mortgage = new MortgageAccount { Name = "Mortgage", Principal = -200000, SecuredAssetId = house.Id };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(visa);
            accounts.UpsertAccount(mortgage);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = new DateTime(2026, 9, 30),
                StatementBalance = 1000
            });
            statements.Save(new CreditCardStatement
            {
                AccountId = visa.Id,
                StatementDate = new DateTime(2026, 8, 31),
                StatementBalance = -400
            });
            transactions.AddPostedTransaction(Posted(checking.Id, new DateTime(2026, 9, 30), 50));
            transactions.AddPostedTransaction(Posted(checking.Id, new DateTime(2026, 10, 15), 200));
            transactions.AddPostedTransaction(Posted(checking.Id, new DateTime(2026, 11, 3), 100));
            transactions.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                AccountId = checking.Id,
                Date = new DateTime(2026, 10, 20),
                Amount = 25
            });
            transactions.AddPostedTransaction(Posted(visa.Id, new DateTime(2026, 9, 2), 100));

            var snapshot = new NetWorthComposer(
                accounts,
                transactions,
                statements,
                new InMemoryInvestmentBookStore(),
                assets).Compose(new DateTime(2026, 11, 15));

            var august = Month(snapshot, new DateTime(2026, 8, 1));
            var september = Month(snapshot, new DateTime(2026, 9, 1));
            var october = Month(snapshot, new DateTime(2026, 10, 1));
            var november = Month(snapshot, new DateTime(2026, 11, 1));

            Assert.That(august.Cash, Is.EqualTo(0m));
            Assert.That(august.Debt, Is.EqualTo(-400m));
            Assert.That(august.Change, Is.Null);
            Assert.That(september.Cash, Is.EqualTo(1000m));
            Assert.That(september.Debt, Is.EqualTo(-300m));
            Assert.That(october.Cash, Is.EqualTo(1225m));
            Assert.That(october.Debt, Is.EqualTo(-300m));
            Assert.That(november.Cash, Is.EqualTo(1600m));
            Assert.That(november.Debt, Is.EqualTo(-200250m));
            Assert.That(november.Property, Is.EqualTo(400000m));
            Assert.That(november.NetWorth, Is.EqualTo(201350m));
            Assert.That(september.Lines.Single(line => line.Name == "Checking").Amount, Is.EqualTo(1000m));
            Assert.That(september.Lines.Single(line => line.Name == "Checking").Source, Does.StartWith("Balance as of"));
            Assert.That(september.Lines.Single(line => line.Name == "Visa").Amount, Is.EqualTo(-300m));
            Assert.That(september.Lines.Any(line => line.Name == "Mortgage"), Is.False);
            Assert.That(november.Lines.Single(line => line.Name == "Checking").Source, Is.EqualTo("Current balance"));
            Assert.That(november.Lines.Single(line => line.Name == "Mortgage").Source, Is.EqualTo("Current balance, secured by House"));
            Assert.That(snapshot.NetWorth, Is.EqualTo(201350m));
            Assert.That(snapshot.Note, Is.EqualTo(NetWorthSnapshot.MethodNote));
            AssertAddsUp(snapshot);
        }

        [Test]
        public void Compose_UsesInvestmentHistoryAndAddsUntrackedOnlyToTheCurrentMonth()
        {
            var accounts = new InMemoryAccountDataStore();
            var books = new InMemoryInvestmentBookStore();
            var tracked = new InvestmentAccount
            {
                Name = "Brokerage",
                TracksPositions = true,
                BookOpenedOn = new DateTime(2026, 10, 31)
            };
            var stored = new InvestmentAccount { Name = "Old IRA", CashBalance = 20, PositionValue = 80 };
            var checking = new BankAccount { Name = "Checking", PostedBalance = 500 };
            accounts.UpsertAccount(tracked);
            accounts.UpsertAccount(stored);
            accounts.UpsertAccount(checking);
            books.UpsertAction(Applied(tracked.Id, 1, new DateTime(2026, 10, 31), "AGNC", 10));
            books.UpsertMarketQuote(Quote("AGNC", 8m, new DateTime(2026, 10, 31)));
            books.UpsertMarketQuote(Quote("AGNC", 10m, new DateTime(2026, 11, 14)));

            var snapshot = new NetWorthComposer(
                accounts,
                new InMemoryTransactionDataStore(),
                new InMemoryAccountStatementDataStore(),
                books,
                new InMemoryAssetDataStore()).Compose(new DateTime(2026, 11, 15));

            Assert.That(Month(snapshot, new DateTime(2026, 10, 1)).Investments, Is.EqualTo(80m));
            Assert.That(Month(snapshot, new DateTime(2026, 10, 1)).Cash, Is.EqualTo(0m));
            Assert.That(Month(snapshot, new DateTime(2026, 10, 1)).Lines.Select(line => line.Name), Is.EqualTo(new[] { "Brokerage" }));
            Assert.That(Month(snapshot, new DateTime(2026, 10, 1)).Lines.Single().Amount, Is.EqualTo(80m));
            Assert.That(Month(snapshot, new DateTime(2026, 11, 1)).Investments, Is.EqualTo(200m));
            Assert.That(Month(snapshot, new DateTime(2026, 11, 1)).Cash, Is.EqualTo(500m));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Brokerage").Amount, Is.EqualTo(100m));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Brokerage").Source, Is.EqualTo("Market value"));
            Assert.That(snapshot.Lines.Single(line => line.Name == "Old IRA").Amount, Is.EqualTo(100m));
            Assert.That(snapshot.Note, Does.Contain("no position history"));
            AssertAddsUp(snapshot);
        }

        [Test]
        public void Compose_UsesThePrivatePartyMedianAndKeepsTheLease()
        {
            var accounts = new InMemoryAccountDataStore();
            var assets = new InMemoryAssetDataStore();
            var bluey = new VehicleAsset { Name = "Bluey" };
            assets.Save(bluey);
            SaveValue(assets, bluey.Id, ValuationTypes.PrivateParty, 10000, new DateTime(2026, 10, 1), "Kelley Blue Book");
            SaveValue(assets, bluey.Id, ValuationTypes.PrivateParty, 28000, new DateTime(2026, 11, 20), "Kelley Blue Book");
            SaveValue(assets, bluey.Id, ValuationTypes.PrivateParty, 30000, new DateTime(2026, 11, 20), "Edmunds");
            SaveValue(assets, bluey.Id, ValuationTypes.PrivateParty, 32000, new DateTime(2026, 11, 20), "NADA");
            SaveValue(assets, bluey.Id, ValuationTypes.PrivateParty, 34000, new DateTime(2026, 11, 18), "CarGurus");
            SaveValue(assets, bluey.Id, ValuationTypes.TradeIn, 20000, new DateTime(2026, 11, 24), "Kelley Blue Book");
            SaveValue(assets, bluey.Id, ValuationTypes.TradeIn, 22000, new DateTime(2026, 11, 24), "Edmunds");
            SaveValue(assets, bluey.Id, ValuationTypes.TradeIn, 24000, new DateTime(2026, 11, 24), "NADA");
            SaveValue(assets, bluey.Id, ValuationTypes.TradeIn, 26000, new DateTime(2026, 11, 24), "CarGurus");
            accounts.UpsertAccount(new LeaseAccount
            {
                Name = "Bluey lease",
                Type = AccountType.Lease,
                Principal = -8000,
                Residual = 20000,
                SecuredAssetId = bluey.Id
            });

            var snapshot = Compose(accounts, assets, new DateTime(2026, 11, 24));

            var line = snapshot.Lines.Single(item => item.Name == "Bluey");
            Assert.That(line.Group, Is.EqualTo("Vehicles"));
            Assert.That(line.Amount, Is.EqualTo(3000m));
            Assert.That(line.Source, Does.Contain("Private party median of 4"));
            Assert.That(line.Source, Does.Contain("Kelley Blue Book"));
            Assert.That(line.Source, Does.Contain("Edmunds"));
            Assert.That(line.Source, Does.Contain("NADA"));
            Assert.That(line.Source, Does.Contain("CarGurus"));
            Assert.That(line.Source, Does.Contain("Keeping it"));
            Assert.That(line.Source, Does.Not.Contain("Trade-in"));
            Assert.That(line.Source, Does.Not.Contain("No valuation"));
            Assert.That(snapshot.Lines.Any(item => item.Name == "Bluey lease"), Is.False);
            Assert.That(snapshot.OtherAssets, Is.EqualTo(3000m));
            Assert.That(snapshot.Debt, Is.EqualTo(0m));
            Assert.That(snapshot.NetWorth, Is.EqualTo(3000m));
            Assert.That(snapshot.Months.Select(month => month.Vehicles), Is.EqualTo(new[] { 0m, 3000m }));
            AssertAddsUp(snapshot);
        }

        [Test]
        public void Compose_UsesTradeInWhenNoPrivatePartyIsRecorded()
        {
            var assets = new InMemoryAssetDataStore();
            var bluey = new VehicleAsset { Name = "Bluey" };
            assets.Save(bluey);
            SaveValue(assets, bluey.Id, ValuationTypes.TradeIn, 21000, new DateTime(2026, 11, 2), "Kelley Blue Book");
            SaveValue(assets, bluey.Id, ValuationTypes.Retail, 40000, new DateTime(2026, 11, 20), "Edmunds");

            var snapshot = Compose(new InMemoryAccountDataStore(), assets, new DateTime(2026, 11, 24));

            var line = snapshot.Lines.Single();
            Assert.That(line.Group, Is.EqualTo("Vehicles"));
            Assert.That(line.Amount, Is.EqualTo(21000m));
            Assert.That(line.Source, Does.StartWith("Trade-in"));
            AssertAddsUp(snapshot);
        }

        [Test]
        public void Compose_CarriesPrivatePartyAheadOfANewerTradeIn()
        {
            var assets = new InMemoryAssetDataStore();
            var bluey = new VehicleAsset { Name = "Bluey" };
            assets.Save(bluey);
            SaveValue(assets, bluey.Id, ValuationTypes.PrivateParty, 25000, new DateTime(2026, 9, 15), "Kelley Blue Book");
            SaveValue(assets, bluey.Id, ValuationTypes.TradeIn, 40000, new DateTime(2026, 11, 10), "Edmunds");

            var snapshot = Compose(new InMemoryAccountDataStore(), assets, new DateTime(2026, 11, 15));

            Assert.That(snapshot.Lines.Single().Amount, Is.EqualTo(25000m));
            Assert.That(snapshot.Lines.Single().Source, Does.StartWith("Private party"));
            Assert.That(snapshot.Lines.Single().Source, Does.Contain("carried forward"));
            Assert.That(snapshot.Months.Select(month => month.Label), Is.EqualTo(new[]
            {
                "Sep 2026", "Oct 2026", "Nov 2026"
            }));
            Assert.That(snapshot.Months.Select(month => month.Vehicles), Is.EqualTo(new[] { 25000m, 25000m, 25000m }));
            AssertAddsUp(snapshot);
        }

        [Test]
        public void Compose_WithNoData_ReturnsTheCurrentMonthAtZero()
        {
            var snapshot = Compose(new InMemoryAccountDataStore(), new InMemoryAssetDataStore(), new DateTime(2026, 11, 15));

            Assert.That(snapshot.NetWorth, Is.EqualTo(0m));
            Assert.That(snapshot.Months.Single().Label, Is.EqualTo("Nov 2026"));
            Assert.That(snapshot.Months.Single().Change, Is.Null);
            Assert.That(snapshot.Lines, Is.Empty);
            Assert.That(snapshot.Note, Is.EqualTo(NetWorthSnapshot.MethodNote));
        }

        private static NetWorthSnapshot Compose(
            InMemoryAccountDataStore accounts,
            InMemoryAssetDataStore assets,
            DateTime asOf) =>
            new NetWorthComposer(
                accounts,
                new InMemoryTransactionDataStore(),
                new InMemoryAccountStatementDataStore(),
                new InMemoryInvestmentBookStore(),
                assets).Compose(asOf);

        private static NetWorthMonthRow Month(NetWorthSnapshot snapshot, DateTime month) =>
            snapshot.Months.Single(row => row.Month == month);

        private static void AssertAddsUp(NetWorthSnapshot snapshot)
        {
            foreach (var month in snapshot.Months)
            {
                Assert.That(
                    month.Cash + month.Investments + month.Property + month.Vehicles + month.Personal + month.Debt,
                    Is.EqualTo(month.NetWorth));
            }

            var latest = snapshot.Months[^1];
            Assert.That(snapshot.Cash, Is.EqualTo(latest.Cash));
            Assert.That(snapshot.Investments, Is.EqualTo(latest.Investments));
            Assert.That(snapshot.OtherAssets, Is.EqualTo(latest.Property + latest.Vehicles + latest.Personal));
            Assert.That(snapshot.Debt, Is.EqualTo(latest.Debt));
            Assert.That(snapshot.Lines, Is.SameAs(latest.Lines));
            foreach (var month in snapshot.Months)
            {
                Assert.That(Sum(month, "Cash"), Is.EqualTo(month.Cash));
                Assert.That(Sum(month, "Investments"), Is.EqualTo(month.Investments));
                Assert.That(Sum(month, "Property"), Is.EqualTo(month.Property));
                Assert.That(Sum(month, "Vehicles"), Is.EqualTo(month.Vehicles));
                Assert.That(Sum(month, "Personal"), Is.EqualTo(month.Personal));
                Assert.That(Sum(month, "Debt"), Is.EqualTo(month.Debt));
            }
        }

        private static decimal Sum(NetWorthMonthRow month, string group) =>
            month.Lines.Where(line => line.Group == group).Sum(line => line.Amount ?? 0m);

        private static void SaveValue(
            InMemoryAssetDataStore assets,
            Guid assetId,
            string type,
            decimal amount,
            DateTime asOf,
            string source = "") =>
            assets.SaveValuation(new AssetValuation
            {
                AssetId = assetId,
                ValueType = type,
                Amount = amount,
                AsOf = asOf,
                Source = source
            });

        private static PostedTransaction Posted(Guid accountId, DateTime date, decimal amount) =>
            new()
            {
                AccountId = accountId,
                Date = date,
                Amount = amount
            };

        private static InvestmentAction Applied(Guid accountId, int sequence, DateTime date, string symbol, decimal shares) =>
            new()
            {
                AccountId = accountId,
                Sequence = sequence,
                RunDate = date,
                Kind = InvestmentActionKind.Opening,
                Status = InvestmentActionStatus.Applied,
                Symbol = symbol,
                Quantity = shares,
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
