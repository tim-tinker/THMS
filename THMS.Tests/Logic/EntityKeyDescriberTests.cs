using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Diagnostics;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class EntityKeyDescriberTests
    {
        [Test]
        public void DescribeIdsIn_NamesPostedAndTransferCopiesAndTheOtherSide()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var savings = new BankAccount { Name = "WF Way2Save", Institution = "WF", AccountNumber = "1" };
            var checking = new BankAccount { Name = "WF Checking", Institution = "WF", AccountNumber = "2" };
            accounts.UpsertAccount(savings);
            accounts.UpsertAccount(checking);

            var savingsId = Guid.NewGuid();
            var checkingId = Guid.NewGuid();
            transactions.AddPostedTransaction(new PostedTransaction
            {
                Id = savingsId,
                AccountId = savings.Id,
                Date = new DateTime(2026, 8, 21),
                Amount = 1000,
                Description = "RECURRING TRANSFER FROM CHECKING"
            });
            transactions.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                Id = savingsId,
                AccountId = savings.Id,
                Date = new DateTime(2026, 8, 21),
                Amount = 1000,
                Description = "RECURRING TRANSFER FROM CHECKING",
                RelatedPostedTransactionId = checkingId
            });
            transactions.AddPostedTransferTransaction(new PostedTransferTransaction
            {
                Id = checkingId,
                AccountId = checking.Id,
                Date = new DateTime(2026, 8, 21),
                Amount = -1000,
                Description = "RECURRING TRANSFER TO SAVINGS",
                RelatedPostedTransactionId = savingsId
            });

            var lines = new EntityKeyDescriber(accounts, transactions, statements)
                .DescribeIdsIn($"An item with the same key has already been added. Key: {savingsId}");

            Assert.That(lines, Has.Some.Contains($"Posted transaction {savingsId:D}:"));
            Assert.That(lines, Has.Some.Contains($"Posted transfer {savingsId:D}:"));
            Assert.That(lines, Has.Some.Contains($"Related posted transfer {checkingId:D}:"));
            Assert.That(string.Join("\n", lines), Does.Contain("WF Way2Save"));
            Assert.That(string.Join("\n", lines), Does.Contain("WF Checking"));
        }

        [Test]
        public void DescribeIdsIn_SaysWhenTheIdIsUnknown()
        {
            var id = Guid.NewGuid();
            var lines = new EntityKeyDescriber(
                    new InMemoryAccountDataStore(),
                    new InMemoryTransactionDataStore(),
                    new InMemoryAccountStatementDataStore())
                .DescribeIdsIn($"Key: {id}");

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Does.Contain(id.ToString()));
            Assert.That(lines[0], Does.Contain("was not found"));
        }

        [Test]
        public void DescribeIdsIn_IgnoresTextWithoutAnId()
        {
            var lines = new EntityKeyDescriber(
                    new InMemoryAccountDataStore(),
                    new InMemoryTransactionDataStore(),
                    new InMemoryAccountStatementDataStore())
                .DescribeIdsIn("Select an expected transaction to match.");

            Assert.That(lines, Is.Empty);
        }
    }
}
