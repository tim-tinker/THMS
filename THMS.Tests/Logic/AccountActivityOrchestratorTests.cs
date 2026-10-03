using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AccountActivityOrchestratorTests
    {
        [Test]
        public void AddPosted_RecordsUnmatchedChargeOnTheEnteredDate()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var accountId = Guid.NewGuid();
            var bought = new DateTime(2026, 9, 28);

            var posted = orchestrator.AddPosted(
                accountId,
                bought,
                -1093.94m,
                "Charge",
                DefaultExpenseCategories.UncategorizedId);

            Assert.That(posted.Date, Is.EqualTo(bought));
            Assert.That(posted.Amount, Is.EqualTo(-1093.94m));
            Assert.That(posted.ImportedStatus, Is.EqualTo(ImportedStatus.Unmatched));
            Assert.That(store.GetPostedTransactions(accountId).Single().Id, Is.EqualTo(posted.Id));
            Assert.That(store.GetFutureSingleTransactions(accountId), Is.Empty);

            var ledger = THMS.Logic.ViewModels.Finance.UnifiedTransactionViewBuilder.Build(
                store.GetPostedTransactions(accountId),
                store.GetPostedTransferTransactions(accountId),
                forAccountId: accountId);
            Assert.That(ledger.Single().Date, Is.EqualTo(bought));
            Assert.That(ledger.Single().Status, Is.EqualTo(TransactionStatuses.Unmatched));
        }

        [Test]
        public void AddPosted_WritesInterestToLedger()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var accountId = Guid.NewGuid();

            var posted = orchestrator.AddPosted(
                accountId,
                DateTime.Today,
                170.94m,
                "Interest",
                DefaultExpenseCategories.InterestId);

            Assert.That(posted.Amount, Is.EqualTo(170.94m));
            Assert.That(posted.ImportedStatus, Is.EqualTo(ImportedStatus.Unmatched));
            Assert.That(posted.CategoryId, Is.EqualTo(DefaultExpenseCategories.InterestId));
            Assert.That(store.GetPostedTransactions(accountId).Single().Id, Is.EqualTo(posted.Id));
            Assert.That(store.GetFutureSingleTransactions(accountId), Is.Empty);
        }

        [Test]
        public void AddTransfer_CreatesPendingTransfer()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var from = Guid.NewGuid();
            var to = Guid.NewGuid();

            var transfer = orchestrator.AddTransfer(from, to, DateTime.Today, 200, "To savings");

            Assert.That(transfer.FromAccountId, Is.EqualTo(from));
            Assert.That(transfer.ToAccountId, Is.EqualTo(to));
            Assert.That(transfer.Amount, Is.EqualTo(200m));
            Assert.That(transfer.IsUserCreated, Is.True);
            Assert.That(transfer.CategoryId, Is.EqualTo(DefaultExpenseCategories.PaymentId));
            Assert.That(store.GetFutureTransferTransactions(from).Single().Id, Is.EqualTo(transfer.Id));
        }

        [Test]
        public void AddPosted_RejectsZeroAmount()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            Assert.That(
                () => orchestrator.AddPosted(Guid.NewGuid(), DateTime.Today, 0, "X", null),
                Throws.InvalidOperationException.With.Message.Contains("zero"));
        }
    }
}
