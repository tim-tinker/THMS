using THMS.Data.Stores;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AccountActivityOrchestratorTests
    {
        [Test]
        public void AddPending_CreatesUserFutureOnAccount()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var accountId = Guid.NewGuid();

            var pending = orchestrator.AddPending(
                accountId,
                new DateTime(2026, 9, 20),
                -42.50m,
                "Coffee",
                DefaultExpenseCategories.RestaurantsId);

            Assert.That(pending.IsUserCreated, Is.True);
            Assert.That(pending.IsRealized, Is.False);
            Assert.That(pending.AccountId, Is.EqualTo(accountId));
            Assert.That(pending.Amount, Is.EqualTo(-42.50m));
            Assert.That(pending.CategoryId, Is.EqualTo(DefaultExpenseCategories.RestaurantsId));
            Assert.That(store.GetFutureSingleTransactions(accountId).Single().Id, Is.EqualTo(pending.Id));
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
        public void AddPending_RejectsZeroAmount()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            Assert.That(
                () => orchestrator.AddPending(Guid.NewGuid(), DateTime.Today, 0, "X", null),
                Throws.InvalidOperationException.With.Message.Contains("zero"));
        }
    }
}
