using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class PlanningOrchestratorTests
    {
        [Test]
        public void GetAllStatements_IncludesBankStatements()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today,
                StatementBalance = 100
            });

            var all = new PlanningOrchestrator(accounts, transactions, statements).GetAllStatements();

            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(all[0], Is.TypeOf<BankStatement>());
        }

        [Test]
        public void GetStatementListRows_MapsAmountDueWithoutInterest()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            statements.Save(new BankStatement
            {
                AccountId = checking.Id,
                StatementDate = new DateTime(2026, 8, 31),
                DueDate = new DateTime(2026, 8, 31),
                StatementBalance = 101.25m
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = new DateTime(2026, 8, 15),
                Amount = 1.25m,
                CategoryId = DefaultExpenseCategories.InterestId,
                Category = DefaultExpenseCategories.Interest
            });

            var rows = new PlanningOrchestrator(accounts, transactions, statements).GetStatementListRows(checking.Id);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].AmountDue, Is.EqualTo(AccountStatementListRow.NotApplicable));
            Assert.That(rows[0].StatementBalance, Is.EqualTo(101.25m.ToString("c2")));
        }

        [Test]
        public void DeleteStatement_RemovesStatement()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            var electric = new UntrackedAccount
            {
                Name = "Electric",
                Institution = "Duke",
                AccountNumber = "A1",
                Type = AccountType.Utility
            };
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(electric);
            var statement = new UtilityStatement
            {
                AccountId = electric.Id,
                StatementDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(5),
                AmountDue = 40,
                Charges = [new() { Description = "Energy", Amount = 40 }]
            };
            statements.Save(statement);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            orchestrator.DeleteStatement(statement.Id);

            Assert.That(statements.Get(statement.Id), Is.Null);
        }

        [Test]
        public void SaveStatement_RejectsInvalidDates()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            Assert.That(
                () => orchestrator.SaveStatement(new CreditCardStatement
                {
                    AccountId = card.Id,
                    StatementDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(-1),
                    AmountDue = 50,
                    StatementBalance = 50
                }),
                Throws.InvalidOperationException.With.Message.Contains("Due date"));
        }

        [Test]
        public void SaveStatement_AutoPayCreatesScheduledIntent()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "9", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            card.AutoPay = true;
            card.AutoPayFromAccountId = checking.Id;
            accounts.UpsertAccount(card);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            var statement = new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-5),
                DueDate = DateTime.Today.AddDays(10),
                AmountDue = 819.80m,
                StatementBalance = 17672.62m
            };

            orchestrator.SaveStatement(statement);

            var expected = transactions.GetAllFutureTransferTransactions().Single();
            Assert.That(expected.Origin, Is.EqualTo(ExpectedOrigin.StatementPay));
            Assert.That(expected.OriginId, Is.EqualTo(statement.Id));
            Assert.That(expected.ToAccountId, Is.EqualTo(card.Id));
            Assert.That(expected.FromAccountId, Is.EqualTo(checking.Id));
            Assert.That(expected.Amount, Is.EqualTo(819.80m));
            Assert.That(expected.Date, Is.EqualTo(statement.DueDate.Date));
            Assert.That(expected.Status, Is.EqualTo(ExpectedStatus.Scheduled));
        }

        [Test]
        public void SaveStatement_WithoutAutoPayDoesNotCreateIntent()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);

            orchestrator.SaveStatement(new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-20),
                DueDate = DateTime.Today.AddDays(10),
                AmountDue = 50,
                StatementBalance = 50
            });

            Assert.That(transactions.GetAllFutureTransferTransactions(), Is.Empty);
        }

        [Test]
        public void SyncAutoPayForAccount_SchedulesExistingStatements()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "9", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            card.AutoPay = true;
            card.AutoPayFromAccountId = checking.Id;
            accounts.UpsertAccount(card);
            var existing = statements.GetForAccount(card.Id).Single();

            new PlanningOrchestrator(accounts, transactions, statements).SyncAutoPayForAccount(card);

            var expected = transactions.GetAllFutureTransferTransactions().Single();
            Assert.That(expected.OriginId, Is.EqualTo(existing.Id));
            Assert.That(expected.FromAccountId, Is.EqualTo(checking.Id));
            Assert.That(expected.Amount, Is.EqualTo(existing.AmountDue));
        }

        [Test]
        public void SyncAutoPayForAccount_SchedulesOnlyLatestStatement()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "9", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            var older = statements.GetForAccount(card.Id).Single();
            var latest = new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-5),
                DueDate = DateTime.Today.AddDays(20),
                AmountDue = 3257.20m,
                StatementBalance = 17000
            };
            statements.Save(latest);
            card.AutoPay = true;
            card.AutoPayFromAccountId = checking.Id;
            accounts.UpsertAccount(card);

            new PlanningOrchestrator(accounts, transactions, statements).SyncAutoPayForAccount(card);

            var expected = transactions.GetAllFutureTransferTransactions().Single();
            Assert.That(expected.OriginId, Is.EqualTo(latest.Id));
            Assert.That(expected.Amount, Is.EqualTo(3257.20m));
            Assert.That(expected.Date, Is.EqualTo(latest.DueDate.Date));
            Assert.That(transactions.GetAllFutureTransferTransactions().Any(p => p.OriginId == older.Id), Is.False);
        }

        [Test]
        public void SaveStatement_AutoPayDropsOlderStatementIntents()
        {
            var (accounts, transactions, statements, card) = SeedCardStatement();
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "9", WebsiteUrl = "" };
            accounts.UpsertAccount(checking);
            card.AutoPay = true;
            card.AutoPayFromAccountId = checking.Id;
            accounts.UpsertAccount(card);
            var orchestrator = new PlanningOrchestrator(accounts, transactions, statements);
            orchestrator.SyncAutoPayForAccount(card);
            var older = statements.GetForAccount(card.Id).Single();
            Assert.That(transactions.GetAllFutureTransferTransactions().Single().OriginId, Is.EqualTo(older.Id));

            var latest = new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-5),
                DueDate = DateTime.Today.AddDays(20),
                AmountDue = 3257.20m,
                StatementBalance = 17000
            };
            orchestrator.SaveStatement(latest);

            var expected = transactions.GetAllFutureTransferTransactions().Single();
            Assert.That(expected.OriginId, Is.EqualTo(latest.Id));
            Assert.That(expected.Amount, Is.EqualTo(3257.20m));
        }

        private static (InMemoryAccountDataStore accounts, InMemoryTransactionDataStore transactions,
            InMemoryAccountStatementDataStore statements, CreditAccount card) SeedCardStatement()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new InMemoryAccountStatementDataStore();
            var card = new CreditAccount
            {
                Name = "Card",
                Institution = "X",
                AccountNumber = "1",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(card);
            statements.Save(new CreditCardStatement
            {
                AccountId = card.Id,
                StatementDate = DateTime.Today.AddDays(-15),
                DueDate = DateTime.Today.AddDays(7),
                AmountDue = 220,
                StatementBalance = 220,
                Promotions =
                [
                    new()
                    {
                        AccountId = card.Id,
                        Amount = 80,
                        Deadline = DateTime.Today.AddDays(5),
                        Type = PromoType.LumpSum
                    }
                ]
            });
            return (accounts, transactions, statements, card);
        }
    }
}
