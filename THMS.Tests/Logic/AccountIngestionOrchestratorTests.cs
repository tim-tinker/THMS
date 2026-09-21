using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels.Finance;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AccountIngestionOrchestratorTests
    {
        [Test]
        public void ImportAccounts_ReusesExistingIdByName()
        {
            var store = new InMemoryAccountDataStore();
            var existing = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "111",
                WebsiteUrl = ""
            };
            store.UpsertAccount(existing);
            var orchestrator = new AccountImportOrchestrator(store);
            var incoming = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "222",
                WebsiteUrl = "https://bank"
            };

            var imported = orchestrator.ImportAccounts([AccountImportPreview.FromAccount(incoming)]);

            Assert.That(imported.Count, Is.EqualTo(1));
            var accounts = store.GetAllAccounts().ToList();
            Assert.That(accounts, Has.Count.EqualTo(1));
            Assert.That(accounts[0].Id, Is.EqualTo(existing.Id));
            Assert.That(accounts[0].AccountNumber, Is.EqualTo("222"));
        }

        [Test]
        public void ImportAccounts_AppliesPreviewEdits()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountImportOrchestrator(store);
            var incoming = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "111",
                WebsiteUrl = ""
            };
            var preview = AccountImportPreview.FromAccount(incoming);
            preview.Name = "New Checking";
            preview.AccountNumber = "999";
            preview.WebsiteUrl = "https://bank";

            orchestrator.ImportAccounts([preview]);

            var accounts = store.GetAllAccounts().ToList();
            Assert.That(accounts, Has.Count.EqualTo(1));
            Assert.That(accounts[0].Name, Is.EqualTo("New Checking"));
            Assert.That(accounts[0].AccountNumber, Is.EqualTo("999"));
            Assert.That(accounts[0].WebsiteUrl, Is.EqualTo("https://bank"));
        }

        [Test]
        public void ImportAccounts_ChangesTypeFromPreview()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountImportOrchestrator(store);
            var incoming = new BankAccount
            {
                Name = "Card",
                Institution = "Bank",
                AccountNumber = "111",
                WebsiteUrl = ""
            };
            var preview = AccountImportPreview.FromAccount(incoming);
            preview.Type = "Credit";
            preview.CreditLimit = 5000;
            preview.Apr = 0.1999m;

            orchestrator.ImportAccounts([preview]);

            var account = store.GetAllAccounts().Single();
            Assert.That(account, Is.TypeOf<CreditAccount>());
            var credit = (CreditAccount)account;
            Assert.That(credit.CreditLimit, Is.EqualTo(5000));
            Assert.That(credit.APR, Is.EqualTo(0.1999m));
        }

        [Test]
        public void ImportAccounts_AppliesLoanAndMortgageAprFromPreview()
        {
            var store = new InMemoryAccountDataStore();
            var orchestrator = new AccountImportOrchestrator(store);
            var loanPreview = AccountImportPreview.FromAccount(new LoanAccount
            {
                Name = "Auto",
                Institution = "Bank",
                AccountNumber = "1",
                WebsiteUrl = ""
            });
            loanPreview.Apr = 0.0699m;
            loanPreview.Principal = 10000;
            var mortgagePreview = AccountImportPreview.FromAccount(new MortgageAccount
            {
                Name = "House",
                Institution = "Bank",
                AccountNumber = "2",
                WebsiteUrl = ""
            });
            mortgagePreview.Apr = 2.875m;
            mortgagePreview.Principal = 200000;

            orchestrator.ImportAccounts([loanPreview, mortgagePreview]);

            var loan = store.GetAllAccounts().OfType<LoanAccount>().Single();
            var mortgage = store.GetAllAccounts().OfType<MortgageAccount>().Single();
            Assert.That(loan.InterestRate, Is.EqualTo(0.0699m));
            Assert.That(mortgage.InterestRate, Is.EqualTo(2.875m));
        }

        [Test]
        public void LoadAccountsFromFile_ParsesCsv()
        {
            var path = Path.Combine(Path.GetTempPath(), $"accounts-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, """
                Name,Type,Number,URL,CreditLimit,APR,Principal,Term
                Checking,Bank,111,https://bank,,,,
                Card,Credit,222,,5000,0.1999,,
                Auto,Loan,333,,,0.0699,29133.33,60
                House,Mortgage,444,,,2.875,39594.91,360
                """);
            try
            {
                var orchestrator = new AccountImportOrchestrator(new InMemoryAccountDataStore());
                var rows = orchestrator.LoadAccountsFromFile(path);

                Assert.That(rows, Has.Count.EqualTo(4));
                Assert.That(rows[0].Name, Is.EqualTo("Checking"));
                Assert.That(rows[0].Type, Is.EqualTo("Bank"));
                Assert.That(rows[1].Name, Is.EqualTo("Card"));
                Assert.That(rows[1].Type, Is.EqualTo("Credit"));
                Assert.That(rows[1].CreditLimit, Is.EqualTo(5000));
                Assert.That(rows[1].Apr, Is.EqualTo(0.1999m));
                Assert.That(rows[2].Type, Is.EqualTo("Loan"));
                Assert.That(rows[2].Apr, Is.EqualTo(0.0699m));
                Assert.That(rows[2].Principal, Is.EqualTo(29133.33m));
                Assert.That(rows[3].Type, Is.EqualTo("Mortgage"));
                Assert.That(rows[3].Apr, Is.EqualTo(2.875m));
                Assert.That(rows[3].Principal, Is.EqualTo(39594.91m));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void LoadAccountsFromFile_ThrowsWhenMissing()
        {
            var orchestrator = new AccountImportOrchestrator(new InMemoryAccountDataStore());
            Assert.That(
                () => orchestrator.LoadAccountsFromFile(@"C:\missing\accounts.xlsx"),
                Throws.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void Run_ReportsMissingMetadataDuplicatesOrphansAndEmptyAccounts()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var credit = new CreditAccount { Name = "Card", Institution = "X", AccountNumber = "1", WebsiteUrl = "" };
            var loan = new LoanAccount { Name = "Auto", Institution = "X", AccountNumber = "2", WebsiteUrl = "" };
            var mortgage = new MortgageAccount { Name = "House", Institution = "X", AccountNumber = "3", WebsiteUrl = "" };
            var checking = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "4", WebsiteUrl = "" };
            var duplicate = new BankAccount { Name = "Checking", Institution = "X", AccountNumber = "5", WebsiteUrl = "" };
            accounts.UpsertAccount(credit);
            accounts.UpsertAccount(loan);
            accounts.UpsertAccount(mortgage);
            accounts.UpsertAccount(checking);
            accounts.UpsertAccount(duplicate);

            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = Guid.NewGuid(),
                Date = DateTime.Today,
                Description = "Ghost",
                Amount = -10
            });
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = checking.Id,
                Date = DateTime.Today,
                Description = "Paycheck",
                Amount = 100
            });

            var findings = new AccountDiagnosticsOrchestrator(accounts, transactions).Run(["AMEX Card"]);

            Assert.That(findings, Has.Some.Contains("Missing APR: Card"));
            Assert.That(findings, Has.Some.Contains("Missing credit limit: Card"));
            Assert.That(findings, Has.Some.Contains("Missing loan term: Auto"));
            Assert.That(findings, Has.Some.Contains("Missing mortgage metadata"));
            Assert.That(findings, Has.Some.Contains("Duplicate accounts: Checking"));
            Assert.That(findings, Has.Some.Contains("Orphaned transaction Ghost"));
            Assert.That(findings, Has.Some.Contains("Account with no transactions: Card"));
            Assert.That(findings, Has.Some.Contains("spreadsheet 'AMEX Card' is not in the ledger"));
        }
    }
}
