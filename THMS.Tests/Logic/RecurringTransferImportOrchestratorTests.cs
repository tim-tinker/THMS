using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class RecurringTransferImportOrchestratorTests
    {
        [Test]
        public void LoadRulesFromFile_ParsesKnownAccountsAndSkipsUnknown()
        {
            var path = WriteCsv("""
                From Account,To Account,Description,Frequency,Last Occurrence,Last Amount,Date,Amount,Category
                WF Checking,Credit: Amazon Store Card,Payment,Bi-weekly,2026-08-31,20,2026-09-14,0,Credit Card Payment
                WF Checking,Fidelity HSA (Julie),HSA Transfer,Monthly,2026-08-28,448,2026-09-27,448,Transfer
                WF Checking,Unknown Destination,Skip me,Monthly,2026-01-01,1,2026-02-01,1,Transfer
                Missing Source,WF Checking,Also skip,Yearly,2025-01-01,900,2026-12-01,900,Transfer
                WF Checking,WF Checking,Same account,Monthly,2026-01-01,10,2026-02-01,10,Transfer
                """);
            try
            {
                var (orchestrator, _, _) = Create();
                var rows = orchestrator.LoadRulesFromFile(path);

                Assert.That(rows, Has.Count.EqualTo(2));
                Assert.That(rows[0].FromAccount, Is.EqualTo("WF Checking"));
                Assert.That(rows[0].ToAccount, Is.EqualTo("Credit: Amazon Store Card"));
                Assert.That(rows[0].Description, Is.EqualTo("Payment"));
                Assert.That(rows[0].Frequency, Is.EqualTo("Biweekly"));
                Assert.That(rows[0].LastOccurrence, Is.EqualTo(new DateTime(2026, 8, 31)));
                Assert.That(rows[0].NextOccurrence, Is.EqualTo(new DateTime(2026, 9, 14)));
                Assert.That(rows[0].Amount, Is.EqualTo(20m));
                Assert.That(rows[0].Category, Is.EqualTo("Credit Card Payment"));
                Assert.That(rows[1].Description, Is.EqualTo("HSA Transfer"));
                Assert.That(rows[1].Frequency, Is.EqualTo("Monthly"));
                Assert.That(rows[1].Amount, Is.EqualTo(448m));
                Assert.That(rows.Any(r => r.Description is "Skip me" or "Also skip" or "Same account"), Is.False);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ImportRules_CreatesUserRulesAndUpsertsMatchingPattern()
        {
            var path = WriteCsv("""
                From Account,To Account,Description,Frequency,Last Occurrence,Last Amount,Date,Amount,Category
                WF Checking,Credit: Amazon Store Card,Payment,Bi-weekly,2026-08-31,20,2026-09-14,20,Credit Card Payment
                """);
            try
            {
                var (orchestrator, transactions, accounts) = Create();
                var rows = orchestrator.LoadRulesFromFile(path);
                Assert.That(orchestrator.ImportRules(rows).Count, Is.EqualTo(1));

                var first = transactions.GetAllRecurringTransferRules().Single();
                Assert.That(first.FromAccountId, Is.EqualTo(accounts.GetAccount("WF Checking")!.Id));
                Assert.That(first.ToAccountId, Is.EqualTo(accounts.GetAccount("Credit: Amazon Store Card")!.Id));
                Assert.That(first.Description, Is.EqualTo("Payment"));
                Assert.That(first.Frequency, Is.EqualTo(RecurrenceFrequency.BiWeekly));
                Assert.That(first.Amount, Is.EqualTo(20m));
                Assert.That(first.IsUserCreated, Is.True);
                Assert.That(first.IsActive, Is.True);
                Assert.That(first.Category, Is.EqualTo("Credit Card Payment"));
                Assert.That(first.NextOccurrence, Is.EqualTo(new DateTime(2026, 9, 14)));
                Assert.That(first.LastOccurrence, Is.EqualTo(new DateTime(2026, 8, 31)));

                rows[0].NextOccurrence = new DateTime(2026, 9, 28);
                Assert.That(orchestrator.ImportRules(rows).Count, Is.EqualTo(1));
                var updated = transactions.GetAllRecurringTransferRules().Single();
                Assert.That(updated.Id, Is.EqualTo(first.Id));
                Assert.That(updated.NextOccurrence, Is.EqualTo(new DateTime(2026, 9, 28)));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void LoadRulesFromFile_ThrowsWhenMissing()
        {
            var (orchestrator, _, _) = Create();
            Assert.That(
                () => orchestrator.LoadRulesFromFile(@"C:\missing\recurring-transfers.xlsx"),
                Throws.TypeOf<FileNotFoundException>());
        }

        private static (RecurringTransferImportOrchestrator orchestrator, InMemoryTransactionDataStore transactions, InMemoryAccountDataStore accounts) Create()
        {
            var accounts = new InMemoryAccountDataStore();
            accounts.UpsertAccount(new BankAccount
            {
                Name = "WF Checking",
                Institution = "Wells Fargo",
                AccountNumber = "1234",
                WebsiteUrl = ""
            });
            accounts.UpsertAccount(new BankAccount
            {
                Name = "Credit: Amazon Store Card",
                Institution = "Synchrony",
                AccountNumber = "5678",
                WebsiteUrl = ""
            });
            accounts.UpsertAccount(new BankAccount
            {
                Name = "Fidelity HSA (Julie)",
                Institution = "Fidelity",
                AccountNumber = "9012",
                WebsiteUrl = ""
            });
            var transactions = new InMemoryTransactionDataStore();
            return (new RecurringTransferImportOrchestrator(accounts, transactions), transactions, accounts);
        }

        private static string WriteCsv(string csv)
        {
            var path = Path.Combine(Path.GetTempPath(), $"recurring-transfers-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, csv);
            return path;
        }
    }
}
