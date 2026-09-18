using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Planning;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class StatementImportOrchestratorTests
    {
        [Test]
        public void LoadStatementsFromFile_SkipsUnknownAndUnsupportedAccounts()
        {
            var path = WriteCsv("""
                Account,Statement Date,Due Date,Amount Due,Statement Balance,Escrow Balance,Notes
                Checking,2026-01-15,2026-01-15,0,1000,0,bank note
                Visa,2026-01-20,2026-02-10,125,800,0,card note
                Unknown,2026-01-01,2026-01-15,10,0,0,
                Brokerage,2026-01-05,2026-01-05,0,50,0,
                """);
            try
            {
                var (orchestrator, _, _, _) = Create();
                var rows = orchestrator.LoadStatementsFromFile(path);

                Assert.That(rows, Has.Count.EqualTo(2));
                Assert.That(rows[0].Account, Is.EqualTo("Checking"));
                Assert.That(rows[0].Type, Is.EqualTo("Bank"));
                Assert.That(rows[0].AmountDue, Is.EqualTo(0m));
                Assert.That(rows[0].StatementBalance, Is.EqualTo(1000m));
                Assert.That(rows[1].Account, Is.EqualTo("Visa"));
                Assert.That(rows[1].Type, Is.EqualTo("Credit Card"));
                Assert.That(rows[1].AmountDue, Is.EqualTo(125m));
                Assert.That(rows.Any(r => r.Account == "Unknown"), Is.False);
                Assert.That(rows.Any(r => r.Account == "Brokerage"), Is.False);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ImportStatements_CreatesTypedStatementsAndSkipsDuplicates()
        {
            var path = WriteCsv("""
                Account,Statement Date,Due Date,Amount Due,Statement Balance,Escrow Balance,Notes,Pay From
                Checking,2026-01-15,2026-01-20,25,1090.73,0,bank,
                Visa,2026-01-20,2026-02-10,125,800,0,card,Checking
                Mortgage,2026-01-01,2026-01-15,1800,200000,4500,house,Checking
                Electric,2026-01-10,2026-01-28,87.50,0,0,kWh,Checking
                """);
            try
            {
                var (orchestrator, _, statements, _) = Create();
                var rows = orchestrator.LoadStatementsFromFile(path);

                var imported = orchestrator.ImportStatements(rows);
                Assert.That(imported.Count, Is.EqualTo(4));

                var checking = statements.GetForAccount(rows.Single(r => r.Account == "Checking").AccountId);
                Assert.That(checking, Has.Count.EqualTo(1));
                Assert.That(checking[0], Is.TypeOf<BankStatement>());
                Assert.That(((BankStatement)checking[0]).StatementBalance, Is.EqualTo(1090.73m));
                Assert.That(checking[0].AmountDue, Is.EqualTo(0m));
                Assert.That(checking[0].DueDate, Is.EqualTo(new DateTime(2026, 1, 15)));

                var card = statements.GetForAccount(rows.Single(r => r.Account == "Visa").AccountId).Single();
                Assert.That(card, Is.TypeOf<CreditCardStatement>());
                Assert.That(card.AmountDue, Is.EqualTo(125m));

                var mortgage = (MortgageStatement)statements
                    .GetForAccount(rows.Single(r => r.Account == "Mortgage").AccountId).Single();
                Assert.That(mortgage.EscrowBalance, Is.EqualTo(4500m));
                Assert.That(mortgage.StatementBalance, Is.EqualTo(200000m));

                var utility = (UtilityStatement)statements
                    .GetForAccount(rows.Single(r => r.Account == "Electric").AccountId).Single();
                Assert.That(utility.Charges, Has.Count.EqualTo(1));
                Assert.That(utility.Charges[0].Amount, Is.EqualTo(87.50m));

                var second = orchestrator.ImportStatements(rows);
                Assert.That(second.Count, Is.EqualTo(0));
                Assert.That(statements.GetForAccount(card.AccountId), Has.Count.EqualTo(1));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void LoadStatementsFromFile_ThrowsWhenMissing()
        {
            var (orchestrator, _, _, _) = Create();
            Assert.That(
                () => orchestrator.LoadStatementsFromFile(@"C:\missing\statements.xlsx"),
                Throws.TypeOf<FileNotFoundException>());
        }

        private static (StatementImportOrchestrator orchestrator, InMemoryAccountDataStore accounts,
            InMemoryAccountStatementDataStore statements, InMemoryTransactionDataStore transactions) Create()
        {
            var accounts = new InMemoryAccountDataStore();
            accounts.UpsertAccount(Account("Checking", new BankAccount { Type = AccountType.Checking }));
            accounts.UpsertAccount(Account("Visa", new CreditAccount { Type = AccountType.CreditCard }));
            accounts.UpsertAccount(Account("Mortgage", new MortgageAccount { Type = AccountType.Mortgage }));
            accounts.UpsertAccount(Account("Electric", new UntrackedAccount { Type = AccountType.Utility }));
            accounts.UpsertAccount(Account("Brokerage", new InvestmentAccount { Type = AccountType.Investment }));

            var statements = new InMemoryAccountStatementDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var orchestrator = new StatementImportOrchestrator(accounts, statements, transactions);
            return (orchestrator, accounts, statements, transactions);
        }

        private static Account Account(string name, Account account)
        {
            account.Name = name;
            account.Institution = "Test";
            account.AccountNumber = "1";
            account.WebsiteUrl = "";
            return account;
        }

        private static string WriteCsv(string csv)
        {
            var path = Path.Combine(Path.GetTempPath(), $"statements-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, csv);
            return path;
        }
    }
}
