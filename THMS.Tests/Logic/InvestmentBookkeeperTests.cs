using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Domain.Finance.Transactions;
using THMS.External;
using THMS.Logic.Finance.Aggregation;
using THMS.Logic.Finance.Investments;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class InvestmentBookkeeperTests
    {
        private const string PositionsName = "Portfolio_Positions_Oct-31-2026.csv";

        private const string Positions = """
            Account number,Account name,Symbol,Description,Quantity,Current value,Last price,Cost basis total,Average cost basis,Account type,Amount per share,Est. annual income,Dist. rate, Distribution rate as of,Ex-date,Pay date
            257460702,HSA,FDRXX**,HELD IN MONEY MARKET,,$100.00,,,,Cash,,--,--,--,,,
            257460702,HSA,AGNC,AGNC INVT CORP COM,10,$86.50,$8.65,$98.30,$9.83,Cash,$0.12,$14.40,16.31%,Oct-30-2026,Oct-30-2026,Nov-10-2026,
            257460702,HSA,Pending activity,,,$5.00,,,,,,,,,,,

            "The data and information in this spreadsheet is provided to you solely for your use."
            """;

        private const string History = """
            Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
            11/16/2026,MYSTERY FEE,,No Description,Cash,"",0,"","","",-1.00,120.00,""
            11/14/2026,STOCK SPLIT AGNC 2 FOR 1 (AGNC) (Cash),AGNC,AGNC 2 FOR 1,Cash,"","","","","",0,86.40,""
            11/03/2026,PARTIC CONTR CURRENT (Cash),"",No Description,Cash,"",0,"","","",50,100.00,""
            11/02/2026,DIVIDEND RECEIVED AGNC (AGNC) (Cash),AGNC,AGNC,Cash,"",0,"","","",1.20,Processing,""
            11/02/2026,REINVESTMENT FIDELITY GOVERNMENT CASH RESERVES (FDRXX) (Cash),FDRXX,FIDELITY GOVERNMENT CASH RESERVES,Cash,1,1.20,"","","","-1.20",50.00,""
            10/30/2026,DIVIDEND RECEIVED AGNC (AGNC) (Cash),AGNC,AGNC,Cash,"",0,"","","",1.00,100.00,""
            """;

        [Test]
        public void OpeningBook_UsesPositionsAndAppliesOnlyLaterHistory()
        {
            var state = Book();
            var first = InvestmentBookkeeper.Import(
                state,
                FidelityInvestmentFile.ParsePositions(Positions, PositionsName),
                FidelityInvestmentFile.ParseHistory(History));

            Assert.That(state.OpenedOn, Is.EqualTo(new DateTime(2026, 10, 31)));
            Assert.That(first.Snapshot.Positions.Select(row => row.Symbol), Is.EqualTo(new[] { "AGNC" }));
            var agnc = first.Snapshot.Positions.Single();
            Assert.That(agnc.Shares, Is.EqualTo(20));
            Assert.That(agnc.AverageCost, Is.EqualTo(4.915m));
            Assert.That(agnc.MarketValue, Is.EqualTo(86.50m));
            Assert.That(agnc.EstimatedAnnualIncome, Is.EqualTo(14.40m));
            Assert.That(first.Snapshot.Cash, Is.EqualTo(151.20m));
            Assert.That(first.Snapshot.Income.Any(point => point.Date == new DateTime(2026, 10, 31) && point.EstimatedAnnualIncome == 14.40m), Is.True);
            Assert.That(first.Snapshot.Income.Last().EstimatedAnnualIncome, Is.EqualTo(14.40m));
            Assert.That(first.Review, Is.EqualTo(1));
            Assert.That(state.Actions.Any(action => action.RunDate == new DateTime(2026, 10, 30) && action.Status == InvestmentActionStatus.Skipped), Is.True);
            Assert.That(state.Actions.Any(action => action.Kind == InvestmentActionKind.Reinvestment && action.Status == InvestmentActionStatus.Skipped), Is.True);

            var again = InvestmentBookkeeper.Import(
                state,
                FidelityInvestmentFile.ParsePositions(Positions, PositionsName),
                FidelityInvestmentFile.ParseHistory(History));
            Assert.That(again.Applied, Is.EqualTo(0));
            Assert.That(again.Snapshot.Positions.Single().Shares, Is.EqualTo(20));
            Assert.That(again.Snapshot.Cash, Is.EqualTo(151.20m));
        }

        [Test]
        public void Sale_UsesOldestLotsAndStopsWhenSharesAreShort()
        {
            var state = Book();
            InvestmentBookkeeper.Import(
                state,
                FidelityInvestmentFile.ParsePositions(Positions, PositionsName),
                FidelityInvestmentFile.ParseHistory("""
                    Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
                    11/05/2026,YOU BOUGHT OTHER (OTHER) (Cash),OTHER,OTHER,Cash,2,5,"","","",-10,90,""
                    11/04/2026,YOU SOLD AGNC (AGNC) (Cash),AGNC,AGNC,Cash,8,100,"","","",800,100,""
                    11/03/2026,YOU BOUGHT AGNC (AGNC) (Cash),AGNC,AGNC,Cash,10,4,"","","",-40,100,""
                    """));

            Assert.That(state.Actions.Single(action => action.Kind == InvestmentActionKind.Sell).Status, Is.EqualTo(InvestmentActionStatus.Review));
            var agnc = state.Actions.Where(action => action.Symbol == "AGNC" && action.Status == InvestmentActionStatus.Applied).ToList();
            Assert.That(agnc.Any(action => action.Kind == InvestmentActionKind.Buy), Is.True);

            var sold = Book();
            var preview = InvestmentBookkeeper.Import(
                sold,
                FidelityInvestmentFile.ParsePositions(Positions, PositionsName),
                FidelityInvestmentFile.ParseHistory("""
                    Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
                    11/04/2026,YOU SOLD AGNC (AGNC) (Cash),AGNC,AGNC,Cash,8,-4,"","","",32,132,""
                    11/03/2026,YOU BOUGHT AGNC (AGNC) (Cash),AGNC,AGNC,Cash,10,4,"","","",-40,100,""
                    """));
            var row = preview.Snapshot.Positions.Single(position => position.Symbol == "AGNC");
            Assert.That(row.Shares, Is.EqualTo(10));
            Assert.That(row.TotalBasis, Is.EqualTo(98.98m));
            Assert.That(preview.Snapshot.Cash, Is.EqualTo(92m));
        }

        [Test]
        public void LaterPositionsFile_ReplacesQuotesAndDoesNotAddShares()
        {
            var state = Book();
            InvestmentBookkeeper.Import(state, FidelityInvestmentFile.ParsePositions(Positions, PositionsName), history: null);
            var second = """
                Account number,Account name,Symbol,Description,Quantity,Current value,Last price,Cost basis total,Average cost basis,Account type,Amount per share,Est. annual income,Dist. rate, Distribution rate as of,Ex-date,Pay date
                257460702,HSA,FDRXX**,HELD IN MONEY MARKET,,$100.00,,,,Cash,,--,--,--,,,
                257460702,HSA,AGNC,AGNC INVT CORP COM,12,$120.00,$10.00,$98.30,$9.83,Cash,$0.12,$17.28,16.31%,Nov-13-2026,Nov-13-2026,Dec-10-2026,
                """;
            var preview = InvestmentBookkeeper.Import(
                state,
                FidelityInvestmentFile.ParsePositions(second, "Portfolio_Positions_Nov-14-2026.csv"),
                history: null);

            Assert.That(preview.Snapshot.Positions.Single().Shares, Is.EqualTo(10));
            Assert.That(preview.Snapshot.Positions.Single().Price, Is.EqualTo(10m));
            Assert.That(preview.Snapshot.Positions.Single().EstimatedAnnualIncome, Is.EqualTo(14.40m));
            Assert.That(preview.Messages, Has.Some.Contains("12 shares in the positions file and 10"));
        }

        [Test]
        public void PendingCashBalance_UpdatesWithoutPostingTwice()
        {
            var state = Book();
            InvestmentBookkeeper.Import(
                state,
                FidelityInvestmentFile.ParsePositions(Positions, PositionsName),
                FidelityInvestmentFile.ParseHistory("""
                    Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
                    11/02/2026,DIVIDEND RECEIVED AGNC (AGNC) (Cash),AGNC,AGNC,Cash,"",0,"","","",1.20,Processing,""
                    """));
            var again = InvestmentBookkeeper.Import(
                state,
                positions: null,
                FidelityInvestmentFile.ParseHistory("""
                    Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
                    11/02/2026,DIVIDEND RECEIVED AGNC (AGNC) (Cash),AGNC,AGNC,Cash,"",0,"","","",1.20,101.20,""
                    """));

            Assert.That(again.Updated, Is.EqualTo(1));
            Assert.That(again.Applied, Is.EqualTo(0));
            Assert.That(again.Snapshot.Cash, Is.EqualTo(101.20m));
        }

        [Test]
        public void ContributionAndDistribution_PostToTheLedgerWithoutABankLeg()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var account = new InvestmentAccount
            {
                Name = "HSA",
                AccountNumber = "257460702",
                Type = AccountType.Investment
            };
            accounts.UpsertAccount(account);
            var orchestrator = new InvestmentImportOrchestrator(accounts, new InMemoryInvestmentBookStore(), transactions, transactions);
            var directory = Path.Combine(Path.GetTempPath(), "thms-invest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var positions = Path.Combine(directory, PositionsName);
                var history = Path.Combine(directory, "history.csv");
                File.WriteAllText(positions, Positions);
                File.WriteAllText(history, """
                    Run Date,Action,Symbol,Description,Type,Price ($),Quantity,Commission ($),Fees ($),Accrued Interest ($),Amount ($),Cash Balance ($),Settlement Date
                    11/04/2026,NORMAL DISTR PARTIAL (Cash),"",No Description,Cash,"",0,"","","","-20",130,""
                    11/03/2026,PARTIC CONTR CURRENT (Cash),"",No Description,Cash,"",0,"","","",50,150,""
                    """);
                orchestrator.Preview(account.Id, positions, history);
                orchestrator.Commit(account.Id);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }

            var saved = (InvestmentAccount)accounts.GetAllAccounts().Single();
            Assert.That(saved.TracksPositions, Is.True);
            Assert.That(saved.CashBalance, Is.EqualTo(130m));
            Assert.That(saved.PositionValue, Is.EqualTo(86.50m));
            var posted = transactions.GetPostedTransactions(account.Id).OrderBy(tx => tx.Date).ToList();
            Assert.That(posted.Select(tx => tx.Amount), Is.EqualTo(new[] { 50m, -20m }));
            Assert.That(posted.Select(tx => tx.Category), Is.EqualTo(new[]
            {
                InvestmentImportOrchestrator.ContributionCategoryName,
                InvestmentImportOrchestrator.DistributionCategoryName
            }));
            Assert.That(posted.All(tx => transactions.GetCategory(tx.CategoryId!.Value)!.Role == CategoryRole.Investment), Is.True);
            Assert.That(transactions.GetPostedTransferTransactions(account.Id), Is.Empty);
        }

        [Test]
        public void PlaidAndPostedBalance_LeaveCashAloneAfterTheBookOpens()
        {
            var store = new InMemoryAccountDataStore();
            var account = new InvestmentAccount
            {
                Name = "HSA",
                CashBalance = 40,
                PositionValue = 60,
                TracksPositions = true,
                ExternalLink = new ExternalAccountLink { AccessToken = "tok", PlaidAccountId = "p1" }
            };
            store.UpsertAccount(account);
            var fetcher = new FakeAccountFetcher
            {
                Accounts = [new AccountDto { PlaidAccountId = "p1", Current = 999, Name = "Plaid" }]
            };
            var result = new AccountSyncOrchestrator(fetcher, store).SyncAsync(account).GetAwaiter().GetResult();
            Assert.That(result.CashBalance, Is.EqualTo(40));
            Assert.That(((InvestmentAccount)store.GetAllAccounts().Single()).CashBalance, Is.EqualTo(40));

            new AccountOrchestrator(store).UpdatePostedBalance("HSA", 70, new DateTime(2026, 11, 1));
            Assert.That(((InvestmentAccount)store.GetAccount("HSA")!).CashBalance, Is.EqualTo(40));
        }

        [Test]
        public void AccountValue_IsCashPlusPositions()
        {
            var views = UnifiedAccountViewBuilder.Build(
            [
                new InvestmentAccount { Name = "Inv", CashBalance = 5 },
                new InvestmentAccount { Name = "HSA", CashBalance = 100, PositionValue = 50, TracksPositions = true }
            ]);
            Assert.That(views[0].Balance, Is.EqualTo(5));
            Assert.That(views[1].Balance, Is.EqualTo(150));

            var snapshot = new FinanceDashboardComposer().Compose(
                DateTime.Today,
                [new InvestmentAccount { Name = "HSA", CashBalance = 100, PositionValue = 50 }],
                [],
                [],
                [],
                new Dictionary<Guid, ExpenseBudgetHistory?>(),
                [],
                [],
                []);
            Assert.That(snapshot.InvestmentCash, Is.EqualTo(150));
            Assert.That(snapshot.NetLiquid, Is.EqualTo(150));
        }

        [Test]
        public void SqliteBook_RoundTripsActionsAndQuotes()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-invest-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteInvestmentBookStore(path);
                var accountId = Guid.NewGuid();
                var action = new InvestmentAction
                {
                    AccountId = accountId,
                    ImportKey = "k1",
                    Sequence = 1,
                    RunDate = new DateTime(2026, 11, 2),
                    ActionText = "DIVIDEND RECEIVED",
                    Symbol = "AGNC",
                    Description = "AGNC",
                    Amount = 1.20m,
                    Quantity = 0,
                    Kind = InvestmentActionKind.Dividend,
                    Status = InvestmentActionStatus.Applied,
                    CashChange = 1.20m
                };
                store.UpsertAction(action);
                store.UpsertQuote(new SecurityQuote
                {
                    AccountId = accountId,
                    Symbol = "AGNC",
                    AsOf = new DateTime(2026, 10, 30),
                    Price = 8.65m,
                    AnnualIncomePerShare = 1.44m
                });
                store.UpsertSecurity(new InvestmentSecurity { Symbol = "FDRXX", Name = "Cash", IsSweep = true });

                var again = new SQLiteInvestmentBookStore(path);
                var stored = again.GetActions(accountId).Single();
                Assert.That(stored.Amount, Is.EqualTo(1.20m));
                Assert.That(stored.Kind, Is.EqualTo(InvestmentActionKind.Dividend));
                Assert.That(again.GetQuotes(accountId).Single().AnnualIncomePerShare, Is.EqualTo(1.44m));
                Assert.That(again.GetSecurities().Single().IsSweep, Is.True);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Test]
        public void FidelityDownloads_OpenTheHsaAndApplyNovemberDividendsOnce()
        {
            var positionsPath = @"C:\Users\timti\Downloads\Portfolio_Positions_Oct-31-2026 (3).csv";
            var historyPath = @"C:\Users\timti\Downloads\History_for_Account_257460702 (1).csv";
            if (!File.Exists(positionsPath) || !File.Exists(historyPath))
                Assert.Ignore("The Fidelity downloads are not on this machine.");

            var positions = FidelityInvestmentFile.ReadPositions(positionsPath);
            var history = FidelityInvestmentFile.ReadHistory(historyPath);
            var state = Book();
            var preview = InvestmentBookkeeper.Import(state, positions, history);

            Assert.That(positions.Cash, Is.EqualTo(3676.31m));
            Assert.That(positions.FileDate, Is.EqualTo(new DateTime(2026, 10, 31)));
            Assert.That(preview.Snapshot.Positions.Any(row => row.Symbol.StartsWith("FDRXX", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(preview.Snapshot.Positions.Any(row => row.Symbol.Contains("Pending", StringComparison.OrdinalIgnoreCase)), Is.False);
            var agnc = preview.Snapshot.Positions.Single(row => row.Symbol == "AGNC");
            Assert.That(agnc.Shares, Is.EqualTo(72));
            Assert.That(agnc.AverageCost, Is.EqualTo(9.83m));
            Assert.That(agnc.Price, Is.EqualTo(8.65m));
            Assert.That(agnc.EstimatedAnnualIncome, Is.EqualTo(103.68m));

            var afterOpening = history.Rows.Where(row => row.RunDate > new DateTime(2026, 10, 31)).Sum(row => row.Amount ?? 0);
            Assert.That(preview.Snapshot.Cash, Is.EqualTo(3676.31m + afterOpening));
            Assert.That(state.Actions.Where(action => action.RunDate <= new DateTime(2026, 10, 31) && action.Kind != InvestmentActionKind.Opening && action.Kind != InvestmentActionKind.OpeningCash)
                .All(action => action.Status == InvestmentActionStatus.Skipped), Is.True);

            var repeat = InvestmentBookkeeper.Import(state, positions, history);
            Assert.That(repeat.Applied, Is.EqualTo(0));
            Assert.That(repeat.Snapshot.Cash, Is.EqualTo(preview.Snapshot.Cash));
            Assert.That(repeat.Snapshot.Positions.Single(row => row.Symbol == "AGNC").Shares, Is.EqualTo(72));
        }

        private static InvestmentBookState Book() => new() { AccountId = Guid.NewGuid() };
    }
}
