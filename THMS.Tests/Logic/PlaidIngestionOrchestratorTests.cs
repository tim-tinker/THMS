using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.External;
using THMS.Logic.Orchestrators;
using THMS.Logic.Orchestrators.Finance;
using THMS.Logic.ViewModels;
using THMS.Logic.ViewModels.Finance;
using THMS.Tests.Logic.TestSupport;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class PlaidAccountOrchestratorTests
    {
        [Test]
        public async Task StartLinkFlow_LoadsAccountsAndSaveMappingsWritesExternalLink()
        {
            var accounts = new InMemoryAccountDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "",
                AccountNumber = "1234",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            var link = new FakePlaidLinkSession();
            var fetcher = new FakeAccountFetcher
            {
                Accounts =
                [
                    new AccountDto
                    {
                        PlaidAccountId = "plaid-checking",
                        Name = "Checking",
                        Mask = "1234",
                        Subtype = "checking"
                    }
                ]
            };
            var orchestrator = new PlaidAccountOrchestrator(accounts, fetcher, link, "Sandbox");

            await orchestrator.StartLinkFlow();

            Assert.That(link.CreatedLinkToken, Is.True);
            Assert.That(link.CreatedSandboxToken, Is.True);
            Assert.That(link.LastPublicToken, Is.EqualTo(link.SandboxPublicToken));
            Assert.That(fetcher.LastAccessToken, Is.EqualTo(link.AccessToken));

            var rows = orchestrator.GetPlaidAccounts();
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Institution, Is.EqualTo("First Platypus Bank"));
            Assert.That(rows[0].SuggestedThmsAccountId, Is.EqualTo(checking.Id));

            var saved = orchestrator.SaveAccountMappings(rows);
            Assert.That(saved, Is.EqualTo(1));

            var linked = accounts.GetAccount("Checking")!;
            Assert.That(linked.ExternalLink, Is.Not.Null);
            Assert.That(linked.ExternalLink!.PlaidAccountId, Is.EqualTo("plaid-checking"));
            Assert.That(linked.ExternalLink.AccessToken, Is.EqualTo(link.AccessToken));
            Assert.That(linked.ExternalLink.ItemId, Is.EqualTo(link.ItemId));
            Assert.That(linked.ExternalLink.AccountMask, Is.EqualTo("1234"));
            Assert.That(linked.ExternalLink.InstitutionName, Is.EqualTo("First Platypus Bank"));
            Assert.That(linked.Institution, Is.EqualTo("First Platypus Bank"));
        }

        [Test]
        public async Task StartLinkFlow_RequiresPublicTokenOutsideSandbox()
        {
            var orchestrator = new PlaidAccountOrchestrator(
                new InMemoryAccountDataStore(),
                new FakeAccountFetcher(),
                new FakePlaidLinkSession(),
                "Production");

            Assert.That(
                async () => await orchestrator.StartLinkFlow(),
                Throws.InvalidOperationException);
        }
    }

    [TestFixture]
    public class TransactionFileImportOrchestratorTests
    {
        [Test]
        public void LoadTransactionsFromFiles_ThrowsWhenMissing()
        {
            var orchestrator = new TransactionImportOrchestrator(
                new FakeTransactionFetcher(),
                new InMemoryTransactionDataStore(),
                new InMemoryAccountDataStore());

            Assert.That(
                () => orchestrator.LoadTransactionsFromFiles([@"C:\missing\transactions.xlsx"]),
                Throws.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void ImportTransactions_PersistsCategorizesAndSkipsDuplicates()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                WebsiteUrl = ""
            };
            accounts.UpsertAccount(checking);
            var orchestrator = new TransactionImportOrchestrator(
                new FakeTransactionFetcher(),
                txs,
                accounts);
            var rows = new List<TransactionImportPreview>
            {
                new()
                {
                    Date = new DateTime(2026, 3, 1),
                    Description = "Coffee",
                    Amount = -4.50m,
                    Account = "Checking",
                    Category = "Restaurants",
                    AccountId = checking.Id
                },
                new()
                {
                    Date = new DateTime(2026, 3, 1),
                    Description = "Coffee",
                    Amount = -4.50m,
                    Account = "Checking",
                    Category = "Restaurants",
                    AccountId = checking.Id
                }
            };

            var reports = new List<ImportProgress>();
            var imported = orchestrator.ImportTransactions(
                rows,
                new CollectingProgress<ImportProgress>(reports));

            Assert.That(imported.Count, Is.EqualTo(1));
            Assert.That(imported.Start, Is.EqualTo(new DateTime(2026, 3, 1)));
            Assert.That(imported.End, Is.EqualTo(new DateTime(2026, 3, 1)));
            Assert.That(reports.Select(r => r.Total).Distinct().ToList(), Is.EqualTo(new[] { 1 }));
            Assert.That(reports.Last().Completed, Is.EqualTo(imported.Count));
            var posted = txs.GetPostedTransactions(checking.Id).ToList();
            Assert.That(posted, Has.Count.EqualTo(1));
            Assert.That(posted[0].Category, Is.EqualTo("Restaurants"));
            Assert.That(posted[0].CategoryId, Is.Not.Null);
        }
    }

    [TestFixture]
    public class PlaidTransactionOrchestratorTests
    {
        [Test]
        public async Task DownloadNewTransactions_MapsByPlaidAccountId_AndImportSkipsPending()
        {
            var accounts = new InMemoryAccountDataStore();
            var txs = new InMemoryTransactionDataStore();
            var checking = new BankAccount
            {
                Name = "Checking",
                Institution = "Bank",
                AccountNumber = "1",
                WebsiteUrl = "",
                ExternalLink = new ExternalAccountLink
                {
                    AccessToken = "tok",
                    PlaidAccountId = "plaid-checking",
                    ItemId = "item",
                    InstitutionId = "ins_1",
                    AccountMask = "1111"
                }
            };
            accounts.UpsertAccount(checking);
            var fetcher = new FakeTransactionFetcher
            {
                Transactions =
                [
                    new TransactionDto
                    {
                        AccountId = "plaid-checking",
                        Amount = -12.34m,
                        Date = new DateTime(2026, 4, 2),
                        Name = "Market",
                        Category = "Groceries",
                        Pending = false
                    },
                    new TransactionDto
                    {
                        AccountId = "plaid-checking",
                        Amount = -1m,
                        Date = new DateTime(2026, 4, 3),
                        Name = "Hold",
                        Pending = true
                    },
                    new TransactionDto
                    {
                        AccountId = "other-plaid",
                        Amount = -9m,
                        Date = new DateTime(2026, 4, 2),
                        Name = "Other bank"
                    }
                ]
            };
            var orchestrator = new PlaidTransactionOrchestrator(accounts, fetcher, txs);

            var preview = await orchestrator.DownloadNewTransactions(
                new DateTime(2026, 4, 1),
                new DateTime(2026, 4, 30));

            Assert.That(preview, Has.Count.EqualTo(2));
            Assert.That(preview.Any(r => r.Description == "Market" && r.AccountId == checking.Id), Is.True);
            Assert.That(preview.Any(r => r.Pending && r.Description == "Hold"), Is.True);
            Assert.That(preview.Any(r => r.Description == "Other bank"), Is.False);

            var imported = orchestrator.ImportTransactions(preview);
            Assert.That(imported.Count, Is.EqualTo(1));
            Assert.That(txs.GetPostedTransactions(checking.Id).Single().Description, Is.EqualTo("Market"));
        }

        [Test]
        public void DownloadNewTransactions_ThrowsWhenNoLinkedAccounts()
        {
            var orchestrator = new PlaidTransactionOrchestrator(
                new InMemoryAccountDataStore(),
                new FakeTransactionFetcher(),
                new InMemoryTransactionDataStore());

            Assert.That(
                async () => await orchestrator.DownloadNewTransactions(DateTime.Today.AddDays(-30), DateTime.Today),
                Throws.InvalidOperationException);
        }
    }

    file sealed class CollectingProgress<T>(List<T> items) : IProgress<T>
    {
        public void Report(T value) => items.Add(value);
    }
}
