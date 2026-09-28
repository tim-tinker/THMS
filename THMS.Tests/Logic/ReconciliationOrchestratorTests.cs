using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class ReconciliationOrchestratorTests
    {
        [Test]
        public void Recommend_DoesNotAdvanceRules()
        {
            var store = new InMemoryTransactionDataStore();
            var account = Guid.NewGuid();
            var rule = new RecurringSingleTransactionRule
            {
                AccountId = account,
                Amount = -40,
                Description = "Gym",
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today,
                IsActive = true
            };
            store.AddRecurringSingleRule(rule);
            var orchestrator = new ReconciliationOrchestrator(store);
            orchestrator.MaterializePlannedFromRules();
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account,
                Date = DateTime.Today,
                Amount = -40,
                Description = "Gym"
            });

            Assert.That(orchestrator.RecommendMatches(), Is.EqualTo(1));
            Assert.That(store.GetRecurringSingleRule(rule.Id)!.LastOccurrence, Is.Null);
            Assert.That(store.GetPostedTransactions(account).Single().RecommendedExpectedId, Is.Not.Null);
        }

        [Test]
        public void AcceptMatch_LinksAndCanUndo()
        {
            var store = new InMemoryTransactionDataStore();
            var account = Guid.NewGuid();
            var expected = new FutureSingleTransaction
            {
                AccountId = account,
                Date = DateTime.Today.AddDays(-1),
                Amount = -25,
                Description = "Coffee",
                Category = "Dining",
                Origin = ExpectedOrigin.Manual,
                Status = ExpectedStatus.Planned
            };
            store.AddFutureSingleTransaction(expected);
            var imported = new PostedTransaction
            {
                AccountId = account,
                Date = DateTime.Today,
                Amount = -25,
                Description = "Coffee"
            };
            store.AddPostedTransaction(imported);
            var orchestrator = new ReconciliationOrchestrator(store);
            orchestrator.RecommendMatches();

            orchestrator.AcceptMatch(imported.Id);
            Assert.That(store.GetPostedTransaction(imported.Id)!.ImportedStatus, Is.EqualTo(ImportedStatus.Matched));
            Assert.That(store.GetPostedTransaction(imported.Id)!.Category, Is.EqualTo("Dining"));
            Assert.That(store.GetFutureSingleTransaction(expected.Id)!.IsRealized, Is.True);
            Assert.That(orchestrator.GetUnreconciled(account), Is.Empty);

            orchestrator.UndoMatch(imported.Id);
            Assert.That(store.GetPostedTransaction(imported.Id)!.ImportedStatus, Is.EqualTo(ImportedStatus.Unreconciled));
            Assert.That(store.GetFutureSingleTransaction(expected.Id)!.IsRealized, Is.False);
        }

        [Test]
        public void AcceptAsNew_AndBulkOnOrBeforeDate()
        {
            var store = new InMemoryTransactionDataStore();
            var account = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account,
                Date = new DateTime(2026, 1, 1),
                Amount = -5,
                Description = "Old"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account,
                Date = new DateTime(2026, 2, 1),
                Amount = -7,
                Description = "OnCutoff"
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account,
                Date = new DateTime(2026, 3, 1),
                Amount = -6,
                Description = "Open"
            });
            var orchestrator = new ReconciliationOrchestrator(store);

            var result = orchestrator.AcceptAsNewOnOrBefore(account, new DateTime(2026, 2, 1));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Start, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(result.End, Is.EqualTo(new DateTime(2026, 2, 1)));
            Assert.That(store.GetPostedTransactions(account).Single(p => p.Description == "Old").ImportedStatus,
                Is.EqualTo(ImportedStatus.AcceptedNew));
            Assert.That(store.GetPostedTransactions(account).Single(p => p.Description == "OnCutoff").ImportedStatus,
                Is.EqualTo(ImportedStatus.AcceptedNew));
            Assert.That(store.GetPostedTransactions(account).Single(p => p.Description == "Open").ImportedStatus,
                Is.EqualTo(ImportedStatus.Unreconciled));
        }

        [Test]
        public void AcceptAsNew_RefreshesBudgetPeriodActuals()
        {
            var store = new InMemoryTransactionDataStore();
            var account = Guid.NewGuid();
            var budgets = new BudgetOrchestrator(store);
            budgets.AddRule(new ExpenseBudgetRule
            {
                AccountId = account,
                BudgetName = "Electric",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });

            var ruleId = budgets.GetRules().Single().Id;
            Assert.That(budgets.GetActivePeriod(ruleId)!.ActualExpenses, Is.EqualTo(0m));

            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account,
                Date = DateTime.Today,
                Amount = -48,
                Category = "Electric",
                CategoryId = DefaultExpenseCategories.ElectricId,
                Description = "Bill",
                ImportedStatus = ImportedStatus.Unreconciled
            });

            new ReconciliationOrchestrator(store).AcceptAsNew(store.GetPostedTransactions(account).Single().Id);

            var period = budgets.GetActivePeriod(ruleId)!;
            Assert.That(period.ActualExpenses, Is.EqualTo(48m));
            Assert.That(period.Remaining, Is.EqualTo(52m));
        }

        [Test]
        public void AcceptAsNew_DoesNotChangeBudgetWhenCategoryIsNotBudgeted()
        {
            var store = new InMemoryTransactionDataStore();
            var budgets = new BudgetOrchestrator(store);
            budgets.AddRule(new ExpenseBudgetRule
            {
                BudgetName = "Electric",
                IncludedCategoryIds = [DefaultExpenseCategories.ElectricId],
                BudgetFrequency = BudgetFrequency.Monthly,
                DefaultBudgetAmount = -100,
                IsActive = true
            });
            var ruleId = budgets.GetRules().Single().Id;
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Date = DateTime.Today,
                Amount = -75,
                Category = "Misc",
                Description = "Uncategorized import",
                ImportedStatus = ImportedStatus.Unreconciled
            });

            new ReconciliationOrchestrator(store).AcceptAsNew(store.GetPostedTransactions(DateTime.MinValue, DateTime.MaxValue).Single().Id);

            Assert.That(budgets.GetActivePeriod(ruleId)!.ActualExpenses, Is.EqualTo(0m));
        }

        [Test]
        public void AccountIdsWithUnreconciled_IncludesAccountsThatStillHaveImports()
        {
            var store = new InMemoryTransactionDataStore();
            var pending = Guid.NewGuid();
            var cleared = Guid.NewGuid();
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = pending,
                Date = DateTime.Today,
                Amount = -12,
                Description = "Open",
                ImportedStatus = ImportedStatus.Unreconciled
            });
            store.AddPostedTransaction(new PostedTransaction
            {
                AccountId = cleared,
                Date = DateTime.Today,
                Amount = -9,
                Description = "Done",
                ImportedStatus = ImportedStatus.AcceptedNew
            });

            var ids = new ReconciliationOrchestrator(store).AccountIdsWithUnreconciled();

            Assert.That(ids.Contains(pending), Is.True);
            Assert.That(ids.Contains(cleared), Is.False);
        }

        [Test]
        public void AccountIdsWithUnreconciled_ClearsAccountAfterAcceptAsNew()
        {
            var store = new InMemoryTransactionDataStore();
            var account = Guid.NewGuid();
            var imported = new PostedTransaction
            {
                AccountId = account,
                Date = DateTime.Today,
                Amount = -18,
                Description = "Only import",
                ImportedStatus = ImportedStatus.Unreconciled
            };
            store.AddPostedTransaction(imported);
            var orchestrator = new ReconciliationOrchestrator(store);

            Assert.That(orchestrator.AccountIdsWithUnreconciled().Contains(account), Is.True);
            orchestrator.AcceptAsNew(imported.Id);
            Assert.That(orchestrator.AccountIdsWithUnreconciled().Contains(account), Is.False);
        }

        [Test]
        public void ChangeMatch_UsesSelectedExpected()
        {
            var store = new InMemoryTransactionDataStore();
            var account = Guid.NewGuid();
            var first = new FutureSingleTransaction
            {
                AccountId = account,
                Date = DateTime.Today,
                Amount = -10,
                Description = "A"
            };
            var second = new FutureSingleTransaction
            {
                AccountId = account,
                Date = DateTime.Today,
                Amount = -10,
                Description = "B"
            };
            store.AddFutureSingleTransaction(first);
            store.AddFutureSingleTransaction(second);
            var imported = new PostedTransaction
            {
                AccountId = account,
                Date = DateTime.Today,
                Amount = -10,
                Description = "A"
            };
            store.AddPostedTransaction(imported);
            var orchestrator = new ReconciliationOrchestrator(store);
            orchestrator.RecommendMatches();
            orchestrator.AcceptMatch(imported.Id, second.Id);

            Assert.That(store.GetFutureSingleTransaction(second.Id)!.IsRealized, Is.True);
            Assert.That(store.GetFutureSingleTransaction(first.Id)!.IsRealized, Is.False);
        }

        [Test]
        public void Materialize_CreatesPlannedAndPastDatesArePending()
        {
            var store = new InMemoryTransactionDataStore();
            var rule = new RecurringSingleTransactionRule
            {
                AccountId = Guid.NewGuid(),
                Amount = -10,
                Description = "Gym",
                Frequency = RecurrenceFrequency.Monthly,
                NextOccurrence = DateTime.Today.AddDays(-3),
                IsActive = true
            };
            store.AddRecurringSingleRule(rule);
            var orchestrator = new ReconciliationOrchestrator(store);
            orchestrator.MaterializePlannedFromRules();

            var expected = store.GetAllFutureSingleTransactions().Single();
            Assert.That(expected.Origin, Is.EqualTo(ExpectedOrigin.RecurringSingle));
            Assert.That(expected.Status, Is.EqualTo(ExpectedStatus.Planned));
            Assert.That(expected.DisplayStatus(), Is.EqualTo(TransactionStatuses.Pending));
        }
    }
}
