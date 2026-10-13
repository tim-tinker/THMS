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
            Assert.That(posted.ImportedStatus, Is.EqualTo(ImportedStatus.Matched));
            Assert.That(store.GetPostedTransactions(accountId).Single().Id, Is.EqualTo(posted.Id));
            Assert.That(store.GetFutureSingleTransactions(accountId), Is.Empty);

            var ledger = THMS.Logic.ViewModels.Finance.UnifiedTransactionViewBuilder.Build(
                store.GetPostedTransactions(accountId),
                store.GetPostedTransferTransactions(accountId),
                forAccountId: accountId);
            Assert.That(ledger.Single().Date, Is.EqualTo(bought));
            Assert.That(ledger.Single().Status, Is.EqualTo(TransactionStatuses.Reconciled));
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
            Assert.That(posted.ImportedStatus, Is.EqualTo(ImportedStatus.Matched));
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
        public void UpdateLedgerEntry_ChangesDateAmountDescriptionAndCategory()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var accountId = Guid.NewGuid();
            var posted = orchestrator.AddPosted(
                accountId,
                new DateTime(2026, 10, 11),
                -2.50m,
                "Allowance",
                DefaultExpenseCategories.UncategorizedId);

            orchestrator.UpdateLedgerEntry(
                posted.Id,
                new DateTime(2026, 10, 2),
                -3m,
                "Corrected",
                DefaultExpenseCategories.PaymentId);

            var saved = store.GetPostedTransaction(posted.Id);
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.Date, Is.EqualTo(new DateTime(2026, 10, 2)));
            Assert.That(saved.Amount, Is.EqualTo(-3m));
            Assert.That(saved.Description, Is.EqualTo("Corrected"));
            Assert.That(saved.CategoryId, Is.EqualTo(DefaultExpenseCategories.PaymentId));
            Assert.That(saved.ImportedStatus, Is.EqualTo(ImportedStatus.Matched));
        }

        [Test]
        public void UpdateLedgerEntry_AllowsDateChangeWhenSplitAndRejectsAmountChange()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var posted = orchestrator.AddPosted(
                Guid.NewGuid(),
                new DateTime(2026, 10, 11),
                -2.50m,
                "Allowance",
                DefaultExpenseCategories.UncategorizedId);
            store.SaveSplits(posted.Id,
            [
                new SplitTransactionRow
                {
                    Id = Guid.NewGuid(),
                    ParentTransactionId = posted.Id,
                    Amount = -2.50m,
                    Type = SplitType.Expense,
                    CategoryId = DefaultExpenseCategories.UncategorizedId
                }
            ]);

            Assert.Throws<InvalidOperationException>(() => orchestrator.UpdateLedgerEntry(
                posted.Id,
                posted.Date,
                -3m,
                posted.Description,
                posted.CategoryId));

            var corrected = new DateTime(2026, 10, 2);
            orchestrator.UpdateLedgerEntry(
                posted.Id,
                corrected,
                posted.Amount,
                "Dated",
                posted.CategoryId);

            var saved = store.GetPostedTransaction(posted.Id);
            Assert.That(saved!.Date, Is.EqualTo(corrected));
            Assert.That(saved.Description, Is.EqualTo("Dated"));
            Assert.That(saved.Amount, Is.EqualTo(-2.50m));
            Assert.That(saved.HasSplits, Is.True);
        }

        [Test]
        public void DeleteLedgerEntry_RemovesPostedTransactionAndItsSplits()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var accountId = Guid.NewGuid();
            var posted = orchestrator.AddPosted(
                accountId,
                new DateTime(2026, 10, 11),
                -2.50m,
                "Allowance",
                DefaultExpenseCategories.UncategorizedId);
            store.SaveSplits(posted.Id,
            [
                new SplitTransactionRow
                {
                    Id = Guid.NewGuid(),
                    ParentTransactionId = posted.Id,
                    Amount = -2.50m,
                    Type = SplitType.Expense
                }
            ]);

            orchestrator.DeleteLedgerEntry(posted.Id);

            Assert.That(store.GetPostedTransaction(posted.Id), Is.Null);
            Assert.That(store.GetSplits(posted.Id), Is.Empty);
            Assert.That(store.GetPostedTransactions(accountId), Is.Empty);
        }

        [Test]
        public void DeleteLedgerEntry_RemovesBothSidesOfAPostedTransfer()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var checking = Guid.NewGuid();
            var card = Guid.NewGuid();
            var from = new PostedTransferTransaction
            {
                AccountId = checking,
                FromAccountId = checking,
                ToAccountId = card,
                Date = new DateTime(2026, 10, 11),
                Amount = -40m,
                Description = "Card payment",
                ImportedStatus = ImportedStatus.Matched
            };
            var to = new PostedTransferTransaction
            {
                AccountId = card,
                FromAccountId = checking,
                ToAccountId = card,
                RelatedPostedTransactionId = from.Id,
                Date = from.Date,
                Amount = 40m,
                Description = "Card payment",
                ImportedStatus = ImportedStatus.Matched
            };
            from.RelatedPostedTransactionId = to.Id;
            store.AddPostedTransferTransaction(from);
            store.AddPostedTransferTransaction(to);

            orchestrator.DeleteLedgerEntry(from.Id);

            Assert.That(store.GetPostedTransferTransaction(from.Id), Is.Null);
            Assert.That(store.GetPostedTransferTransaction(to.Id), Is.Null);
        }

        [Test]
        public void DeleteLedgerEntry_RemovesAFutureTransfer()
        {
            var store = new InMemoryTransactionDataStore();
            var orchestrator = new AccountActivityOrchestrator(store, store);
            var transfer = orchestrator.AddTransfer(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, 25m, "Move");

            orchestrator.DeleteLedgerEntry(transfer.Id);

            Assert.That(store.GetFutureTransferTransaction(transfer.Id), Is.Null);
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
