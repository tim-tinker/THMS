using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Budget;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Recurrence;
using THMS.Logic.Finance.Transfer;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class BaseOrchestratorTests
    {
        [Test]
        public void GetStartDate_SupportsYearLifetimeAndDefault()
        {
            var orchestrator = new TestableBaseOrchestrator();
            var end = new DateTime(2026, 6, 15);

            Assert.That(orchestrator.CallGetStartDate(end, "Year"), Is.EqualTo(end.AddYears(-1)));
            Assert.That(orchestrator.CallGetStartDate(end, "Lifetime"), Is.EqualTo(DateTime.MinValue));
            Assert.That(orchestrator.CallGetStartDate(end, "Month"), Is.EqualTo(end.AddMonths(-1)));
            Assert.That(orchestrator.CallGetStartDate(end, "whatever"), Is.EqualTo(end.AddMonths(-1)));
        }
    }

    [TestFixture]
    public class CategorizerTests
    {
        [Test]
        public void ApplyCategories_MatchesKnownMerchantsAndPayment()
        {
            var txs = new List<PostedTransaction>
            {
                new() { Description = "AMAZON marketplace", Amount = -12 },
                new() { Description = "Walmart Supercenter", Amount = -40 },
                new() { Description = "Card Payment", Amount = -100 },
                new() { Description = "Random cafe", Amount = -8 },
                new() { Description = "Payment received", Amount = 50 }
            };

            new Categorizer().ApplyCategories(txs);

            Assert.That(txs[0].Category, Is.EqualTo("Shopping"));
            Assert.That(txs[1].Category, Is.EqualTo("Groceries"));
            Assert.That(txs[2].Category, Is.EqualTo("Payment"));
            Assert.That(txs[3].Category, Is.EqualTo("Uncategorized"));
            Assert.That(txs[4].Category, Is.EqualTo("Uncategorized"));
        }
    }

    [TestFixture]
    public class RecurringDetectorTests
    {
        private static List<PostedTransaction> Series(string description, DateTime start, int count, int days, decimal amount)
        {
            var accountId = Guid.NewGuid();
            return Enumerable.Range(0, count)
                .Select(i => new PostedTransaction
                {
                    AccountId = accountId,
                    Description = description,
                    Date = start.AddDays(i * days),
                    Amount = amount
                })
                .ToList();
        }

        [TestCase(7, RecurrenceFrequency.Weekly)]
        [TestCase(14, RecurrenceFrequency.BiWeekly)]
        [TestCase(30, RecurrenceFrequency.Monthly)]
        [TestCase(90, RecurrenceFrequency.Quarterly)]
        [TestCase(365, RecurrenceFrequency.Yearly)]
        public void DetectRecurringSingles_ClassifiesFrequencies(int days, RecurrenceFrequency expected)
        {
            var historical = Series("Netflix", new DateTime(2024, 1, 1), 3, days, 15.99m);
            var detected = new RecurringDetector().DetectRecurringSingles(historical, []);
            Assert.That(detected, Has.Count.EqualTo(1));
            Assert.That(detected[0].Frequency, Is.EqualTo(expected));
            Assert.That(detected[0].IsActive, Is.True);
        }

        [Test]
        public void DetectRecurringSingles_SkipsShortGroupsIrregularVarianceAndDuplicates()
        {
            var detector = new RecurringDetector();
            Assert.That(detector.DetectRecurringSingles(Series("A", DateTime.Today, 2, 7, 10), []), Is.Empty);

            var irregular = new List<PostedTransaction>
            {
                new() { Description = "X", Date = new DateTime(2026, 1, 1), Amount = 10 },
                new() { Description = "X", Date = new DateTime(2026, 1, 10), Amount = 10 },
                new() { Description = "X", Date = new DateTime(2026, 2, 20), Amount = 10 }
            };
            Assert.That(detector.DetectRecurringSingles(irregular, []), Is.Empty);

            var varying = Series("Gym", new DateTime(2026, 1, 1), 3, 7, 10);
            varying[2].Amount = 20;
            Assert.That(detector.DetectRecurringSingles(varying, []), Is.Empty);

            var weekly = Series("Hulu", new DateTime(2026, 1, 1), 3, 7, 12);
            var existing = new List<RecurringSingleTransactionRule>
            {
                new()
                {
                    Description = "Hulu",
                    AccountId = weekly[0].AccountId,
                    Frequency = RecurrenceFrequency.Weekly
                }
            };
            Assert.That(detector.DetectRecurringSingles(weekly, existing), Is.Empty);
        }

        [Test]
        public void DetectRecurringTransfers_MirrorsSingleDetection()
        {
            var accountId = Guid.NewGuid();
            var historical = Enumerable.Range(0, 3)
                .Select(i => new PostedTransferTransaction
                {
                    AccountId = accountId,
                    Description = "Sweep",
                    Date = new DateTime(2026, 1, 1).AddDays(i * 7),
                    Amount = 50
                })
                .ToList();

            var detector = new RecurringDetector();
            var detected = detector.DetectRecurringTransfers(historical, []);
            Assert.That(detected, Has.Count.EqualTo(1));
            Assert.That(detected[0].Frequency, Is.EqualTo(RecurrenceFrequency.Weekly));

            Assert.That(detector.DetectRecurringTransfers(historical.Take(2), []), Is.Empty);
            Assert.That(
                detector.DetectRecurringTransfers(historical, [
                    new RecurringTransferRule
                    {
                        Description = "Sweep",
                        FromAccountId = accountId,
                        Frequency = RecurrenceFrequency.Weekly
                    }
                ]),
                Is.Empty);

            historical[2].Amount = 80;
            Assert.That(detector.DetectRecurringTransfers(historical, []), Is.Empty);
        }
    }

    [TestFixture]
    public class TransferDetectorTests
    {
        [Test]
        public void DetectTransfers_PairsOppositeAmountsOnSameDate()
        {
            var from = Guid.NewGuid();
            var to = Guid.NewGuid();
            var date = new DateTime(2026, 3, 3);
            var posted = new List<PostedTransaction>
            {
                new() { Id = Guid.NewGuid(), AccountId = from, Date = date, Amount = -40, Description = "Out" },
                new() { Id = Guid.NewGuid(), AccountId = to, Date = date, Amount = 40, Description = "In" },
                new() { Id = Guid.NewGuid(), AccountId = to, Date = date, Amount = 5, Description = "Noise" }
            };

            var detector = new TransferDetector();
            detector.DetectTransfers(posted);

            Assert.That(detector.Detected, Has.Count.EqualTo(2));
            Assert.That(detector.Matched, Has.Count.EqualTo(2));
            Assert.That(detector.Detected, Has.Some.Matches<PostedTransferTransaction>(t => t.Direction == TransferDirection.Incoming));
            Assert.That(detector.Detected, Has.Some.Matches<PostedTransferTransaction>(t => t.Direction == TransferDirection.Outgoing));
        }
    }

    [TestFixture]
    public class ForecastGeneratorTests
    {
        [Test]
        public void GenerateForecast_SkipsInactive_UsesFinalPayment_DoesNotMutateRules()
        {
            var accountId = Guid.NewGuid();
            var today = DateTime.Today;
            var rules = new List<RecurringSingleTransactionRule>
            {
                new() { IsActive = false, NextOccurrence = today, AccountId = accountId, Amount = 1, Frequency = RecurrenceFrequency.Weekly },
                new()
                {
                    IsActive = true,
                    AccountId = accountId,
                    Amount = 10,
                    Frequency = RecurrenceFrequency.Monthly,
                    NextOccurrence = today,
                    EndDate = today,
                    IsFinalPaymentDifferent = true,
                    FinalPaymentAmount = 3,
                    Description = "Loan",
                    Category = "Debt"
                },
                new()
                {
                    IsActive = true,
                    AccountId = accountId,
                    Amount = 8,
                    Frequency = RecurrenceFrequency.Weekly,
                    NextOccurrence = today,
                    EndDate = today,
                    IsFinalPaymentDifferent = true,
                    FinalPaymentAmount = null
                }
            };

            var originalNext = rules[1].NextOccurrence;
            var futures = new ForecastGenerator().GenerateForecast(
                accountId, today, today.AddMonths(3), rules, []);

            Assert.That(futures.Any(f => f.Amount == 3), Is.True);
            Assert.That(futures.Any(f => f.Amount == 8), Is.True);
            Assert.That(futures.All(f => f.Type == UnifiedTransactionView.ForecastType), Is.True);
            Assert.That(rules[1].NextOccurrence, Is.EqualTo(originalNext));
        }

        [Test]
        public void GenerateForecast_TransfersHonorEndDateAndInactive()
        {
            var today = DateTime.Today;
            var from = Guid.NewGuid();
            var to = Guid.NewGuid();
            var rules = new List<RecurringTransferRule>
            {
                new() { IsActive = false, NextOccurrence = today, Amount = 1, Frequency = RecurrenceFrequency.Weekly, FromAccountId = from },
                new()
                {
                    IsActive = true,
                    FromAccountId = from,
                    ToAccountId = to,
                    Amount = 25,
                    Frequency = RecurrenceFrequency.Monthly,
                    NextOccurrence = today,
                    EndDate = today.AddMonths(1),
                    Description = "Sweep"
                },
                new()
                {
                    IsActive = true,
                    FromAccountId = from,
                    ToAccountId = to,
                    Amount = 9,
                    Frequency = RecurrenceFrequency.Weekly,
                    NextOccurrence = today,
                    EndDate = today,
                    IsFinalPaymentDifferent = true,
                    FinalPaymentAmount = 2
                }
            };

            var futures = new ForecastGenerator().GenerateForecast(
                from, today, today.AddMonths(3), [], rules);
            Assert.That(futures.Any(f => f.Amount == 25), Is.True);
            Assert.That(futures.Any(f => f.Amount == 2), Is.True);
            Assert.That(futures.All(f => f.Type == UnifiedTransactionView.ForecastTransferType), Is.True);
        }

        [Test]
        public void GenerateForecast_ExpandsEachFrequencyWithinWindow()
        {
            var accountId = Guid.NewGuid();
            var start = new DateTime(2026, 1, 5);
            var rules = new List<RecurringSingleTransactionRule>
            {
                new() { IsActive = true, AccountId = accountId, Amount = 1, Frequency = RecurrenceFrequency.Weekly, NextOccurrence = start },
                new() { IsActive = true, AccountId = accountId, Amount = 2, Frequency = RecurrenceFrequency.BiWeekly, NextOccurrence = start },
                new() { IsActive = true, AccountId = accountId, Amount = 3, Frequency = RecurrenceFrequency.Monthly, NextOccurrence = start },
                new() { IsActive = true, AccountId = accountId, Amount = 4, Frequency = RecurrenceFrequency.Quarterly, NextOccurrence = start },
                new() { IsActive = true, AccountId = accountId, Amount = 5, Frequency = RecurrenceFrequency.Yearly, NextOccurrence = start }
            };

            var futures = new ForecastGenerator().GenerateForecast(
                accountId, start, start.AddYears(1), rules, []);

            Assert.That(futures.Count(f => f.Amount == 1), Is.EqualTo(53));
            Assert.That(futures.Count(f => f.Amount == 2), Is.EqualTo(27));
            Assert.That(futures.Count(f => f.Amount == 3), Is.EqualTo(13));
            Assert.That(futures.Count(f => f.Amount == 4), Is.EqualTo(5));
            Assert.That(futures.Count(f => f.Amount == 5), Is.EqualTo(2));
        }

        [Test]
        public void GenerateExpenseBudgetForecast_CreatesMonthlyViews_WithoutAdvancingNextOccurrence()
        {
            var accountId = Guid.NewGuid();
            var start = DateTime.Today.AddMonths(1);
            var originalNext = start;
            var rule = new UtilityBudgetRule
            {
                AccountId = accountId,
                CurrentAverage = -42.5m,
                NextOccurrence = start
            };

            var futures = new ExpenseBudgetForecastGenerator().Generate(rule, DateTime.Today, DateTime.Today.AddMonths(3));

            Assert.That(futures, Is.Not.Empty);
            Assert.That(futures.All(f => f.AccountId == accountId), Is.True);
            Assert.That(futures.All(f => f.Amount == -42.5m), Is.True);
            Assert.That(futures.All(f => f.Category == "Utilities"), Is.True);
            Assert.That(futures.All(f => f.Description == "Utilities Budget"), Is.True);
            Assert.That(futures.All(f => f.Type == UnifiedTransactionView.ForecastBudgetType), Is.True);
            Assert.That(futures.Select(f => f.Date), Is.Ordered);
            Assert.That(rule.NextOccurrence, Is.EqualTo(originalNext));
        }

        [Test]
        public void GenerateExpenseBudgetForecast_WhenNextIsPastHorizon_ReturnsEmpty()
        {
            var originalNext = DateTime.Today.AddMonths(4);
            var rule = new UtilityBudgetRule
            {
                AccountId = Guid.NewGuid(),
                CurrentAverage = 10,
                NextOccurrence = originalNext
            };

            var futures = new ExpenseBudgetForecastGenerator().Generate(rule, DateTime.Today, DateTime.Today.AddMonths(3));
            Assert.That(futures, Is.Empty);
            Assert.That(rule.NextOccurrence, Is.EqualTo(originalNext));
        }
    }

    [TestFixture]
    public class ExpenseBudgetDetectorTests
    {
        [Test]
        public void Detect_NoMatchingPostings_CreatesRuleWithZeroAverage()
        {
            var accountId = Guid.NewGuid();
            var posted = new List<PostedTransaction>
            {
                new() { AccountId = accountId, Date = new DateTime(2026, 1, 5), Amount = -12, Category = "Groceries" }
            };

            var rule = new ExpenseBudgetDetector().Detect(accountId, posted, null, ["Utilities"]);

            Assert.That(rule, Is.TypeOf<UtilityBudgetRule>());
            Assert.That(rule.AccountId, Is.EqualTo(accountId));
            Assert.That(rule.CurrentAverage, Is.EqualTo(0));
            Assert.That(rule.Category, Is.EqualTo("Utilities"));
            Assert.That(rule.NextOccurrence.Date, Is.EqualTo(DateTime.Today.AddMonths(1).Date));
            Assert.That(rule.Id, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void Detect_AveragesMonthlyTotalsForIncludedCategories()
        {
            var accountId = Guid.NewGuid();
            var posted = new List<PostedTransaction>
            {
                new() { Date = new DateTime(2026, 1, 8), Amount = -30, Category = "Electric" },
                new() { Date = new DateTime(2026, 1, 22), Amount = -10, Category = "Water" },
                new() { Date = new DateTime(2026, 2, 3), Amount = -20, Category = "Gas" },
                new() { Date = new DateTime(2026, 2, 4), Amount = -5, Category = "Shopping" }
            };

            var existing = new UtilityBudgetRule
            {
                AccountId = accountId,
                SmoothingMode = ExpenseSmoothingMode.SimpleAverage,
                NextOccurrence = DateTime.Today.AddMonths(1)
            };

            var rule = new ExpenseBudgetDetector().Detect(
                accountId,
                posted,
                existing,
                existing.IncludedCategories);

            Assert.That(rule.CurrentAverage, Is.EqualTo(-30m));
        }

        [Test]
        public void Detect_UpdatesExistingRuleInPlace()
        {
            var accountId = Guid.NewGuid();
            var existing = new UtilityBudgetRule
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                CurrentAverage = 1,
                NextOccurrence = new DateTime(2026, 6, 1)
            };

            var posted = new List<PostedTransaction>
            {
                new() { Date = new DateTime(2026, 3, 1), Amount = -18, Category = "Electric" }
            };

            var updated = new ExpenseBudgetDetector().Detect(
                accountId,
                posted,
                existing,
                existing.IncludedCategories);

            Assert.That(updated, Is.SameAs(existing));
            Assert.That(updated.Id, Is.EqualTo(existing.Id));
            Assert.That(updated.CurrentAverage, Is.EqualTo(-18m));
            Assert.That(updated.NextOccurrence, Is.EqualTo(new DateTime(2026, 6, 1)));
        }
    }

    [TestFixture]
    public class FutureReconcilerTests
    {
        [Test]
        public void ReconcileSingles_UpdatesRuleNextAndLastOccurrence()
        {
            var accountId = Guid.NewGuid();
            var postedId = Guid.NewGuid();
            var next = new DateTime(2026, 4, 4);
            var rule = new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Amount = -20,
                NextOccurrence = next,
                Frequency = RecurrenceFrequency.Monthly,
                Description = "Netflix",
                Category = "Entertainment",
                IsActive = true
            };

            var reconciler = new FutureReconciler();
            reconciler.ReconcileSingles(
                [
                    new PostedTransaction
                    {
                        Id = postedId,
                        AccountId = accountId,
                        Amount = -20,
                        Date = next,
                        Description = "Netflix Premium",
                        Category = "Streaming"
                    }
                ],
                [rule]);

            Assert.That(rule.LastOccurrence, Is.EqualTo(next));
            Assert.That(rule.NextOccurrence, Is.EqualTo(next.AddMonths(1)));
            Assert.That(rule.Amount, Is.EqualTo(-20));
            Assert.That(rule.Description, Is.EqualTo("Netflix Premium"));
            Assert.That(rule.Category, Is.EqualTo("Streaming"));
            Assert.That(reconciler.MatchedSingleRules, Has.Count.EqualTo(1));
        }

        [Test]
        public void ReconcileTransfers_UpdatesRuleFromMatchingPostedTransfer()
        {
            var from = Guid.NewGuid();
            var to = Guid.NewGuid();
            var date = new DateTime(2026, 5, 5);
            var rule = new RecurringTransferRule
            {
                FromAccountId = from,
                ToAccountId = to,
                Amount = 30,
                NextOccurrence = date,
                Frequency = RecurrenceFrequency.Monthly,
                IsActive = true
            };

            var reconciler = new FutureReconciler();
            reconciler.ReconcileTransfers(
                [
                    new PostedTransferTransaction { AccountId = from, Amount = 30, Date = date, Id = Guid.NewGuid() }
                ],
                [rule]);

            Assert.That(rule.LastOccurrence, Is.EqualTo(date));
            Assert.That(rule.NextOccurrence, Is.EqualTo(date.AddMonths(1)));
            Assert.That(reconciler.MatchedTransferRules, Has.Count.EqualTo(1));
        }
    }

    [TestFixture]
    public class AccountOrchestratorTests
    {
        private static BankAccount Bank(string name = "Checking") => new()
        {
            Name = name,
            Institution = "Bank",
            AccountNumber = "12-34 56",
            Type = AccountType.Checking,
            PostedBalance = 10
        };

        [Test]
        public void Save_ValidatesAndNormalizesAccountNumber()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountOrchestrator(store);

            Assert.That(() => orchestrator.Save(new BankAccount { Name = " ", Institution = "A", AccountNumber = "1" }), Throws.ArgumentException);
            Assert.That(() => orchestrator.Save(new BankAccount { Name = "A", Institution = " ", AccountNumber = "1" }), Throws.ArgumentException);
            Assert.That(() => orchestrator.Save(new BankAccount { Name = "A", Institution = "B", AccountNumber = " " }), Throws.ArgumentException);
            Assert.That(
                () => orchestrator.Save(new CreditAccount
                {
                    Name = "Card",
                    Institution = "Bank",
                    AccountNumber = "1",
                    Type = AccountType.CreditCard,
                    CreditLimit = 0
                }),
                Throws.ArgumentException);

            orchestrator.Save(new BankAccount
            {
                Name = "Looks like credit",
                Institution = "Bank",
                AccountNumber = "555",
                Type = AccountType.CreditCard
            });

            var loc = new CreditAccount
            {
                Name = "LOC",
                Institution = "Bank",
                AccountNumber = "99-00",
                Type = AccountType.LineOfCredit,
                CreditLimit = 500
            };
            orchestrator.Save(loc);
            Assert.That(store.GetAccount(loc.Name)!.AccountNumber, Is.EqualTo("9900"));
        }

        [Test]
        public void UpdatePostedBalance_CoversAllAccountTypes()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountOrchestrator(store);
            var asOf = new DateTime(2026, 1, 2);

            var bank = Bank();
            store.UpsertAccount(bank);
            orchestrator.UpdatePostedBalance(bank.Name, 99, asOf);
            Assert.That(((BankAccount)store.GetAccount(bank.Name)!).PostedBalance, Is.EqualTo(99));

            var credit = new CreditAccount { Name = "C", Institution = "I", AccountNumber = "1", Type = AccountType.CreditCard, CreditLimit = 1 };
            store.UpsertAccount(credit);
            orchestrator.UpdatePostedBalance(credit.Name, 40, asOf);
            Assert.That(((CreditAccount)store.GetAccount(credit.Name)!).PostedBalance, Is.EqualTo(40));

            var invest = new InvestmentAccount { Name = "I", Institution = "I", AccountNumber = "1" };
            store.UpsertAccount(invest);
            orchestrator.UpdatePostedBalance(invest.Name, 70, asOf);
            Assert.That(((InvestmentAccount)store.GetAccount(invest.Name)!).CashBalance, Is.EqualTo(70));

            var loan = new LoanAccount { Name = "L", Institution = "I", AccountNumber = "1" };
            store.UpsertAccount(loan);
            orchestrator.UpdatePostedBalance(loan.Name, 800, asOf);
            Assert.That(((LoanAccount)store.GetAccount(loan.Name)!).Principal, Is.EqualTo(800));

            var mortgage = new MortgageAccount { Name = "M", Institution = "I", AccountNumber = "1" };
            store.UpsertAccount(mortgage);
            orchestrator.UpdatePostedBalance(mortgage.Name, 900, asOf);
            Assert.That(((MortgageAccount)store.GetAccount(mortgage.Name)!).Principal, Is.EqualTo(900));

            var internalAcc = new InternalAccount { Name = "Int", Institution = "I", AccountNumber = "1" };
            store.UpsertAccount(internalAcc);
            orchestrator.UpdatePostedBalance(internalAcc.Name, 1, asOf);
            Assert.That(store.GetAccount(internalAcc.Name)!.BalanceAsOf, Is.EqualTo(asOf));

            var unknown = new UnknownAccount { Name = "U", Institution = "I", AccountNumber = "1" };
            store.UpsertAccount(unknown);
            Assert.That(() => orchestrator.UpdatePostedBalance(unknown.Name, 1, asOf), Throws.InvalidOperationException);

            Assert.That(() => orchestrator.UpdatePostedBalance("missing-account", 1, asOf), Throws.InvalidOperationException);
        }

        [Test]
        public void DeleteAndGetHelpers()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountOrchestrator(store);
            var bank = Bank();
            orchestrator.Save(bank);

            Assert.That(orchestrator.GetAccount(bank.Name), Is.Not.Null);
            Assert.That(orchestrator.GetAllAccounts().Count(), Is.EqualTo(1));
            orchestrator.Delete(bank.Id);
            Assert.That(orchestrator.GetAccount(bank.Name), Is.Null);
        }

        [Test]
        public void AdjustStartingBalanceForPostedDelta_ShiftsBankAndCreditStartingBalance()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountOrchestrator(store);

            var bank = Bank();
            bank.StartingBalance = 40;
            bank.PostedBalance = 50;
            store.UpsertAccount(bank);

            orchestrator.AdjustStartingBalanceForPostedDelta(bank.Id, 15);
            var updatedBank = (BankAccount)store.GetAccount(bank.Name)!;
            Assert.That(updatedBank.StartingBalance, Is.EqualTo(55));
            Assert.That(updatedBank.PostedBalance, Is.EqualTo(65));

            var credit = new CreditAccount
            {
                Name = "Card",
                Institution = "Bank",
                AccountNumber = "1",
                Type = AccountType.CreditCard,
                CreditLimit = 100,
                StartingBalance = -20,
                PostedBalance = -80
            };
            store.UpsertAccount(credit);
            orchestrator.AdjustStartingBalanceForPostedDelta(credit.Id, -10);
            var updatedCredit = (CreditAccount)store.GetAccount(credit.Name)!;
            Assert.That(updatedCredit.StartingBalance, Is.EqualTo(-30));
            Assert.That(updatedCredit.PostedBalance, Is.EqualTo(-90));
        }
    }

    [TestFixture]
    public class TransactionOrchestratorTests
    {
        [Test]
        public void GenerateForecastAndReconcileRules()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new TransactionOrchestrator(store);
            var accountId = Guid.NewGuid();
            var other = Guid.NewGuid();
            var start = DateTime.Today;

            store.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Amount = 10,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = start,
                IsActive = true
            });
            store.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Amount = 5,
                Frequency = RecurrenceFrequency.Weekly,
                NextOccurrence = start,
                EndDate = start,
                IsFinalPaymentDifferent = true,
                FinalPaymentAmount = 2,
                IsActive = true
            });
            store.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Amount = 1,
                IsActive = false,
                NextOccurrence = start,
                Frequency = RecurrenceFrequency.Weekly
            });
            store.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = accountId,
                ToAccountId = accountId,
                Amount = 20,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = start,
                IsActive = true
            });
            store.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = accountId,
                ToAccountId = other,
                Amount = 4,
                Frequency = RecurrenceFrequency.Weekly,
                NextOccurrence = start,
                EndDate = start,
                IsFinalPaymentDifferent = true,
                FinalPaymentAmount = null,
                IsActive = true
            });
            store.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = accountId,
                ToAccountId = other,
                IsActive = false,
                NextOccurrence = start,
                Frequency = RecurrenceFrequency.Weekly
            });

            var forecast = orchestrator.GenerateForecast(accountId, start, start.AddMonths(1));
            Assert.That(forecast.Any(f => f.Type == UnifiedTransactionView.ForecastType), Is.True);
            Assert.That(forecast.Any(f => f.Type == UnifiedTransactionView.ForecastTransferType), Is.True);
            Assert.That(store.GetFutureSingleTransactions(accountId), Is.Empty);
            Assert.That(store.GetFutureTransferTransactions(accountId), Is.Empty);

            var monthly = store.GetRecurringSingleRules(accountId).First(r => r.Amount == 10);
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = accountId,
                Amount = monthly.Amount,
                Date = monthly.NextOccurrence
            });
            orchestrator.ReconcileRules(accountId);
            Assert.That(store.GetRecurringSingleRules(accountId).First(r => r.Amount == 10).LastOccurrence, Is.EqualTo(start));
            Assert.That(store.GetRecurringSingleRules(accountId).First(r => r.Amount == 10).NextOccurrence, Is.EqualTo(start.AddMonths(1)));

            var transferRule = store.GetRecurringTransferRules(accountId).First(r => r.Amount == 20);
            store.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                AccountId = transferRule.FromAccountId,
                Amount = transferRule.Amount,
                Date = transferRule.NextOccurrence
            });
            orchestrator.ReconcileRules(accountId);
            Assert.That(store.GetRecurringTransferRules(accountId).First(r => r.Amount == 20).LastOccurrence, Is.EqualTo(start));
        }
    }
}
