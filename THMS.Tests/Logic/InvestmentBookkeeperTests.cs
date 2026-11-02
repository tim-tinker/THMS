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
            Assert.That(agnc.Price, Is.Null);
            Assert.That(agnc.EstimatedAnnualIncome, Is.EqualTo(14.40m));
            Assert.That(agnc.AmountPerShare, Is.EqualTo(0.06m));
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
        public void LaterPositionsFile_ReplacesSharesBasisCashAndIncome()
        {
            var state = Book();
            InvestmentBookkeeper.Import(state, FidelityInvestmentFile.ParsePositions(Positions, PositionsName), history: null);
            var second = """
                Account number,Account name,Symbol,Description,Quantity,Cost basis total,Amount per share,Est. annual income,Ex-date,Pay date,Current value
                257460702,HSA,FDRXX**,HELD IN MONEY MARKET,,,,--,,,$80.00
                257460702,HSA,AGNC,AGNC INVT CORP COM,12,$120.00,$0.12,$17.28,Nov-13-2026,Dec-10-2026,$120.00
                """;
            var preview = InvestmentBookkeeper.Import(
                state,
                FidelityInvestmentFile.ParsePositions(second, "Portfolio_Positions_Nov-14-2026.csv"),
                history: null);

            var agnc = preview.Snapshot.Positions.Single();
            Assert.That(agnc.Shares, Is.EqualTo(12));
            Assert.That(agnc.TotalBasis, Is.EqualTo(120.00m));
            Assert.That(agnc.Price, Is.Null);
            Assert.That(agnc.EstimatedAnnualIncome, Is.EqualTo(17.28m));
            Assert.That(agnc.ExDate, Is.EqualTo(new DateTime(2026, 11, 13)));
            Assert.That(agnc.PayDate, Is.EqualTo(new DateTime(2026, 12, 10)));
            Assert.That(preview.Snapshot.Cash, Is.EqualTo(80m));
            Assert.That(preview.Messages, Has.Some.Contains("Updated positions"));
        }

        [Test]
        public void LaterPositionsFile_RemovesAHoldingMissingFromTheFile()
        {
            var state = Book();
            InvestmentBookkeeper.Import(state, FidelityInvestmentFile.ParsePositions(Positions, PositionsName), history: null);
            var second = """
                Account number,Account name,Symbol,Description,Quantity,Cost basis total,Amount per share,Est. annual income,Ex-date,Pay date,Current value
                257460702,HSA,FDRXX**,HELD IN MONEY MARKET,,,,--,,,$100.00
                """;
            var preview = InvestmentBookkeeper.Import(
                state,
                FidelityInvestmentFile.ParsePositions(second, "Portfolio_Positions_Nov-14-2026.csv"),
                history: null);

            Assert.That(preview.Snapshot.Positions, Is.Empty);
            Assert.That(preview.Snapshot.Cash, Is.EqualTo(100m));
        }

        [Test]
        public void PositionsFile_SplitsEveryAccountAndTreatsSweepCashAsCash()
        {
            var csv = """
                Account number,Account name,Symbol,Description,Quantity,Cost basis total,Amount per share,Est. annual income,Ex-date,Pay date,Current value
                Z37381599,Individual - TOD,FCASH**,HELD IN FCASH,,,,--,,,$2.83
                257297693,Tim's Rollover IRA,SPAXX**,HELD IN MONEY MARKET,,,,--,,,$10.00
                257297693,Tim's Rollover IRA,AGNC,AGNC INVT CORP COM,10,$98.30,$0.12,$14.40,Oct-30-2026,Nov-10-2026,$86.50
                257460702,HSA,FDRXX**,HELD IN MONEY MARKET,,,,--,,,$100.00
                257460702,HSA,Pending activity,,,,--,,,$5.00

                "The data and information in this spreadsheet is provided to you solely for your use."
                """;
            var accounts = FidelityInvestmentFile.ParsePositionAccounts(csv, "Fidelity_Portfolio_Positions_Nov-01-2026.csv");

            Assert.That(accounts.Select(file => file.AccountNumber), Is.EqualTo(new[] { "Z37381599", "257297693", "257460702" }));
            Assert.That(accounts[0].FileDate, Is.EqualTo(new DateTime(2026, 11, 1)));
            Assert.That(accounts[0].Cash, Is.EqualTo(2.83m));
            Assert.That(accounts[0].Holdings, Is.Empty);
            Assert.That(accounts[1].Cash, Is.EqualTo(10m));
            Assert.That(accounts[1].Holdings.Single().Symbol, Is.EqualTo("AGNC"));
            Assert.That(accounts[1].Holdings.Single().CostBasisTotal, Is.EqualTo(98.30m));
            Assert.That(accounts[2].Cash, Is.EqualTo(100m));
            Assert.That(accounts[2].Holdings, Is.Empty);
        }

        [Test]
        public void PreviewPositions_MatchesEachAccountNumberAndReportsTheRest()
        {
            var accounts = new InMemoryAccountDataStore();
            var ira = new InvestmentAccount { Name = "Fidelity IRA", AccountNumber = "257297693", Type = AccountType.Investment };
            var hsa = new InvestmentAccount { Name = "Fidelity HSA", AccountNumber = "257460702", Type = AccountType.Investment };
            var plan = new InvestmentAccount { Name = "SLB 401(k)", AccountNumber = "", Type = AccountType.Investment };
            accounts.UpsertAccount(ira);
            accounts.UpsertAccount(hsa);
            accounts.UpsertAccount(plan);
            var books = new InMemoryInvestmentBookStore();
            var orchestrator = new InvestmentImportOrchestrator(accounts, books, new InMemoryTransactionDataStore(), new InMemoryTransactionDataStore());
            var directory = Path.Combine(Path.GetTempPath(), "thms-positions-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "Fidelity_Portfolio_Positions_Nov-01-2026.csv");
                File.WriteAllText(path, """
                    Account number,Account name,Symbol,Description,Quantity,Cost basis total,Amount per share,Est. annual income,Ex-date,Pay date,Current value
                    Z37381599,Individual - TOD,FCASH**,HELD IN FCASH,,,,--,,,$2.83
                    257297693,Tim's Rollover IRA,SPAXX**,HELD IN MONEY MARKET,,,,--,,,$10.00
                    257297693,Tim's Rollover IRA,AGNC,AGNC,10,$98.30,$0.12,$14.40,Oct-30-2026,Nov-10-2026,$86.50
                    257460702,HSA,FDRXX**,HELD IN MONEY MARKET,,,,--,,,$100.00
                    257460702,HSA,AGNC,AGNC,4,$40.00,$0.12,$5.76,Oct-30-2026,Nov-10-2026,$34.60
                    """);
                var preview = orchestrator.PreviewPositions(path);
                Assert.That(preview.Messages, Has.Some.Contains("Z37381599"));
                Assert.That(preview.AccountNames.Values, Is.EquivalentTo(new[] { "Fidelity IRA", "Fidelity HSA" }));
                orchestrator.CommitPositions();
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }

            var savedIra = (InvestmentAccount)accounts.GetAllAccounts().Single(account => account.Id == ira.Id);
            var savedHsa = (InvestmentAccount)accounts.GetAllAccounts().Single(account => account.Id == hsa.Id);
            var savedPlan = (InvestmentAccount)accounts.GetAllAccounts().Single(account => account.Id == plan.Id);
            Assert.That(savedIra.CashBalance, Is.EqualTo(10m));
            Assert.That(savedIra.TracksPositions, Is.True);
            Assert.That(savedHsa.CashBalance, Is.EqualTo(100m));
            Assert.That(savedPlan.TracksPositions, Is.False);
            var iraState = new InvestmentBookState { AccountId = ira.Id, OpenedOn = savedIra.BookOpenedOn };
            iraState.Actions.AddRange(books.GetActions(ira.Id));
            iraState.Quotes.AddRange(books.GetQuotes(ira.Id));
            var iraPosition = InvestmentBookkeeper.Replay(iraState).Positions.Single();
            Assert.That(iraPosition.Shares, Is.EqualTo(10));
            Assert.That(iraPosition.TotalBasis, Is.EqualTo(98.30m));
            Assert.That(iraPosition.Price, Is.Null);
            Assert.That(iraPosition.EstimatedAnnualIncome, Is.EqualTo(14.40m));
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
                    11/04/2026,NORMAL DISTR PARTIAL (Cash),"",No Description,Cash,"",0,"","","","-20",131.20,""
                    11/03/2026,PARTIC CONTR CURRENT (Cash),"",No Description,Cash,"",0,"","","",50,151.20,""
                    11/02/2026,DIVIDEND RECEIVED AGNC (AGNC) (Cash),AGNC,AGNC,Cash,"",0,"","","",1.20,101.20,""
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
            Assert.That(saved.CashBalance, Is.EqualTo(131.20m));
            Assert.That(saved.PositionValue, Is.EqualTo(0m));
            var posted = transactions.GetPostedTransactions(account.Id).OrderBy(tx => tx.Date).ToList();
            Assert.That(posted.Select(tx => tx.Amount), Is.EqualTo(new[] { 1.20m, 50m, -20m }));
            Assert.That(posted.Select(tx => tx.Category), Is.EqualTo(new[]
            {
                InvestmentImportOrchestrator.IncomeCategoryName,
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
            Assert.That(agnc.AverageCost, Is.EqualTo(707.40m / 72));
            Assert.That(agnc.Price, Is.Null);
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
