using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Investments;
using THMS.Domain.Finance.Planning;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AccountConversionTests
    {
        [Test]
        public void CompatibleConversion_KeepsTheAccountAndItsActivity()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new AccountStatementDataStore();
            var book = new InMemoryInvestmentBookStore();
            var orchestrator = new AccountOrchestrator(accounts, transactions, statements, book);

            var hsa = new BankAccount { Name = "HSA", PostedBalance = 80 };
            var checking = new BankAccount { Name = "Checking", PostedBalance = 25 };
            accounts.UpsertAccount(hsa);
            accounts.UpsertAccount(checking);
            AddActivity(transactions, statements, book, hsa.Id, checking.Id);

            var credit = new CreditAccount
            {
                Id = hsa.Id,
                Name = "HSA",
                Type = AccountType.CreditCard,
                CreditLimit = 5000,
                StatementDate = new DateTime(2026, 10, 31),
                DueDate = new DateTime(2026, 11, 20)
            };

            orchestrator.Save(credit);

            var saved = accounts.GetAllAccounts().Single(account => account.Id == hsa.Id);
            Assert.That(saved, Is.InstanceOf<CreditAccount>());
            Assert.That(accounts.GetAllAccounts().Count(), Is.EqualTo(2));
            Assert.That(transactions.GetPostedTransactions(hsa.Id).Count(), Is.EqualTo(1));
            Assert.That(transactions.GetPostedTransferTransactions(hsa.Id).Count(), Is.EqualTo(1));
            Assert.That(transactions.GetPostedTransferTransactions(checking.Id).Count(), Is.EqualTo(1));
            Assert.That(transactions.GetFutureSingleTransactions(hsa.Id).Count(), Is.EqualTo(1));
            Assert.That(statements.GetForAccount(hsa.Id), Has.Count.EqualTo(1));
            Assert.That(book.GetActions(hsa.Id), Has.Count.EqualTo(1));
        }

        [Test]
        public void UtilityToService_KeepsTransactionsOnTheSameAccount()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var orchestrator = new AccountOrchestrator(
                accounts, transactions, new AccountStatementDataStore(), new InMemoryInvestmentBookStore());

            var utility = new UntrackedAccount { Name = "Power", Type = AccountType.Utility };
            accounts.UpsertAccount(utility);
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = utility.Id,
                Date = new DateTime(2026, 10, 2),
                Description = "Bill",
                Amount = -40
            });

            orchestrator.Save(new UntrackedAccount
            {
                Id = utility.Id,
                Name = "Power",
                Type = AccountType.Service
            });

            var saved = accounts.GetAllAccounts().Single();
            Assert.That(saved.Type, Is.EqualTo(AccountType.Service));
            Assert.That(transactions.GetPostedTransactions(utility.Id).Single().Amount, Is.EqualTo(-40));
        }

        [Test]
        public void InvestmentConversion_RequiresConfirmationThenRemovesActivity()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var statements = new AccountStatementDataStore();
            var book = new InMemoryInvestmentBookStore();
            var orchestrator = new AccountOrchestrator(accounts, transactions, statements, book);

            var hsa = new BankAccount { Name = "HSA", PostedBalance = 80 };
            var checking = new BankAccount { Name = "Checking", PostedBalance = 25 };
            accounts.UpsertAccount(hsa);
            accounts.UpsertAccount(checking);
            var counterpartId = AddActivity(transactions, statements, book, hsa.Id, checking.Id);

            var investment = new InvestmentAccount
            {
                Id = hsa.Id,
                Name = "HSA",
                Type = AccountType.Investment
            };

            var blocked = Assert.Throws<InvalidOperationException>(() => orchestrator.Save(investment));
            Assert.That(blocked!.Message, Is.EqualTo(AccountConversion.LossWarning));
            Assert.That(transactions.GetPostedTransactions(hsa.Id).Count(), Is.EqualTo(1));
            Assert.That(accounts.GetAllAccounts().Single(account => account.Id == hsa.Id), Is.InstanceOf<BankAccount>());

            orchestrator.Save(investment, discardActivity: true);

            Assert.That(accounts.GetAllAccounts().Single(account => account.Id == hsa.Id), Is.InstanceOf<InvestmentAccount>());
            Assert.That(accounts.GetAllAccounts().Count(), Is.EqualTo(2));
            Assert.That(transactions.GetPostedTransactions(hsa.Id), Is.Empty);
            Assert.That(transactions.GetPostedTransferTransactions(hsa.Id), Is.Empty);
            Assert.That(transactions.GetPostedTransferTransaction(counterpartId), Is.Null);
            Assert.That(transactions.GetPostedTransactions(checking.Id).Single().Description, Is.EqualTo("Paycheck"));
            Assert.That(transactions.GetFutureSingleTransactions(hsa.Id), Is.Empty);
            Assert.That(transactions.GetFutureTransferTransactions(hsa.Id), Is.Empty);
            Assert.That(transactions.GetRecurringSingleRules(hsa.Id), Is.Empty);
            Assert.That(transactions.GetRecurringTransferRules(hsa.Id), Is.Empty);
            Assert.That(statements.GetForAccount(hsa.Id), Is.Empty);
            Assert.That(book.GetActions(hsa.Id), Is.Empty);
            Assert.That(book.GetQuotes(hsa.Id), Is.Empty);
        }

        [Test]
        public void LeavingInvestment_RemovesHoldingsWhenConfirmed()
        {
            var accounts = new InMemoryAccountDataStore();
            var book = new InMemoryInvestmentBookStore();
            var orchestrator = new AccountOrchestrator(
                accounts, new InMemoryTransactionDataStore(), new AccountStatementDataStore(), book);

            var hsa = new InvestmentAccount { Name = "HSA", Type = AccountType.Investment, CashBalance = 40 };
            accounts.UpsertAccount(hsa);
            book.UpsertAction(new InvestmentAction
            {
                AccountId = hsa.Id,
                ImportKey = "opening|AGNC|2026-10-31",
                RunDate = new DateTime(2026, 10, 31),
                Symbol = "AGNC",
                Kind = InvestmentActionKind.Opening,
                Status = InvestmentActionStatus.Applied,
                Quantity = 10
            });

            orchestrator.Save(new BankAccount { Id = hsa.Id, Name = "HSA" }, discardActivity: true);

            Assert.That(accounts.GetAllAccounts().Single(), Is.InstanceOf<BankAccount>());
            Assert.That(book.GetActions(hsa.Id), Is.Empty);
        }

        [Test]
        public void SqliteConversion_ReplacesTheSubtypeRowAndKeepsCarriedTransactions()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-convert-{Guid.NewGuid():N}.db");
            try
            {
                var accounts = new SQLiteAccountDataStore(path);
                var transactions = new SQLiteTransactionDataStore(path);
                var book = new SQLiteInvestmentBookStore(path);
                var orchestrator = new AccountOrchestrator(
                    accounts, transactions, new AccountStatementDataStore(), book);

                var hsa = new BankAccount
                {
                    Name = "HSA",
                    Institution = "Fidelity",
                    AccountNumber = "257460702",
                    WebsiteUrl = "",
                    PostedBalance = 80
                };
                accounts.UpsertAccount(hsa);
                transactions.AddPostedTransaction(new PostedTransaction
                {
                    AccountId = hsa.Id,
                    Date = new DateTime(2026, 10, 1),
                    Description = "Deposit",
                    Amount = 80
                });

                orchestrator.Save(new CreditAccount
                {
                    Id = hsa.Id,
                    Name = "HSA",
                    Type = AccountType.CreditCard,
                    CreditLimit = 1000,
                    StatementDate = new DateTime(2026, 10, 31),
                    DueDate = new DateTime(2026, 11, 20)
                });

                var carried = accounts.GetAllAccounts().Single();
                Assert.That(carried, Is.InstanceOf<CreditAccount>());
                Assert.That(carried.Id, Is.EqualTo(hsa.Id));
                Assert.That(transactions.GetPostedTransactions(hsa.Id).Single().Amount, Is.EqualTo(80));
                Assert.That(Count(path, "SELECT COUNT(*) FROM BankAccounts WHERE AccountId = @Id", hsa.Id), Is.Zero);
                Assert.That(Count(path, "SELECT COUNT(*) FROM CreditAccounts WHERE AccountId = @Id", hsa.Id), Is.EqualTo(1));
                Assert.That(Count(path, "SELECT COUNT(*) FROM Accounts WHERE Id = @Id", hsa.Id), Is.EqualTo(1));

                book.UpsertQuote(new SecurityQuote
                {
                    AccountId = hsa.Id,
                    Symbol = "AGNC",
                    AsOf = new DateTime(2026, 10, 30),
                    Price = 8.65m
                });

                orchestrator.Save(
                    new InvestmentAccount { Id = hsa.Id, Name = "HSA", Type = AccountType.Investment },
                    discardActivity: true);

                var converted = accounts.GetAllAccounts().Single();
                Assert.That(converted, Is.InstanceOf<InvestmentAccount>());
                Assert.That(converted.Id, Is.EqualTo(hsa.Id));
                Assert.That(transactions.GetPostedTransactions(hsa.Id), Is.Empty);
                Assert.That(book.GetQuotes(hsa.Id), Is.Empty);
                Assert.That(Count(path, "SELECT COUNT(*) FROM CreditAccounts WHERE AccountId = @Id", hsa.Id), Is.Zero);
                Assert.That(Count(path, "SELECT COUNT(*) FROM BankAccounts WHERE AccountId = @Id", hsa.Id), Is.Zero);
                Assert.That(Count(path, "SELECT COUNT(*) FROM InvestmentAccounts WHERE AccountId = @Id", hsa.Id), Is.EqualTo(1));
                Assert.That(Count(path, "SELECT COUNT(*) FROM Accounts WHERE Id = @Id", hsa.Id), Is.EqualTo(1));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                {
                    var file = path + suffix;
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        private static Guid AddActivity(
            InMemoryTransactionDataStore transactions,
            AccountStatementDataStore statements,
            InMemoryInvestmentBookStore book,
            Guid hsaId,
            Guid checkingId)
        {
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = hsaId,
                Date = new DateTime(2026, 10, 1),
                Description = "Deposit",
                Amount = 80
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checkingId,
                Date = new DateTime(2026, 10, 1),
                Description = "Paycheck",
                Amount = 25
            });

            var outgoing = new PostedTransferTransaction
            {
                AccountId = hsaId,
                Date = new DateTime(2026, 10, 3),
                Description = "To checking",
                Amount = -20,
                FromAccountId = hsaId,
                ToAccountId = checkingId,
                Direction = TransferDirection.Outgoing
            };
            var incoming = new PostedTransferTransaction
            {
                AccountId = checkingId,
                Date = new DateTime(2026, 10, 3),
                Description = "From HSA",
                Amount = 20,
                FromAccountId = hsaId,
                ToAccountId = checkingId,
                Direction = TransferDirection.Incoming,
                RelatedPostedTransactionId = outgoing.Id
            };
            outgoing.RelatedPostedTransactionId = incoming.Id;
            transactions.AddPostedTransferTransaction(outgoing);
            transactions.AddPostedTransferTransaction(incoming);

            transactions.AddFutureSingleTransaction(new FutureSingleTransaction
            {
                AccountId = hsaId,
                Date = new DateTime(2026, 11, 1),
                Description = "Planned",
                Amount = -5
            });
            transactions.AddFutureTransferTransaction(new FutureTransferTransaction
            {
                FromAccountId = hsaId,
                ToAccountId = checkingId,
                Date = new DateTime(2026, 11, 2),
                Description = "Planned transfer",
                Amount = 5
            });
            transactions.AddRecurringSingleRule(new RecurringSingleTransactionRule
            {
                AccountId = hsaId,
                Date = new DateTime(2026, 11, 1),
                Description = "Fee",
                Amount = -1,
                Frequency = RecurrenceFrequency.Monthly
            });
            transactions.AddRecurringTransferRule(new RecurringTransferRule
            {
                FromAccountId = hsaId,
                ToAccountId = checkingId,
                Date = new DateTime(2026, 11, 1),
                Description = "Move",
                Amount = 5,
                Frequency = RecurrenceFrequency.Monthly
            });

            statements.Save(new BankStatement
            {
                AccountId = hsaId,
                StatementDate = new DateTime(2026, 10, 31),
                DueDate = new DateTime(2026, 10, 31),
                StatementBalance = 80
            });
            book.UpsertAction(new InvestmentAction
            {
                AccountId = hsaId,
                ImportKey = "opening-cash|2026-10-31",
                RunDate = new DateTime(2026, 10, 31),
                Kind = InvestmentActionKind.OpeningCash,
                Status = InvestmentActionStatus.Applied,
                Amount = 80
            });
            book.UpsertQuote(new SecurityQuote
            {
                AccountId = hsaId,
                Symbol = "AGNC",
                AsOf = new DateTime(2026, 10, 30),
                Price = 8.65m
            });
            return incoming.Id;
        }

        private static int Count(string path, string sql, Guid id)
        {
            using var conn = new SqliteConnection($"Data Source={path}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Id", id.ToString());
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
