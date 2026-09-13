using THMS.Data.Stores;
using THMS.Domain.Energy;
using THMS.Domain.Finance;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Domain.Transportation;
using THMS.External;
using THMS.Logic.Finance.Budget;
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

            var timed = new ElectricContract
            {
                Name = "Timed",
                StartDate = new DateTime(2026, 8, 1, 15, 30, 0),
                EndDate = new DateTime(2026, 8, 31, 22, 45, 0)
            };
            orchestrator.Save(timed);
            Assert.That(timed.StartDate, Is.EqualTo(new DateTime(2026, 8, 1)));
            Assert.That(timed.EndDate, Is.EqualTo(new DateTime(2026, 8, 31)));
            Assert.That(store.GetElectricContract(timed.Id)!.StartDate.TimeOfDay, Is.EqualTo(TimeSpan.Zero));

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
            orchestrator.Update([bad]);
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
            orchestrator.Update([good]);

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
            orchestrator.Update([bad]);
            Assert.That(orchestrator.ErrorMessage, Is.Not.Empty);

            var good = WriteTemp("circuit-good",
                "Local SPAN Panel time (America/Chicago),Energy Data (Wh)\n" +
                "2026-02-01 08:00:00,1.25\n");
            orchestrator.Update([good]);
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
            Assert.That(listed.Select(s => s.StartTime), Is.Ordered.Descending);
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
            Assert.That(completed.KwhDrawn, Is.EqualTo(completed.SolarKwh + completed.BatteryKwh + completed.GridKwh));
            Assert.That(completed.KwhDrawn, Is.GreaterThan(0));
            Assert.That(
                completed.SessionCost,
                Is.EqualTo(completed.GridKwh * (0.10m + 0.05m)));

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
            Assert.That(loaded.KwhDrawn, Is.EqualTo(1m));
            Assert.That(loaded.SessionCost, Is.EqualTo(2m));

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

            finance.UpsertElectricContract(new ElectricContract
            {
                Id = Guid.NewGuid(),
                Name = "January 2027",
                StartDate = new DateTime(2027, 1, 1, 15, 0, 0),
                EndDate = new DateTime(2027, 1, 15, 0, 0, 0),
                EnergyChargeRate = 0.12m,
                DeliveryChargeRate = 0.06m
            });
            var importedZeros = new HomeEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2026, 8, 30, 13, 44, 0),
                EndTime = new DateTime(2026, 8, 30, 17, 57, 0),
                KwhDrawn = 0,
                SessionCost = 0
            };
            orchestrator.Save(importedZeros);
            vehicles.UpsertHomeEvChargeAttribution(importedZeros.Id, new HomeEvChargeAttribution
            {
                SolarKwh = 17.642m,
                BatteryKwh = 0,
                GridKwh = 23.868m
            });
            vehicles.UpsertHomeEvChargeBilling(importedZeros.Id, new HomeEvChargeBilling { SessionCost = 0 });
            var derived = orchestrator.GetEvChargeSessions("Lifetime").OfType<HomeEvChargeSession>()
                .First(s => s.Id == importedZeros.Id);
            Assert.That(derived.KwhDrawn, Is.EqualTo(17.642m + 23.868m));
            Assert.That(derived.SessionCost, Is.EqualTo(23.868m * (0.10m + 0.05m)));

            var endDateMidnight = new HomeEvChargeSession
            {
                VehicleId = ev.Id,
                StartTime = new DateTime(2027, 1, 15, 13, 44, 0),
                EndTime = new DateTime(2027, 1, 15, 17, 0, 0),
                KwhDrawn = 0,
                SessionCost = 0
            };
            orchestrator.Save(endDateMidnight);
            vehicles.UpsertHomeEvChargeAttribution(endDateMidnight.Id, new HomeEvChargeAttribution
            {
                GridKwh = 10m
            });
            var billedOnExpiry = orchestrator.GetEvChargeSessions("Lifetime").OfType<HomeEvChargeSession>()
                .First(s => s.Id == endDateMidnight.Id);
            Assert.That(billedOnExpiry.SessionCost, Is.EqualTo(10m * (0.12m + 0.06m)));
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
                store.GetPostedTransactions(account.Id).Any(t => t.Description == "STARBUCKS #12" && t.Category == "Restaurants"),
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
        public void RunLedgerUpdate_PostedBalanceUsesLatestStatementPlusLaterActivity()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var orchestrator = new TransactionUpdaterOrchestrator(accounts, txs, statements);

            var account = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                StartingBalance = 0,
                PostedBalance = 0
            };
            accounts.UpsertAccount(account);

            var statementDate = new DateTime(2026, 8, 31);
            statements.Save(new BankStatement
            {
                AccountId = account.Id,
                StatementDate = statementDate,
                PeriodStart = new DateTime(2026, 8, 1),
                EndingBalance = 1000
            });

            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = statementDate.AddDays(-10),
                Amount = 500,
                Description = "Already on statement"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = statementDate,
                Amount = 75,
                Description = "Same-day as statement"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = statementDate.AddDays(3),
                Amount = -40,
                Description = "After statement"
            });
            txs.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                AccountId = account.Id,
                Date = statementDate.AddDays(4),
                Amount = 15,
                Description = "Transfer after statement"
            });

            orchestrator.RunLedgerUpdate();

            var updated = (BankAccount)accounts.GetAllAccounts().Single(a => a.Id == account.Id);
            Assert.That(updated.PostedBalance, Is.EqualTo(975m));
        }

        [Test]
        public void RunLedgerUpdate_WithoutStatementLeavesStoredPostedBalanceUnchanged()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var orchestrator = new TransactionUpdaterOrchestrator(accounts, txs, statements);

            var account = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                StartingBalance = 100,
                PostedBalance = 2500
            };
            accounts.UpsertAccount(account);
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = DateTime.Today.AddDays(-2),
                Amount = -25,
                Description = "Spend"
            });

            orchestrator.RunLedgerUpdate();

            var updated = (BankAccount)accounts.GetAllAccounts().Single();
            Assert.That(updated.PostedBalance, Is.EqualTo(2500m));
        }

        [Test]
        public void RunLedgerUpdate_PersistsNewlyDetectedRecurringRules()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionUpdaterOrchestrator(accounts, txs);

            var account = new BankAccount { Name = "Checking", Institution = "Bank", AccountNumber = "1" };
            accounts.UpsertAccount(account);

            var date = DateTime.Today.AddDays(-2);
            foreach (var i in Enumerable.Range(0, 3))
            {
                txs.AddPostedTransaction(new PostedTransaction
                {
                    AccountId = account.Id,
                    Description = "Netflix",
                    Amount = 15.99m,
                    Date = date.AddDays(-21 + i * 7)
                });
            }

            var other = new BankAccount { Name = "Savings", Institution = "Bank", AccountNumber = "2" };
            accounts.UpsertAccount(other);
            foreach (var i in Enumerable.Range(0, 3))
            {
                txs.AddPostedTransferTransaction(new PostedTransferTransaction
                {
                    AccountId = account.Id,
                    Description = "Sweep",
                    Amount = 50,
                    Date = date.AddDays(-21 + i * 7)
                });
            }

            var result = orchestrator.RunLedgerUpdate();
            Assert.That(result.RecurringRulesUpdated, Is.EqualTo(2));

            var singleRules = txs.GetRecurringSingleRules(account.Id).ToList();
            Assert.That(singleRules, Has.Count.EqualTo(1));
            Assert.That(singleRules[0].Description, Is.EqualTo("Netflix"));

            var transferRules = txs.GetRecurringTransferRules(account.Id).ToList();
            Assert.That(transferRules, Has.Count.EqualTo(1));
            Assert.That(transferRules[0].Description, Is.EqualTo("Sweep"));
            Assert.That(singleRules[0].IsUserCreated, Is.False);
            Assert.That(transferRules[0].IsUserCreated, Is.False);
        }

        [Test]
        public void RunLedgerUpdate_DoesNotDeleteOrOverwriteUserCreatedRules()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionUpdaterOrchestrator(accounts, txs);

            var account = new BankAccount { Name = "Checking", Institution = "Bank", AccountNumber = "1" };
            accounts.UpsertAccount(account);

            var userRuleId = Guid.NewGuid();
            txs.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                Id = userRuleId,
                AccountId = account.Id,
                Description = "Netflix",
                Amount = 99,
                Frequency = RecurrenceFrequency.Weekly,
                NextOccurrence = DateTime.Today.AddDays(7),
                IsActive = true,
                IsUserCreated = true
            });

            var date = DateTime.Today.AddDays(-2);
            foreach (var i in Enumerable.Range(0, 3))
            {
                txs.AddPostedTransaction(new PostedTransaction
                {
                    AccountId = account.Id,
                    Description = "Netflix",
                    Amount = 15.99m,
                    Date = date.AddDays(-21 + i * 7)
                });
            }

            orchestrator.RunLedgerUpdate();

            var rules = txs.GetRecurringSingleRules(account.Id).ToList();
            Assert.That(rules, Has.Count.EqualTo(1));
            Assert.That(rules[0].Id, Is.EqualTo(userRuleId));
            Assert.That(rules[0].Amount, Is.EqualTo(99));
            Assert.That(rules[0].IsUserCreated, Is.True);
        }

        [Test]
        public void RunLedgerUpdate_DoesNotInsertDuplicateRecurringRulesAcrossAccountsOrReruns()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionUpdaterOrchestrator(accounts, txs);

            var checking = new BankAccount { Name = "Checking", Institution = "Bank", AccountNumber = "1" };
            var savings = new BankAccount { Name = "Savings", Institution = "Bank", AccountNumber = "2" };
            var cash = new BankAccount { Name = "Cash", Institution = "Bank", AccountNumber = "3" };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(savings);
            accounts.UpsertAccount(cash);

            foreach (var month in Enumerable.Range(1, 5))
            {
                txs.AddPostedTransaction(new PostedTransaction
                {
                    AccountId = checking.Id,
                    Description = "LESLIES POOLMART",
                    Amount = -45.67m,
                    Date = new DateTime(2026, month, 20)
                });
            }

            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = savings.Id,
                Description = "Interest",
                Amount = 1.25m,
                Date = new DateTime(2026, 5, 21)
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = cash.Id,
                Description = "ATM",
                Amount = -20,
                Date = new DateTime(2026, 5, 22)
            });

            orchestrator.RunLedgerUpdate();
            orchestrator.RunLedgerUpdate();

            var rules = txs.GetRecurringSingleRules(checking.Id).ToList();
            Assert.That(rules, Has.Count.EqualTo(1));
            Assert.That(rules[0].Description, Is.EqualTo("LESLIES POOLMART"));
            Assert.That(rules[0].NextOccurrence, Is.EqualTo(new DateTime(2026, 6, 20)));
        }

        [Test]
        public void AddRecurringSingleRule_UpsertsMatchingPatternInsteadOfInserting()
        {
            var txs = new InMemoryTransactionDataStore();
            var accountId = Guid.NewGuid();
            txs.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Description = "LESLIES POOLMART",
                Amount = -45.67m,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = new DateTime(2026, 6, 20)
            });
            txs.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Description = "leslies poolmart",
                Amount = -45.67m,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = new DateTime(2026, 6, 20)
            });

            Assert.That(txs.GetRecurringSingleRules(accountId).Count(), Is.EqualTo(1));
        }

        [Test]
        public void RunLedgerUpdate_DoesNotUpdateRolledOffFuturesFromPriorAccount()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionUpdaterOrchestrator(accounts, txs);

            var checking = new BankAccount { Name = "Checking", Institution = "Bank", AccountNumber = "1" };
            var savings = new BankAccount { Name = "Savings", Institution = "Bank", AccountNumber = "2" };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(savings);

            var date = DateTime.Today.AddDays(-2);
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Description = "Netflix",
                Amount = 15.99m,
                Date = date.AddDays(-7)
            });
            txs.AddFutureSingleTransaction(new FutureSingleTransaction
            {
                AccountId = checking.Id,
                Description = "Netflix",
                Amount = 15.99m,
                Date = date.AddDays(-7)
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = savings.Id,
                Description = "Interest",
                Amount = 1.25m,
                Date = date
            });

            Assert.That(() => orchestrator.RunLedgerUpdate(), Throws.Nothing);
            Assert.That(txs.GetFutureSingleTransactions(checking.Id), Is.Empty);
        }

        [Test]
        public void RunLedgerUpdate_RefreshesBudgetPeriodActualsAndRecommended()
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
                CategoryId = DefaultExpenseCategories.ElectricId,
                Description = "Electric"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(month1.Year, month1.Month, 20),
                Amount = -10,
                Category = "Water",
                CategoryId = DefaultExpenseCategories.WaterId,
                Description = "Water"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(month2.Year, month2.Month, 8),
                Amount = -20,
                Category = "Gas",
                CategoryId = DefaultExpenseCategories.GasId,
                Description = "Gas"
            });
            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(month2.Year, month2.Month, 9),
                Amount = -99,
                Category = "Shopping",
                CategoryId = DefaultExpenseCategories.RestaurantsId,
                Description = "Not a utility"
            });

            var budgets = new BudgetOrchestrator(txs);
            budgets.AddRule(new ExpenseBudgetRule
            {
                AccountId = account.Id,
                BudgetName = "Utilities",
                IncludedCategoryIds = DefaultExpenseCategories.UtilityMemberIds.ToList(),
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -200,
                IsActive = true
            });

            var result = new TransactionUpdaterOrchestrator(accounts, txs).RunLedgerUpdate();
            Assert.That(result.ForecastUpdated, Is.True);

            var rule = txs.GetExpenseBudgetRules(account.Id).Single();
            Assert.That(rule.BudgetName, Is.EqualTo("Utilities"));
            Assert.That(rule.AccountId, Is.EqualTo(account.Id));

            var period = txs.GetActiveBudgetHistory(rule.Id);
            Assert.That(period, Is.Not.Null);
            Assert.That(period!.RecommendedAmount, Is.GreaterThan(0));
            Assert.That(period.Remaining, Is.EqualTo(period.StartingBalance + Math.Abs(period.BudgetAmount) - period.ActualExpenses));
            Assert.That(txs.GetFutureSingleTransactions(account.Id), Is.Empty);
        }

        [Test]
        public void RunLedgerUpdate_UpdatesExistingBudgetPeriodActuals()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var account = new BankAccount { Name = "Checking", Institution = "Bank", AccountNumber = "2" };
            accounts.UpsertAccount(account);

            var budgets = new BudgetOrchestrator(txs);
            budgets.AddRule(new ExpenseBudgetRule
            {
                AccountId = account.Id,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });

            var ruleId = txs.GetExpenseBudgetRules(account.Id).Single().Id;
            var originalPeriodId = txs.GetActiveBudgetHistory(ruleId)!.Id;

            txs.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = DateTime.Today,
                Amount = -48,
                Category = "Electric",
                CategoryId = DefaultExpenseCategories.ElectricId
            });

            new TransactionUpdaterOrchestrator(accounts, txs).RunLedgerUpdate();

            var updated = txs.GetActiveBudgetHistory(ruleId);
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated!.Id, Is.EqualTo(originalPeriodId));
            Assert.That(updated.ActualExpenses, Is.EqualTo(48m));
            Assert.That(updated.Remaining, Is.EqualTo(52m));
            Assert.That(updated.EndingBalance, Is.EqualTo(52m));
        }
    }

    [TestFixture]
    public class BudgetOrchestratorTests
    {
        [Test]
        public void AddUpdateDeleteRule_PersistsDefinitionAndHistory()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();

            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId, DefaultExpenseCategories.WaterId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -150,
                IsActive = true
            });

            var rule = orchestrator.GetRules(accountId).Single();
            Assert.That(rule.BudgetName, Is.EqualTo("Utilities"));
            Assert.That(rule.IncludedCategoryIds, Is.EquivalentTo(new[] { DefaultExpenseCategories.ElectricId, DefaultExpenseCategories.WaterId }));
            Assert.That(rule.DefaultBudgetAmount, Is.EqualTo(-150m));
            Assert.That(rule.BudgetFrequency, Is.EqualTo(BudgetFrequency.Monthly));

            var period = orchestrator.GetActivePeriod(rule.Id);
            Assert.That(period, Is.Not.Null);
            Assert.That(period!.BudgetAmount, Is.EqualTo(150m));
            Assert.That(period.StartingBalance, Is.EqualTo(0m));
            Assert.That(period.IsClosed, Is.False);

            rule.BudgetName = "Home utilities";
            rule.IncludedCategoryIds = [DefaultExpenseCategories.ElectricId];
            rule.DefaultBudgetAmount = -175;
            rule.BudgetFrequency = BudgetFrequency.Weekly;
            orchestrator.UpdateRule(rule);

            var updated = orchestrator.GetRule(rule.Id);
            Assert.That(updated!.BudgetName, Is.EqualTo("Home utilities"));
            Assert.That(updated.IncludedCategoryIds, Is.EquivalentTo(new[] { DefaultExpenseCategories.ElectricId }));
            Assert.That(updated.DefaultBudgetAmount, Is.EqualTo(-175m));
            Assert.That(updated.BudgetFrequency, Is.EqualTo(BudgetFrequency.Weekly));

            orchestrator.DeleteRule(rule.Id);
            Assert.That(orchestrator.GetRules(accountId), Is.Empty);
            Assert.That(store.GetExpenseBudgetHistory(rule.Id), Is.Empty);
        }

        [Test]
        public void EnsureSuggestedRules_CreatesMonthlyBudgetsFromSpending()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            var month = DateTime.Today.AddMonths(-1);
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = accountId,
                Date = new DateTime(month.Year, month.Month, 4),
                Amount = -80,
                Category = DefaultExpenseCategories.Groceries,
                CategoryId = DefaultExpenseCategories.GroceriesId,
                Description = "Market"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = accountId,
                Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 6),
                Amount = -90,
                Category = DefaultExpenseCategories.Groceries,
                CategoryId = DefaultExpenseCategories.GroceriesId,
                Description = "Market"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = accountId,
                Date = DateTime.Today,
                Amount = -200,
                Category = DefaultExpenseCategories.Payment,
                CategoryId = DefaultExpenseCategories.PaymentId,
                Description = "Card payment"
            });

            var created = orchestrator.EnsureSuggestedRules(accountId);

            Assert.That(created, Is.GreaterThan(0));
            var rules = orchestrator.GetRules(accountId);
            Assert.That(rules, Has.Some.Matches<ExpenseBudgetRule>(r =>
                r.BudgetName == DefaultExpenseCategories.Groceries &&
                r.IncludedCategoryIds.Contains(DefaultExpenseCategories.GroceriesId)));
            Assert.That(rules, Has.None.Matches<ExpenseBudgetRule>(r =>
                r.IncludedCategoryIds.Contains(DefaultExpenseCategories.PaymentId)));
            Assert.That(orchestrator.EnsureSuggestedRules(accountId), Is.EqualTo(0));
        }

        [Test]
        public void RefreshAccount_RollsForwardEndedPeriod()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });

            var ruleId = orchestrator.GetRules(accountId).Single().Id;
            var original = store.GetActiveBudgetHistory(ruleId)!;
            original.PeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            original.PeriodEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
            store.UpdateExpenseBudgetHistory(original);

            orchestrator.RefreshAccount(accountId);

            var closed = store.GetExpenseBudgetHistoryById(original.Id);
            Assert.That(closed, Is.Not.Null);
            Assert.That(closed!.IsClosed, Is.True);

            var active = store.GetActiveBudgetHistory(ruleId);
            Assert.That(active, Is.Not.Null);
            Assert.That(active!.Id, Is.Not.EqualTo(original.Id));
            Assert.That(active.PeriodStart, Is.EqualTo(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)));
            Assert.That(store.GetExpenseBudgetHistory(ruleId).Count(h => !h.IsClosed), Is.EqualTo(1));
        }

        [Test]
        public void SavePeriod_OverridesBudgetAmountAndRemaining()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = accountId,
                Date = DateTime.Today,
                Amount = -40,
                Category = "Electric",
                CategoryId = DefaultExpenseCategories.ElectricId
            });

            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });

            var period = orchestrator.GetActivePeriod(orchestrator.GetRules(accountId).Single().Id)!;
            Assert.That(period.ActualExpenses, Is.EqualTo(40m));
            Assert.That(period.Remaining, Is.EqualTo(60m));
            Assert.That(period.EndingBalance, Is.EqualTo(60m));

            period.BudgetAmount = 80;
            orchestrator.SavePeriod(period);

            var saved = orchestrator.GetActivePeriod(period.BudgetRuleId)!;
            Assert.That(saved.BudgetAmount, Is.EqualTo(80m));
            Assert.That(saved.Remaining, Is.EqualTo(40m));
            Assert.That(saved.EndingBalance, Is.EqualTo(40m));
        }

        [Test]
        public void RollForward_ClosesCurrentAndOpensNextPeriod()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });

            var ruleId = orchestrator.GetRules(accountId).Single().Id;
            var current = orchestrator.GetActivePeriod(ruleId)!;
            var created = orchestrator.RollForward(ruleId);

            Assert.That(store.GetExpenseBudgetHistoryById(current.Id)!.IsClosed, Is.True);
            Assert.That(created.IsClosed, Is.False);
            Assert.That(created.StartingBalance, Is.EqualTo(store.GetExpenseBudgetHistoryById(current.Id)!.EndingBalance));
            Assert.That(created.PeriodStart, Is.EqualTo(BudgetPeriodCalculator.NextPeriod(current.PeriodEnd, BudgetFrequency.Monthly).Start));
            Assert.That(store.GetActiveBudgetHistory(ruleId)!.Id, Is.EqualTo(created.Id));
        }

        [Test]
        public void ClosePeriod_DoesNotCreateOverlappingActivePeriod()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });

            var ruleId = orchestrator.GetRules(accountId).Single().Id;
            var current = orchestrator.GetActivePeriod(ruleId)!;
            orchestrator.ClosePeriod(current.Id);
            orchestrator.RefreshAccount(accountId);

            Assert.That(orchestrator.GetActivePeriod(ruleId), Is.Null);
            Assert.That(store.GetExpenseBudgetHistory(ruleId).Count(h => !h.IsClosed), Is.EqualTo(0));
        }

        [Test]
        public void RefreshAccount_RecommendedIncludesForecast_NotPostedActuals()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            var lastMonth = DateTime.Today.AddMonths(-1);

            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = accountId,
                Date = new DateTime(lastMonth.Year, lastMonth.Month, 8),
                Amount = -10,
                Category = "Electric",
                CategoryId = DefaultExpenseCategories.ElectricId
            });
            store.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Description = "Electric",
                Amount = -20,
                Category = "Electric",
                CategoryId = DefaultExpenseCategories.ElectricId,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today,
                IsActive = true
            });

            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });

            var period = orchestrator.GetActivePeriod(orchestrator.GetRules(accountId).Single().Id)!;
            Assert.That(period.ActualExpenses, Is.EqualTo(0m));
            Assert.That(period.RecommendedAmount, Is.GreaterThan(0m));
            Assert.That(period.RecommendedAmount, Is.Not.EqualTo(period.ActualExpenses));
        }

        [Test]
        public void RollForward_CarriesEndingBalanceIntoNextStartingBalance()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = accountId,
                Date = DateTime.Today,
                Amount = -40,
                CategoryId = DefaultExpenseCategories.ElectricId
            });

            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Utilities",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = 100,
                IsActive = true
            });

            var ruleId = orchestrator.GetRules(accountId).Single().Id;
            var current = orchestrator.GetActivePeriod(ruleId)!;
            Assert.That(current.ActualExpenses, Is.EqualTo(40m));
            Assert.That(current.Remaining, Is.EqualTo(60m));
            Assert.That(current.EndingBalance, Is.EqualTo(60m));

            var next = orchestrator.RollForward(ruleId);
            Assert.That(next.StartingBalance, Is.EqualTo(60m));
            Assert.That(next.BudgetAmount, Is.EqualTo(100m));
            Assert.That(next.Remaining, Is.EqualTo(next.StartingBalance + next.BudgetAmount - next.ActualExpenses));
        }

        [Test]
        public void SetStartingBalance_UpdatesRemainingAndEnding()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Groceries",
                IncludedCategoryIds = [DefaultExpenseCategories.GroceriesId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = 100,
                IsActive = true
            });

            var period = orchestrator.GetActivePeriod(orchestrator.GetRules(accountId).Single().Id)!;
            orchestrator.SetStartingBalance(period.Id, 25);

            var updated = orchestrator.GetActivePeriod(period.BudgetRuleId)!;
            Assert.That(updated.StartingBalance, Is.EqualTo(25m));
            Assert.That(updated.Remaining, Is.EqualTo(125m));
            Assert.That(updated.EndingBalance, Is.EqualTo(125m));

            orchestrator.SetStartingBalance(updated.Id, 0);
            var reset = orchestrator.GetActivePeriod(period.BudgetRuleId)!;
            Assert.That(reset.StartingBalance, Is.EqualTo(0m));
            Assert.That(reset.Remaining, Is.EqualTo(100m));
        }

        [Test]
        public void SetStartingBalance_RejectsClosedPeriod()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Groceries",
                IncludedCategoryIds = [DefaultExpenseCategories.GroceriesId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = 100,
                IsActive = true
            });

            var period = orchestrator.GetActivePeriod(orchestrator.GetRules(accountId).Single().Id)!;
            orchestrator.ClosePeriod(period.Id);

            Assert.That(
                () => orchestrator.SetStartingBalance(period.Id, 0),
                Throws.InvalidOperationException.With.Message.Contains("Closed periods"));
        }

        [Test]
        public void TransferBalance_MovesStartingBetweenOpenPeriods()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Groceries",
                IncludedCategoryIds = [DefaultExpenseCategories.GroceriesId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = 100,
                IsActive = true
            });
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Dining",
                IncludedCategoryIds = [DefaultExpenseCategories.RestaurantsId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = 50,
                IsActive = true
            });

            var groceries = orchestrator.GetRules(accountId).Single(r => r.BudgetName == "Groceries");
            var dining = orchestrator.GetRules(accountId).Single(r => r.BudgetName == "Dining");
            orchestrator.SetStartingBalance(orchestrator.GetActivePeriod(groceries.Id)!.Id, 40);

            orchestrator.TransferBalance(groceries.Id, dining.Id, 15);

            var groceriesPeriod = orchestrator.GetActivePeriod(groceries.Id)!;
            var diningPeriod = orchestrator.GetActivePeriod(dining.Id)!;
            Assert.That(groceriesPeriod.StartingBalance, Is.EqualTo(25m));
            Assert.That(groceriesPeriod.Remaining, Is.EqualTo(125m));
            Assert.That(diningPeriod.StartingBalance, Is.EqualTo(15m));
            Assert.That(diningPeriod.Remaining, Is.EqualTo(65m));
        }

        [Test]
        public void TransferBalance_RejectsSameBudgetAndNonPositiveAmount()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new BudgetOrchestrator(store);
            var accountId = Guid.NewGuid();
            orchestrator.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Groceries",
                IncludedCategoryIds = [DefaultExpenseCategories.GroceriesId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = 100,
                IsActive = true
            });

            var ruleId = orchestrator.GetRules(accountId).Single().Id;
            Assert.That(() => orchestrator.TransferBalance(ruleId, ruleId, 10), Throws.ArgumentException);
            Assert.That(
                () => orchestrator.TransferBalance(ruleId, Guid.NewGuid(), 0),
                Throws.ArgumentException);
        }
    }

    [TestFixture]
    public class CategoryOrchestratorTests
    {
        [Test]
        public void CreateAndRename_DoesNotChangeTransactionCategoryId()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new CategoryOrchestrator(store, store);
            var created = orchestrator.CreateCategory("MICROSOFT*STORE MICROSOFT.COMWA");
            var accountId = Guid.NewGuid();
            var tx = new PostedTransaction
            {
                AccountId = accountId,
                Date = new DateTime(2026, 1, 8),
                Description = "MICROSOFT*STORE",
                Amount = -12,
                CategoryId = created.Id,
                Category = created.Name
            };
            store.AddPostedTransaction(tx);

            created.Name = "Software";
            orchestrator.UpdateCategory(created);

            var stored = store.GetPostedTransaction(tx.Id);
            Assert.That(store.GetCategory(created.Id)!.Name, Is.EqualTo("Software"));
            Assert.That(stored!.CategoryId, Is.EqualTo(created.Id));
            Assert.That(stored.Category, Is.EqualTo("MICROSOFT*STORE MICROSOFT.COMWA"));
        }

        [Test]
        public void AssignParent_BuildsHierarchyAndUsageCountsTransactions()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new CategoryOrchestrator(store, store);
            var gaming = orchestrator.CreateCategory("Gaming", DefaultExpenseCategories.RestaurantsId);

            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Date = DateTime.Today,
                Description = "FS *Gameloft Club",
                Amount = -8,
                CategoryId = gaming.Id,
                Category = gaming.Name
            });

            var tree = orchestrator.GetCategoryTree();
            Assert.That(tree.Any(c => c.Id == gaming.Id && c.ParentCategoryId == DefaultExpenseCategories.RestaurantsId), Is.True);
            Assert.That(orchestrator.GetUsage(gaming.Id).TransactionCount, Is.EqualTo(1));
            Assert.That(orchestrator.GetUsage(gaming.Id).Total, Is.EqualTo(1));
        }

        [Test]
        public void MergeCategories_RetargetsTransactionsBudgetsRulesAndAssignments()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new CategoryOrchestrator(store, store);
            var keep = orchestrator.CreateCategory("Software");
            var retire = orchestrator.CreateCategory("MICROSOFT*STORE MICROSOFT.COMWA");
            var child = orchestrator.CreateCategory("Office Apps", retire.Id);
            var accountId = Guid.NewGuid();

            var posted = new PostedTransaction
            {
                AccountId = accountId,
                Date = new DateTime(2026, 3, 1),
                Description = "MICROSOFT*STORE",
                Amount = -20,
                CategoryId = retire.Id,
                Category = retire.Name
            };
            store.AddPostedTransaction(posted);
            store.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Description = "Microsoft 365",
                Amount = -10,
                CategoryId = retire.Id,
                Category = retire.Name,
                NextOccurrence = DateTime.Today.AddDays(7)
            });
            store.UpsertAssignment(Categorizer.Normalize(posted.Description), retire.Id);

            var budgets = new BudgetOrchestrator(store);
            budgets.AddRule(new ExpenseBudgetRule
            {
                AccountId = accountId,
                BudgetName = "Software",
                IncludedCategoryIds = [retire.Id],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -50,
                IsActive = true
            });

            orchestrator.MergeCategories(keep.Id, retire.Id);

            Assert.That(store.GetPostedTransaction(posted.Id)!.CategoryId, Is.EqualTo(keep.Id));
            Assert.That(store.GetPostedTransaction(posted.Id)!.Category, Is.EqualTo("Software"));
            Assert.That(store.GetAllRecurringSingleRules().Single().CategoryId, Is.EqualTo(keep.Id));
            Assert.That(store.GetAssignment(Categorizer.Normalize(posted.Description))!.CategoryId, Is.EqualTo(keep.Id));
            Assert.That(store.GetExpenseBudgetRules(accountId).Single().IncludedCategoryIds, Is.EquivalentTo(new[] { keep.Id }));
            Assert.That(store.GetCategory(retire.Id)!.IsActive, Is.False);
            Assert.That(store.GetCategory(child.Id)!.ParentCategoryId, Is.EqualTo(keep.Id));
            Assert.That(store.GetCategory(keep.Id)!.IsActive, Is.True);
            Assert.That(orchestrator.GetActiveCategories().Select(c => c.Id), Does.Not.Contain(retire.Id));
        }

        [Test]
        public void DeactivateCategory_HidesFromActiveListAndBlocksUncategorized()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new CategoryOrchestrator(store, store);
            var created = orchestrator.CreateCategory("Temp");

            orchestrator.DeactivateCategory(created.Id);

            Assert.That(store.GetCategory(created.Id)!.IsActive, Is.False);
            Assert.That(orchestrator.GetActiveCategories().Select(c => c.Id), Does.Not.Contain(created.Id));
            Assert.That(
                () => orchestrator.DeactivateCategory(DefaultExpenseCategories.UncategorizedId),
                Throws.InvalidOperationException);
        }

        [Test]
        public void UpdateCategory_RejectsCyclicParent()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new CategoryOrchestrator(store, store);
            var parent = orchestrator.CreateCategory("Utility Group");
            var child = orchestrator.CreateCategory("Electric Bill", parent.Id);

            parent.ParentCategoryId = child.Id;
            Assert.That(() => orchestrator.UpdateCategory(parent), Throws.InvalidOperationException);
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
