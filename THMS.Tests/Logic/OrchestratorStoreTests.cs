using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Domain.Transportation;
using THMS.External;
using THMS.Logic.Orchestrators;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class ElectricContractOrchestratorTests
    {
        [Test]
        public void Save_AssignsId_AndGetByPeriod()
        {
            var store = new InMemoryFinanceDataStore();
            var orchestrator = new ElectricContractOrchestrator(store);

            Assert.That(orchestrator.GetElectricContracts("Month"), Is.Empty);

            var contract = new ElectricContract
            {
                Name = "Plan",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                EnergyChargeRate = 0.1m
            };
            orchestrator.Save(contract);
            Assert.That(contract.Id, Is.Not.EqualTo(Guid.Empty));

            var existingId = Guid.NewGuid();
            orchestrator.Save(new ElectricContract
            {
                Id = existingId,
                Name = "Kept",
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 12, 31)
            });

            Assert.That(orchestrator.GetElectricContracts("Year").Count(), Is.GreaterThan(0));
            Assert.That(orchestrator.GetElectricContracts("Lifetime").Count(), Is.GreaterThan(0));
        }
    }

    [TestFixture]
    public class SolarAndCircuitOrchestratorTests
    {
        private static string WriteTemp(string name, string contents)
        {
            var path = Path.Combine(Path.GetTempPath(), $"{name}-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, contents);
            return path;
        }

        [Test]
        public void SolarIntervalOrchestrator_UpdateAndQuery()
        {
            var store = new InMemoryEnergyDataStore();
            var orchestrator = new SolarIntervalOrchestrator(store);

            Assert.That(orchestrator.GetSolarIntervals("Month"), Is.Empty);

            var bad = WriteTemp("solar-bad", "a,b\n1,2");
            orchestrator.Update(bad);
            Assert.That(orchestrator.ErrorMessage, Is.Not.Empty);
            Assert.That(orchestrator.IntervalCount, Is.EqualTo(0));

            store.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = new DateTime(2026, 1, 1, 12, 0, 0),
                KiloWattHours = 0.5m
            });

            var good = WriteTemp("solar-good",
                "Date/Time,Energy Produced (Wh),Energy Consumed (Wh),Exported to Grid (Wh),Imported from Grid (Wh),Stored in batteries (Wh),Discharged from batteries (Wh)\n" +
                "2026-01-01 12:00:00,1000,800,10,200,50,20\n");
            orchestrator.Update(good);

            Assert.That(orchestrator.ErrorMessage, Is.Empty);
            Assert.That(orchestrator.IntervalCount, Is.EqualTo(1));
            Assert.That(orchestrator.GetSolarIntervals("Month").Count(), Is.EqualTo(1));
            Assert.That(orchestrator.GetSolarIntervals("Year").Count(), Is.EqualTo(1));
            Assert.That(store.GetHomeCircuitAttribution(DateTime.MinValue, DateTime.MaxValue), Is.Not.Empty);
        }

        [Test]
        public void HomeCircuitReadingOrchestrator_UpdateAndQuery()
        {
            var store = new InMemoryEnergyDataStore();
            var orchestrator = new HomeCircuitReadingOrchestrator(store);
            Assert.That(orchestrator.GetHomeCircuitReadings("Month"), Is.Empty);

            var bad = WriteTemp("circuit-bad", "x,y,z\n1,2,3");
            orchestrator.Update(bad);
            Assert.That(orchestrator.ErrorMessage, Is.Not.Empty);

            var good = WriteTemp("circuit-good",
                "Local SPAN Panel time (America/Chicago),Energy Data (Wh)\n" +
                "2026-02-01 08:00:00,1.25\n");
            orchestrator.Update(good);
            Assert.That(orchestrator.ReadingCount, Is.EqualTo(1));
            Assert.That(orchestrator.GetHomeCircuitReadings("Lifetime").Count(), Is.EqualTo(1));
        }

        [Test]
        public void HomeCircuitAttributionOrchestrator_UpdateAndQuery()
        {
            var store = new InMemoryEnergyDataStore();
            var orchestrator = new HomeCircuitAttributionOrchestrator(store);
            Assert.That(orchestrator.GetHomeCircuitAttributions("Month"), Is.Empty);

            store.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = new DateTime(2026, 3, 1, 12, 0, 0),
                EnergyProducedWh = 1000,
                EnergyConsumedWh = 400,
                ImportedFromGridWh = 100
            });
            store.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = new DateTime(2026, 3, 1, 12, 0, 0),
                KiloWattHours = 0.4m
            });

            orchestrator.Update();
            Assert.That(store.GetLatestHomeCircuitAttribution(), Is.Not.Null);
            Assert.That(orchestrator.GetHomeCircuitAttributions("Year").Count(), Is.GreaterThan(0));
        }
    }

    [TestFixture]
    public class EvChargeSessionOrchestratorTests
    {
        [Test]
        public void SaveUpdateGetAndCompleteHomeSessions()
        {
            var vehicles = new InMemoryVehicleDataStore();
            var energy = new InMemoryEnergyDataStore();
            var finance = new InMemoryFinanceDataStore();
            var ev = vehicles.GetAllVehicles().OfType<VehicleEv>().First();
            var orchestrator = new EvChargeSessionOrchestrator(vehicles, energy, finance)
            {
                VehicleId = ev.Id
            };

            Assert.That(orchestrator.GetEvVehicles().Any(), Is.True);
            Assert.That(orchestrator.GetLastSession(), Is.Null);
            Assert.That(orchestrator.GetEvChargeSessions("Month"), Is.Empty);

            var commercial = new CommercialEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 4, 1, 10, 0, 0),
                EndTime = new DateTime(2026, 4, 1, 12, 0, 0),
                SessionCost = 9
            };
            orchestrator.Save(commercial);
            orchestrator.Update(commercial);
            Assert.That(orchestrator.GetLastSession()!.Id, Is.EqualTo(commercial.Id));

            var other = new OtherEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 4, 2, 10, 0, 0),
                EndTime = new DateTime(2026, 4, 2, 11, 0, 0)
            };
            Assert.That(() => orchestrator.Save(other), Throws.InvalidOperationException);
            vehicles.UpsertBaseEvChargeSession(other);

            var homeBare = new HomeEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 4, 3, 8, 0, 0),
                EndTime = new DateTime(2026, 4, 3, 10, 0, 0)
            };
            orchestrator.Save(homeBare);
            var listed = orchestrator.GetEvChargeSessions("Year").ToList();
            Assert.That(listed.OfType<HomeEvChargeSession>().Any(s => s.Attribution is null), Is.True);

            energy.UpsertSolarProductionInterval(new SolarProductionInterval
            {
                Timestamp = new DateTime(2026, 4, 3, 8, 0, 0),
                EnergyProducedWh = 2000,
                EnergyConsumedWh = 1500,
                ImportedFromGridWh = 400
            });
            energy.UpsertHomeCircuitReading(new HomeCircuitReading
            {
                Timestamp = new DateTime(2026, 4, 3, 8, 0, 0),
                KiloWattHours = 0.6m
            });
            finance.UpsertElectricContract(new ElectricContract
            {
                Id = Guid.NewGuid(),
                Name = "Contract",
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                EnergyChargeRate = 0.10m,
                DeliveryChargeRate = 0.05m
            });

            var homeCompute = new HomeEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 4, 3, 8, 0, 0),
                EndTime = new DateTime(2026, 4, 3, 10, 0, 0)
            };
            orchestrator.Save(homeCompute);
            var completed = orchestrator.GetEvChargeSessions("Lifetime").OfType<HomeEvChargeSession>()
                .First(s => s.Id == homeCompute.Id);
            Assert.That(completed.Attribution, Is.Not.Null);
            Assert.That(completed.Billing, Is.Not.Null);

            vehicles.UpsertHomeEvChargeAttribution(homeCompute.Id, completed.Attribution!);
            vehicles.UpsertHomeEvChargeBilling(homeCompute.Id, completed.Billing!);
            var again = orchestrator.GetEvChargeSessions("Lifetime").OfType<HomeEvChargeSession>()
                .First(s => s.Id == homeCompute.Id);
            Assert.That(again.Attribution, Is.Not.Null);
            Assert.That(again.Billing, Is.Not.Null);

            orchestrator.VehicleId = Guid.Empty;
            Assert.That(orchestrator.GetLastSession(), Is.Not.Null);
            Assert.That(orchestrator.GetEvChargeSessions("Month").Any(), Is.True);

            var storedAttrib = new HomeEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 8, 1, 8, 0, 0),
                EndTime = new DateTime(2026, 8, 1, 9, 0, 0)
            };
            orchestrator.Save(storedAttrib);
            vehicles.UpsertHomeEvChargeAttribution(storedAttrib.Id, new HomeEvChargeAttribution { GridKwh = 1 });
            vehicles.UpsertHomeEvChargeBilling(storedAttrib.Id, new HomeEvChargeBilling { SessionCost = 2 });
            var loaded = orchestrator.GetEvChargeSessions("Lifetime").OfType<HomeEvChargeSession>()
                .First(s => s.Id == storedAttrib.Id);
            Assert.That(loaded.Attribution, Is.Not.Null);
            Assert.That(loaded.Billing, Is.Not.Null);

            var attribNoContract = new HomeEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2019, 1, 1, 8, 0, 0),
                EndTime = new DateTime(2019, 1, 1, 9, 0, 0)
            };
            orchestrator.Save(attribNoContract);
            vehicles.UpsertHomeEvChargeAttribution(attribNoContract.Id, new HomeEvChargeAttribution { GridKwh = 2 });
            var noBill = orchestrator.GetEvChargeSessions("Lifetime").OfType<HomeEvChargeSession>()
                .First(s => s.Id == attribNoContract.Id);
            Assert.That(noBill.Attribution, Is.Not.Null);
            Assert.That(noBill.Billing, Is.Null);
        }
    }

    [TestFixture]
    public class AccountSyncOrchestratorTests
    {
        [Test]
        public async Task SyncAsync_UpdatesEachAccountType()
        {
            var store = new InMemoryAccountDataStore();
            var fetcher = new FakeAccountFetcher
            {
                Accounts =
                [
                    new AccountDto
                    {
                        PlaidAccountId = "p1",
                        Current = 111,
                        Limit = 500,
                        Mask = "9999",
                        Name = "Plaid Bank"
                    }
                ]
            };
            var orchestrator = new AccountSyncOrchestrator(fetcher, store);

            Assert.That(
                async () => await orchestrator.SyncAsync(new BankAccount()),
                Throws.InvalidOperationException.With.Message.Contain("not linked"));

            var unlinkedMatch = new BankAccount
            {
                ExternalLink = new ExternalAccountLink { AccessToken = "t", PlaidAccountId = "missing" }
            };
            Assert.That(
                async () => await orchestrator.SyncAsync(unlinkedMatch),
                Throws.InvalidOperationException.With.Message.Contain("not found"));

            async Task<AccountSyncResult> Sync(Account account)
            {
                account.ExternalLink = new ExternalAccountLink { AccessToken = "tok", PlaidAccountId = "p1" };
                store.UpsertAccount(account);
                return await orchestrator.SyncAsync(account);
            }

            var bank = await Sync(new BankAccount { PostedBalance = 1 });
            Assert.That(bank.PostedBalance, Is.EqualTo(111));

            var credit = await Sync(new CreditAccount { PostedBalance = 2, CreditLimit = 10 });
            Assert.That(credit.CreditLimit, Is.EqualTo(500));

            var invest = await Sync(new InvestmentAccount { CashBalance = 3 });
            Assert.That(invest.CashBalance, Is.EqualTo(111));

            var loan = await Sync(new LoanAccount { Principal = 77 });
            Assert.That(loan.Principal, Is.EqualTo(77));

            var mortgage = await Sync(new MortgageAccount { Principal = 88 });
            Assert.That(mortgage.Principal, Is.EqualTo(88));

            fetcher.Accounts[0].Current = null;
            fetcher.Accounts[0].Limit = null;
            var creditKeep = new CreditAccount { PostedBalance = 9, CreditLimit = 12 };
            var kept = await Sync(creditKeep);
            Assert.That(kept.PostedBalance, Is.EqualTo(9));
            Assert.That(kept.CreditLimit, Is.EqualTo(12));
        }
    }

    [TestFixture]
    public class TransactionImportOrchestratorTests
    {
        [Test]
        public async Task ImportAsync_CategorizesAndPersists_AndTransferMatchThrowsOnReplace()
        {
            var store = new InMemoryTransactionDataStore();
            var fetcher = new FakeTransactionFetcher();
            var orchestrator = new TransactionImportOrchestrator(fetcher, store);
            var account = new BankAccount();

            Assert.That(
                async () => await orchestrator.ImportAsync(account),
                Throws.InvalidOperationException);

            account.ExternalLink = new ExternalAccountLink { AccessToken = "a", PlaidAccountId = "p" };
            fetcher.Transactions =
            [
                new TransactionDto { Pending = true, Amount = 1, Name = "Pending" },
                new TransactionDto { Amount = 5, Name = "Coffee", Category = "Food", Date = new DateTime(2026, 1, 2) },
                new TransactionDto { Amount = 6, Name = "Already", Date = new DateTime(2026, 1, 3) }
            ];

            var result = await orchestrator.ImportAsync(account);
            Assert.That(result.PostedImported, Is.EqualTo(2));

            var existing = store.GetPostedTransactions(account.Id).First(t => t.Description == "Already");
            existing.Category = "Kept";
            store.UpdatePostedTransaction(existing);

            fetcher.Transactions =
            [
                new TransactionDto { Amount = 7, Name = "NoPlaid", Date = new DateTime(2026, 1, 4) },
                new TransactionDto { Amount = 4, Name = "STARBUCKS #12", Date = new DateTime(2026, 1, 5) }
            ];
            await orchestrator.ImportAsync(account);
            Assert.That(
                store.GetPostedTransactions(account.Id).Any(t => t.Description == "NoPlaid" && t.Category == "Uncategorized"),
                Is.True);
            Assert.That(
                store.GetPostedTransactions(account.Id).Any(t => t.Description == "STARBUCKS #12" && t.Category == "Coffee"),
                Is.True);

            var otherAccount = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = otherAccount,
                Amount = 15,
                Date = new DateTime(2026, 2, 1),
                Description = "TRANSFER in"
            });
            fetcher.Transactions =
            [
                new TransactionDto
                {
                    Amount = 15,
                    Name = "TRANSFER out",
                    Date = new DateTime(2026, 2, 1)
                }
            ];
            Assert.That(async () => await orchestrator.ImportAsync(account), Throws.InvalidOperationException);

            fetcher.Transactions =
            [
                new TransactionDto { Amount = -15, Name = "Opposite", Date = new DateTime(2026, 2, 10) },
                new TransactionDto { Amount = 15, Name = "Same acct", Date = new DateTime(2026, 2, 20) }
            ];
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Amount = 15,
                Date = new DateTime(2026, 2, 20),
                Description = "self"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Amount = 15,
                Date = new DateTime(2026, 3, 1),
                Description = "far away"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Amount = 15,
                Date = new DateTime(2026, 2, 21),
                Description = "plain"
            });
            await orchestrator.ImportAsync(account);
        }

        [Test]
        public async Task ImportAsync_CompletesTransferMatchWithPermissiveStore()
        {
            var store = new PermissiveTransactionStore();
            var fetcher = new FakeTransactionFetcher();
            var orchestrator = new TransactionImportOrchestrator(fetcher, store);
            var account = new BankAccount
            {
                ExternalLink = new ExternalAccountLink { AccessToken = "a", PlaidAccountId = "p" }
            };

            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Amount = 40,
                Date = new DateTime(2026, 4, 1),
                Description = "ACH in"
            });
            fetcher.Transactions =
            [
                new TransactionDto
                {
                    Amount = 40,
                    Name = "ACH out",
                    Date = new DateTime(2026, 4, 1)
                }
            ];

            Assert.That(async () => await orchestrator.ImportAsync(account), Throws.InvalidOperationException);
        }
    }

    [TestFixture]
    public class ExternalTransactionAccessTests
    {
        [Test]
        public async Task FetchPostedTransactionsAsync_HandlesMissingLinkAndMapping()
        {
            var fetcher = new FakeTransactionFetcher
            {
                Transactions =
                [
                    new TransactionDto { Amount = 3, Name = "A", Category = "Cat", Date = new DateTime(2026, 1, 1) },
                    new TransactionDto { Amount = 4, Name = "B" }
                ]
            };
            var access = new ExternalTransactionAccess(fetcher);
            var account = new BankAccount { Id = Guid.NewGuid() };

            Assert.That(await access.FetchPostedTransactionsAsync(account, DateTime.Today, DateTime.Today), Is.Empty);

            account.ExternalLink = new ExternalAccountLink { AccessToken = " " };
            Assert.That(await access.FetchPostedTransactionsAsync(account, DateTime.Today, DateTime.Today), Is.Empty);

            account.ExternalLink.AccessToken = "tok";
            var posted = await access.FetchPostedTransactionsAsync(account, DateTime.Today.AddDays(-1), DateTime.Today);
            Assert.That(posted, Has.Count.EqualTo(2));
            Assert.That(posted[0].AccountId, Is.EqualTo(account.Id));
            Assert.That(posted[0].Date, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(posted[1].Date.Date, Is.EqualTo(DateTime.Today));
        }
    }

    [TestFixture]
    public class TransactionUpdaterOrchestratorTests
    {
        [Test]
        public void RunLedgerUpdate_CoversInactiveSkipTransferDetectAndForecast()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionUpdaterOrchestrator(accounts, txs);

            var empty = orchestrator.RunLedgerUpdate();
            Assert.That(empty.AccountsUpdated, Is.EqualTo(0));

            var account = new BankAccount { Name = "A", Institution = "B", AccountNumber = "1" };
            accounts.UpsertAccount(account);
            var noPosted = orchestrator.RunLedgerUpdate();
            Assert.That(noPosted.AccountsUpdated, Is.EqualTo(1));
            Assert.That(noPosted.TransfersDetected, Is.EqualTo(0));

            var date = DateTime.Today.AddDays(-2);
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Amount = -25,
                Date = date,
                Description = "Out"
            });
            txs.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                AccountId = account.Id,
                Amount = -25,
                Date = date.AddDays(-1)
            });
            var other = new BankAccount { Name = "B", Institution = "B", AccountNumber = "2" };
            accounts.UpsertAccount(other);
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = other.Id,
                Amount = 25,
                Date = date,
                Description = "In"
            });
            txs.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                AccountId = other.Id,
                Amount = 25,
                Date = date.AddDays(-1)
            });

            var weekly = Enumerable.Range(0, 3)
                .Select(i => new PostedTransaction
                {
                    AccountId = account.Id,
                    Description = "Netflix",
                    Amount = 15.99m,
                    Date = date.AddDays(-21 + i * 7)
                })
                .ToList();
            foreach (var tx in weekly)
                txs.AddPostedTransaction(tx);

            txs.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = account.Id,
                Description = "Netflix",
                Amount = 15.99m,
                Frequency = RecurrenceFrequency.Weekly,
                NextOccurrence = date.AddDays(7),
                IsActive = true
            });
            txs.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = account.Id,
                ToAccountId = other.Id,
                Description = "Sweep",
                Amount = 10,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = date.AddDays(10),
                IsActive = true
            });

            txs.AddFutureSingleTransaction(new FutureSingleTransaction
            {
                AccountId = account.Id,
                Amount = 15.99m,
                Date = date.AddDays(-7)
            });
            txs.AddFutureTransferTransaction(new FutureTransferTransaction
            {
                FromAccountId = account.Id,
                ToAccountId = account.Id,
                Amount = 25,
                Date = date
            });
            txs.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                AccountId = account.Id,
                Amount = 25,
                Date = date
            });
            txs.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                AccountId = account.Id,
                Amount = -25,
                Date = date
            });

            var result = orchestrator.RunLedgerUpdate();
            Assert.That(result.ForecastUpdated, Is.True);
            Assert.That(result.RollOffCompleted, Is.True);
        }

        [Test]
        public void RunLedgerUpdate_CreatesUtilityBudgetRuleAndForecast()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var account = new BankAccount { Name = "Checking", Institution = "Bank", AccountNumber = "1" };
            accounts.UpsertAccount(account);

            var month1 = DateTime.Today.AddMonths(-2);
            var month2 = DateTime.Today.AddMonths(-1);
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(month1.Year, month1.Month, 5),
                Amount = -30,
                Category = "Electric",
                Description = "Electric"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(month1.Year, month1.Month, 20),
                Amount = -10,
                Category = "Water",
                Description = "Water"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(month2.Year, month2.Month, 8),
                Amount = -20,
                Category = "Gas",
                Description = "Gas"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(month2.Year, month2.Month, 9),
                Amount = -99,
                Category = "Shopping",
                Description = "Not a utility"
            });

            txs.UpsertExpenseBudgetRule(null);
            Assert.That(txs.GetExpenseBudgetRules(account.Id), Is.Empty);

            var seeded = new ExpenseBudgetRuleFactory().Create("Utilities");
            seeded.AccountId = account.Id;
            seeded.NextOccurrence = DateTime.Today.AddMonths(1);
            txs.UpsertExpenseBudgetRule(seeded);

            var result = new TransactionUpdaterOrchestrator(accounts, txs).RunLedgerUpdate();
            Assert.That(result.ForecastUpdated, Is.True);

            var rule = txs.GetExpenseBudgetRules(account.Id).Single();
            Assert.That(rule.CurrentAverage, Is.EqualTo(-22m));
            Assert.That(rule.AccountId, Is.EqualTo(account.Id));
            Assert.That(rule.Category, Is.EqualTo("Utilities"));
            Assert.That(rule, Is.TypeOf<UtilityBudgetRule>());

            var budgetFutures = txs.GetFutureSingleTransactions(account.Id)
                .Where(f => f.Description == "Utilities Budget")
                .ToList();
            Assert.That(budgetFutures, Is.Not.Empty);
            Assert.That(budgetFutures.All(f => f.Amount == -22m), Is.True);
            Assert.That(budgetFutures.All(f => f.Category == "Utilities"), Is.True);
        }

        [Test]
        public void RunLedgerUpdate_UpdatesExistingUtilityBudgetRule()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var account = new BankAccount { Name = "Checking", Institution = "Bank", AccountNumber = "2" };
            accounts.UpsertAccount(account);

            var existingId = Guid.NewGuid();
            var originalNext = DateTime.Today.AddMonths(1);
            txs.UpsertExpenseBudgetRule(new UtilityBudgetRule
            {
                Id = existingId,
                AccountId = account.Id,
                CurrentAverage = -5,
                NextOccurrence = originalNext
            });

            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = DateTime.Today.AddDays(-3),
                Amount = -48,
                Category = "Electric"
            });

            new TransactionUpdaterOrchestrator(accounts, txs).RunLedgerUpdate();

            var updated = txs.GetExpenseBudgetRules(account.Id).Single();
            Assert.That(updated.Id, Is.EqualTo(existingId));
            Assert.That(updated.CurrentAverage, Is.EqualTo(-48m));
            Assert.That(updated.NextOccurrence, Is.GreaterThan(originalNext));
        }
    }

    [TestFixture]
    public class RegisterUpdateOrchestratorTests
    {
        [Test]
        public async Task UpdateAccountAsync_RunsInjectedPipeline()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var fetcher = new FakeAccountFetcher
            {
                Accounts = [new AccountDto { PlaidAccountId = "p", Current = 5, Mask = "1", Name = "N" }]
            };
            var txFetcher = new FakeTransactionFetcher { Transactions = [] };
            var account = new BankAccount
            {
                ExternalLink = new ExternalAccountLink { AccessToken = "t", PlaidAccountId = "p" }
            };
            accounts.UpsertAccount(account);

            var orchestrator = new RegisterUpdateOrchestrator(
                new AccountSyncOrchestrator(fetcher, accounts),
                new TransactionImportOrchestrator(txFetcher, txs),
                new TransactionOrchestrator(txs));

            await orchestrator.UpdateAccountAsync(account);
            Assert.That(account.PostedBalance, Is.EqualTo(5));
        }
    }
}
