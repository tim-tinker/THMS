using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
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
        public void AcceptAsNew_AndBulkBeforeDate()
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
                Date = new DateTime(2026, 3, 1),
                Amount = -6,
                Description = "Open"
            });
            var orchestrator = new ReconciliationOrchestrator(store);

            Assert.That(orchestrator.AcceptAsNewBefore(account, new DateTime(2026, 2, 1)), Is.EqualTo(1));
            Assert.That(store.GetPostedTransactions(account).Single(p => p.Description == "Old").ImportedStatus,
                Is.EqualTo(ImportedStatus.AcceptedNew));
            Assert.That(store.GetPostedTransactions(account).Single(p => p.Description == "Open").ImportedStatus,
                Is.EqualTo(ImportedStatus.Unreconciled));
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
