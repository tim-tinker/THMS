using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Finance.Budget;
using THMS.Logic.Finance.Forecast;
using THMS.Logic.Finance.Model;
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

        [Test]
        public void GetHistoryRange_UsesTodayAndEndOfDay()
        {
            var (start, end) = BaseOrchestrator.GetHistoryRange("Month");
            Assert.That(start, Is.EqualTo(DateTime.Today.AddMonths(-1)));
            Assert.That(end, Is.EqualTo(DateTime.Today.AddDays(1).AddTicks(-1)));

            var lifetime = BaseOrchestrator.GetHistoryRange("Lifetime");
            Assert.That(lifetime.Start, Is.EqualTo(DateTime.MinValue));
            Assert.That(lifetime.End, Is.EqualTo(end));
        }
    }

    [TestFixture]
    public class TransactionHistoryRangeTests
    {
        [Test]
        public void GetTransactionsForAccount_FiltersPostedToDateRange()
        {
            var store = new InMemoryTransactionDataStore();
            var accountId = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = new DateTime(2026, 1, 15),
                Amount = -10,
                Description = "old"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = new DateTime(2026, 9, 2),
                Amount = -5,
                Description = "recent"
            });

            var orchestrator = new TransactionOrchestrator(store);
            var month = orchestrator.GetTransactionsForAccount(
                accountId,
                new DateTime(2026, 8, 11),
                new DateTime(2026, 9, 11));

            Assert.That(month.Posted.Select(t => t.Description), Is.EqualTo(new[] { "recent" }));
        }

        [Test]
        public void SumPostedAmountsBefore_ExcludesLaterTransactions()
        {
            var store = new InMemoryTransactionDataStore();
            var accountId = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = new DateTime(2026, 1, 15),
                Amount = -10,
                Description = "old"
            });
            store.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = new DateTime(2026, 1, 20),
                Amount = 4,
                Description = "transfer"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Date = new DateTime(2026, 9, 2),
                Amount = -5,
                Description = "recent"
            });

            var orchestrator = new TransactionOrchestrator(store);

            Assert.That(orchestrator.SumPostedAmountsBefore(accountId, new DateTime(2026, 8, 11)), Is.EqualTo(-6m));
            Assert.That(orchestrator.SumPostedAmountsBefore(accountId, DateTime.MinValue), Is.EqualTo(0m));
        }
    }

    [TestFixture]
    public class PostedBalanceCalculatorTests
    {
        [Test]
        public void TryResolveAnchor_UsesLatestMatchingStatementAndIgnoresOlderRegisterActivity()
        {
            var bank = new BankAccount { StartingBalance = 0, PostedBalance = 0 };
            var older = new BankStatement
            {
                Id = Guid.NewGuid(),
                AccountId = bank.Id,
                StatementDate = new DateTime(2026, 1, 31),
                StatementBalance = 800
            };
            var latest = new BankStatement
            {
                Id = Guid.NewGuid(),
                AccountId = bank.Id,
                StatementDate = new DateTime(2026, 8, 31),
                StatementBalance = 1250
            };

            Assert.That(PostedBalanceCalculator.TryResolveAnchor(bank, [older, latest], out var anchor), Is.True);
            Assert.That(anchor.LedgerBalance, Is.EqualTo(1250m));
            Assert.That(anchor.AsOf, Is.EqualTo(latest.StatementDate));
            Assert.That(PostedBalanceCalculator.ComputeFromAnchor(anchor, 40m), Is.EqualTo(1290m));
        }

        [Test]
        public void TryGetStatementAnchor_ConvertsCreditStatementBalanceToLedgerSpace()
        {
            var card = new CreditAccount { CreditLimit = 5000 };
            var statement = new CreditCardStatement
            {
                StatementDate = new DateTime(2026, 8, 15),
                StatementBalance = 400
            };

            Assert.That(PostedBalanceCalculator.TryGetStatementAnchor(card, statement, out var anchor), Is.True);
            Assert.That(anchor.LedgerBalance, Is.EqualTo(-400m));
            Assert.That(PostedBalanceCalculator.ComputeFromAnchor(anchor, -25m), Is.EqualTo(-425m));
        }

        [Test]
        public void TryResolveAnchor_SkipsStatementsThatDoNotMatchAccountKind()
        {
            var bank = new BankAccount();
            var utility = new UtilityStatement { StatementDate = DateTime.Today, AmountDue = 90 };

            Assert.That(PostedBalanceCalculator.TryResolveAnchor(bank, [utility], out _), Is.False);
        }

        [Test]
        public void HasUsablePostedBalance_RequiresMatchingStatement()
        {
            var bank = new BankAccount { PostedBalance = 2500 };
            var statement = new BankStatement
            {
                AccountId = bank.Id,
                StatementDate = new DateTime(2026, 8, 31),
                StatementBalance = 1000
            };

            Assert.That(PostedBalanceCalculator.HasUsablePostedBalance(bank, []), Is.False);
            Assert.That(PostedBalanceCalculator.HasUsablePostedBalance(bank, [statement]), Is.True);
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

            Assert.That(txs[0].Category, Is.EqualTo("Restaurants"));
            Assert.That(txs[0].CategoryId, Is.EqualTo(DefaultExpenseCategories.RestaurantsId));
            Assert.That(txs[1].Category, Is.EqualTo("Groceries"));
            Assert.That(txs[2].Category, Is.EqualTo("Payment"));
            Assert.That(txs[3].Category, Is.EqualTo("Uncategorized"));
            Assert.That(txs[4].Category, Is.EqualTo("Uncategorized"));
        }

        [Test]
        public void Suggest_UsesLearnedAssignmentBeforePatterns()
        {
            var store = new InMemoryTransactionDataStore();
            var categorizer = new Categorizer(store);
            var first = new PostedTransaction { Description = "LOCAL POWER CO", Amount = -40 };
            categorizer.Learn(first.Description, DefaultExpenseCategories.ElectricId);

            var suggestion = categorizer.Suggest(new PostedTransaction
            {
                Description = "LOCAL POWER CO",
                Amount = -22
            });

            Assert.That(suggestion.CategoryId, Is.EqualTo(DefaultExpenseCategories.ElectricId));
            Assert.That(suggestion.Confidence, Is.GreaterThan(0.9));
        }

        [Test]
        public void ApplySuggestion_DoesNotOverwriteExistingCategoryId()
        {
            var store = new InMemoryTransactionDataStore();
            var categorizer = new Categorizer(store);
            var tx = new PostedTransaction
            {
                Description = "AMAZON marketplace",
                Amount = -12,
                CategoryId = DefaultExpenseCategories.GroceriesId,
                Category = "Groceries"
            };

            categorizer.ApplySuggestion(tx);

            Assert.That(tx.CategoryId, Is.EqualTo(DefaultExpenseCategories.GroceriesId));
        }

        [Test]
        public void Suggest_SkipsInactiveLearnedCategory()
        {
            var store = new InMemoryTransactionDataStore();
            var categorizer = new Categorizer(store);
            store.AddCategory(new ExpenseCategory { Name = "Old Merchant", IsActive = false });
            var inactive = store.GetAllCategories(includeInactive: true).Single(c => c.Name == "Old Merchant");
            categorizer.Learn("LOCAL POWER CO", inactive.Id);

            var suggestion = categorizer.Suggest(new PostedTransaction
            {
                Description = "LOCAL POWER CO",
                Amount = -22
            });

            Assert.That(suggestion.CategoryId, Is.Not.EqualTo(inactive.Id));
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
            Assert.That(detected[0].IsUserCreated, Is.False);
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

        [Test]
        public void DetectRecurringSingles_UpdatesAutoRulesButLeavesUserCreatedAlone()
        {
            var detector = new RecurringDetector();
            var weekly = Series("Hulu", new DateTime(2026, 1, 1), 3, 7, 12);
            var accountId = weekly[0].AccountId;

            var userRule = new RecurringSingleTransactionRule
            {
                Description = "Hulu",
                AccountId = accountId,
                Frequency = RecurrenceFrequency.Weekly,
                Amount = 99,
                IsUserCreated = true
            };
            Assert.That(detector.DetectRecurringSingles(weekly, [userRule]), Is.Empty);
            Assert.That(userRule.Amount, Is.EqualTo(99));

            var autoRule = new RecurringSingleTransactionRule
            {
                Description = "Hulu",
                AccountId = accountId,
                Frequency = RecurrenceFrequency.Weekly,
                Amount = 1,
                Category = "Old",
                IsUserCreated = false
            };
            Assert.That(detector.DetectRecurringSingles(weekly, [autoRule]), Is.Empty);
            Assert.That(autoRule.Amount, Is.EqualTo(12));
        }

        [Test]
        public void DetectRecurringSingles_IsIdempotentForNormalizedPatternAndDistinctDates()
        {
            var accountId = Guid.NewGuid();
            var historical = Enumerable.Range(1, 5)
                .Select(month => new PostedTransaction
                {
                    AccountId = accountId,
                    Description = "LESLIES POOLMART",
                    Date = new DateTime(2026, month, 20),
                    Amount = -45.67m
                })
                .ToList();
            historical.Add(new PostedTransaction
            {
                AccountId = accountId,
                Description = "LESLIES POOLMART",
                Date = new DateTime(2026, 5, 20).AddHours(3),
                Amount = -45.67m
            });

            var detector = new RecurringDetector();
            var first = detector.DetectRecurringSingles(historical, []);
            Assert.That(first, Has.Count.EqualTo(1));
            Assert.That(first[0].Frequency, Is.EqualTo(RecurrenceFrequency.Monthly));
            Assert.That(first[0].NextOccurrence.Date, Is.EqualTo(new DateTime(2026, 6, 20)));

            var existing = new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Description = "  leslies   poolmart ",
                Amount = -45.67m,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = new DateTime(2026, 1, 20)
            };
            Assert.That(detector.DetectRecurringSingles(historical, [existing]), Is.Empty);
            Assert.That(existing.NextOccurrence.Date, Is.EqualTo(new DateTime(2026, 6, 20)));
            Assert.That(existing.LastOccurrence, Is.EqualTo(new DateTime(2026, 5, 20).AddHours(3)));
        }

        [Test]
        public void DuplicateAutoRuleIds_KeepsOneAutoRulePerPattern()
        {
            var accountId = Guid.NewGuid();
            var rules = Enumerable.Range(0, 8)
                .Select(_ => new RecurringSingleTransactionRule
                {
                    AccountId = accountId,
                    Description = "LESLIES POOLMART",
                    Amount = -45.67m,
                    Frequency = RecurrenceFrequency.Monthly,
                    NextOccurrence = new DateTime(2026, 6, 20),
                    IsUserCreated = false
                })
                .ToList();

            var extras = RecurringDetector.DuplicateAutoRuleIds(rules).ToList();
            Assert.That(extras, Has.Count.EqualTo(7));
            Assert.That(rules.Select(r => r.Id).Except(extras).Count(), Is.EqualTo(1));
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
        public void GenerateForecast_DoesNotEmitBudgetTransactions()
        {
            var accountId = Guid.NewGuid();
            var start = DateTime.Today;
            var futures = new ForecastGenerator().GenerateForecast(
                accountId,
                start,
                start.AddMonths(3),
                [],
                []);

            Assert.That(futures.Any(f => f.Type == UnifiedTransactionView.ForecastBudgetType), Is.False);
        }
    }

    [TestFixture]
    public class ExpenseBudgetDetectorTests
    {
        [Test]
        public void ComputeRecommendedAmount_NoMatchingPostings_ReturnsZero()
        {
            var posted = new List<PostedTransaction>
            {
                new() { Date = new DateTime(2026, 1, 5), Amount = -12, Category = "Groceries" }
            };

            var amount = new ExpenseBudgetDetector().ComputeRecommendedAmount(
                [],
                BudgetFrequency.Monthly,
                posted,
                [DefaultExpenseCategories.UtilityId],
                DefaultExpenseCategories.All);
            Assert.That(amount, Is.EqualTo(0));
        }

        [Test]
        public void ComputeRecommendedAmount_UsesPositivePeriodActuals()
        {
            var posted = new List<PostedTransaction>
            {
                new() { Date = new DateTime(2026, 1, 8), Amount = -30, Category = "Electric", CategoryId = DefaultExpenseCategories.ElectricId },
                new() { Date = new DateTime(2026, 1, 22), Amount = -10, Category = "Water", CategoryId = DefaultExpenseCategories.WaterId },
                new() { Date = new DateTime(2026, 2, 3), Amount = -20, Category = "Gas", CategoryId = DefaultExpenseCategories.GasId },
                new() { Date = new DateTime(2026, 2, 4), Amount = -5, Category = "Shopping", CategoryId = DefaultExpenseCategories.RestaurantsId }
            };

            var amount = new ExpenseBudgetDetector().ComputeRecommendedAmount(
                [],
                BudgetFrequency.Monthly,
                posted,
                [DefaultExpenseCategories.UtilityId],
                DefaultExpenseCategories.All);

            Assert.That(amount, Is.GreaterThan(0));
            Assert.That(amount, Is.LessThanOrEqualTo(40m));
        }

        [Test]
        public void ComputeActualExpenses_NetsReimbursementsAndStaysNonNegative()
        {
            var posted = new List<PostedTransaction>
            {
                new() { Date = new DateTime(2026, 3, 1), Amount = -40, CategoryId = DefaultExpenseCategories.ElectricId },
                new() { Date = new DateTime(2026, 3, 10), Amount = 15, CategoryId = DefaultExpenseCategories.ElectricId },
                new() { Date = new DateTime(2026, 4, 1), Amount = -40, CategoryId = DefaultExpenseCategories.ElectricId }
            };

            var actual = new ExpenseBudgetDetector().ComputeActualExpenses(
                posted,
                [DefaultExpenseCategories.ElectricId],
                new DateTime(2026, 3, 1),
                new DateTime(2026, 3, 31));

            Assert.That(actual, Is.EqualTo(25m));

            var refundOnly = new ExpenseBudgetDetector().ComputeActualExpenses(
                [new PostedTransaction { Date = new DateTime(2026, 3, 10), Amount = 15, CategoryId = DefaultExpenseCategories.ElectricId }],
                [DefaultExpenseCategories.ElectricId],
                new DateTime(2026, 3, 1),
                new DateTime(2026, 3, 31));
            Assert.That(refundOnly, Is.EqualTo(0m));
        }

        [Test]
        public void ComputeRecommendedAmount_UsesHistoryActualsNotSignedLedger()
        {
            var history = new List<ExpenseBudgetHistory>
            {
                new() { PeriodStart = new DateTime(2026, 1, 1), PeriodEnd = new DateTime(2026, 1, 31), ActualExpenses = 40 },
                new() { PeriodStart = new DateTime(2026, 2, 1), PeriodEnd = new DateTime(2026, 2, 28), ActualExpenses = 20 }
            };

            var recommended = new ExpenseBudgetDetector().ComputeRecommendedAmount(
                history,
                BudgetFrequency.Monthly);

            Assert.That(recommended, Is.GreaterThan(0));
            Assert.That(recommended, Is.Not.EqualTo(40m + 20m));
        }
    }

    [TestFixture]
    public class BudgetPeriodCalculatorTests
    {
        [Test]
        public void PeriodContaining_MonthlyUsesCalendarMonth()
        {
            var period = BudgetPeriodCalculator.PeriodContaining(new DateTime(2026, 9, 7), BudgetFrequency.Monthly);
            Assert.That(period.Start, Is.EqualTo(new DateTime(2026, 9, 1)));
            Assert.That(period.End, Is.EqualTo(new DateTime(2026, 9, 30)));
        }

        [Test]
        public void PeriodContaining_WeeklyIsMondayThroughSunday()
        {
            var period = BudgetPeriodCalculator.PeriodContaining(new DateTime(2026, 9, 9), BudgetFrequency.Weekly);
            Assert.That(period.Start, Is.EqualTo(new DateTime(2026, 9, 7)));
            Assert.That(period.End, Is.EqualTo(new DateTime(2026, 9, 13)));
        }

        [Test]
        public void PeriodContaining_QuarterlyAndAnnualUseCalendarBoundaries()
        {
            var quarter = BudgetPeriodCalculator.PeriodContaining(new DateTime(2026, 9, 7), BudgetFrequency.Quarterly);
            Assert.That(quarter.Start, Is.EqualTo(new DateTime(2026, 7, 1)));
            Assert.That(quarter.End, Is.EqualTo(new DateTime(2026, 9, 30)));

            var year = BudgetPeriodCalculator.PeriodContaining(new DateTime(2026, 9, 7), BudgetFrequency.Annual);
            Assert.That(year.Start, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(year.End, Is.EqualTo(new DateTime(2026, 12, 31)));
        }

        [Test]
        public void NextPeriod_MonthlyStartsOnFirstOfFollowingMonth()
        {
            var next = BudgetPeriodCalculator.NextPeriod(new DateTime(2026, 9, 30), BudgetFrequency.Monthly);
            Assert.That(next.Start, Is.EqualTo(new DateTime(2026, 10, 1)));
            Assert.That(next.End, Is.EqualTo(new DateTime(2026, 10, 31)));
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
        public void Save_UntrackedAccount_KeepsAlphanumericAccountNumber()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountOrchestrator(store);
            orchestrator.Save(new UntrackedAccount
            {
                Name = "Electric",
                Institution = "Duke",
                AccountNumber = "ABC-123",
                Type = AccountType.Utility
            });

            Assert.That(store.GetAccount("Electric")!.AccountNumber, Is.EqualTo("ABC-123"));
            Assert.That(store.GetAccount("Electric"), Is.TypeOf<UntrackedAccount>());
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

            var utility = new UntrackedAccount
            {
                Name = "Electric",
                Institution = "I",
                AccountNumber = "1",
                Type = AccountType.Utility
            };
            store.UpsertAccount(utility);
            orchestrator.UpdatePostedBalance(utility.Name, 1, asOf);
            Assert.That(store.GetAccount(utility.Name)!.BalanceAsOf, Is.EqualTo(asOf));

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

        [Test]
        public void CreditDisplayBalance_InvertsLedgerSoPositiveIsAmountOwed()
        {
            var credit = new CreditAccount { PostedBalance = -400, StartingBalance = -50, CreditLimit = 1000 };
            Assert.That(PostedBalanceCalculator.ToDisplayBalance(credit, credit.PostedBalance), Is.EqualTo(400m));
            Assert.That(PostedBalanceCalculator.ToLedgerBalance(credit, 400m), Is.EqualTo(-400m));
            Assert.That(PostedBalanceCalculator.ToLedgerPostedDelta(credit, 25m), Is.EqualTo(-25m));

            var bank = new BankAccount { PostedBalance = 400 };
            Assert.That(PostedBalanceCalculator.ToDisplayBalance(bank, 400m), Is.EqualTo(400m));
            Assert.That(PostedBalanceCalculator.ToLedgerPostedDelta(bank, 25m), Is.EqualTo(25m));
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

    [TestFixture]
    public class RecurringRuleOrchestratorTests
    {
        [Test]
        public void CrudAndNextPaymentDate_IncludeUserCreatedRules()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new RecurringRuleOrchestrator(store);
            var accountId = Guid.NewGuid();
            var other = Guid.NewGuid();

            var single = new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Description = "Rent",
                Amount = -1200,
                Category = "Housing",
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = new DateTime(2026, 10, 1),
                IsActive = true
            };
            orchestrator.AddSingleRule(single);
            Assert.That(single.IsUserCreated, Is.True);

            var transfer = new RecurringTransferRule
            {
                FromAccountId = accountId,
                ToAccountId = other,
                Description = "Sweep",
                Amount = 200,
                Frequency = RecurrenceFrequency.Weekly,
                NextOccurrence = new DateTime(2026, 9, 8),
                IsActive = true
            };
            orchestrator.AddTransferRule(transfer);
            Assert.That(transfer.IsUserCreated, Is.True);

            Assert.That(orchestrator.GetSingleRules(accountId), Has.Count.EqualTo(1));
            Assert.That(orchestrator.GetTransferRules(accountId), Has.Count.EqualTo(1));
            Assert.That(orchestrator.GetNextPaymentDate(accountId), Is.EqualTo(new DateTime(2026, 9, 8)));

            single.Amount = -1250;
            orchestrator.UpdateSingleRule(single);
            Assert.That(orchestrator.GetSingleRule(single.Id)!.Amount, Is.EqualTo(-1250));

            store.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = accountId,
                Description = "Auto",
                Amount = -10,
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = new DateTime(2026, 9, 15),
                IsActive = true,
                IsUserCreated = false
            });

            var forecast = new TransactionOrchestrator(store).GenerateForecast(
                accountId,
                new DateTime(2026, 9, 1),
                new DateTime(2026, 10, 2));
            Assert.That(forecast.Any(f => f.Description == "Rent"), Is.True);
            Assert.That(forecast.Any(f => f.Description == "Auto"), Is.True);
            Assert.That(forecast.Any(f => f.Description == "Sweep"), Is.True);

            orchestrator.DeleteSingleRule(single.Id);
            orchestrator.DeleteTransferRule(transfer.Id);
            Assert.That(orchestrator.GetSingleRules(accountId).Any(r => r.IsUserCreated), Is.False);
            Assert.That(orchestrator.GetTransferRules(accountId), Is.Empty);
        }
    }
}
