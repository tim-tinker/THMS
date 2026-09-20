using THMS.Data.Stores;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class RecurringRuleImportOrchestratorTests
    {
        [Test]
        public void LoadRulesFromFile_ParsesKnownAccountsAndSkipsUnknown()
        {
            var path = WriteCsv("""
                Account,Description,Frequency,Last Occurrence,Last Amount,Date,Amount,Category
                WF Checking,Alarm Permit Renewal,Yearly,2025-11-01,-10,2026-11-01,-10,Home Services
                WF Checking,Allowance (Julie),Bi-weekly,2026-09-04,0,2026-09-18,-100,Julie
                Unknown,Skip me,Monthly,2026-01-01,-1,2026-02-01,-1,Charity
                """);
            try
            {
                var (orchestrator, _, _) = Create();
                var rows = orchestrator.LoadRulesFromFile(path);

                Assert.That(rows, Has.Count.EqualTo(2));
                Assert.That(rows[0].Account, Is.EqualTo("WF Checking"));
                Assert.That(rows[0].Description, Is.EqualTo("Alarm Permit Renewal"));
                Assert.That(rows[0].Frequency, Is.EqualTo("Yearly"));
                Assert.That(rows[0].LastOccurrence, Is.EqualTo(new DateTime(2025, 11, 1)));
                Assert.That(rows[0].NextOccurrence, Is.EqualTo(new DateTime(2026, 11, 1)));
                Assert.That(rows[0].Amount, Is.EqualTo(-10m));
                Assert.That(rows[0].Category, Is.EqualTo("Home Services"));
                Assert.That(rows[1].Frequency, Is.EqualTo("Biweekly"));
                Assert.That(rows[1].Amount, Is.EqualTo(-100m));
                Assert.That(rows.Any(r => r.Description == "Skip me"), Is.False);
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
                Account,Description,Frequency,Last Occurrence,Last Amount,Date,Amount,Category
                WF Checking,Donation: DSF,Monthly,2026-08-16,-25,2026-09-15,-25,Charity
                """);
            try
            {
                var (orchestrator, transactions, _) = Create();
                var rows = orchestrator.LoadRulesFromFile(path);
                Assert.That(orchestrator.ImportRules(rows).Count, Is.EqualTo(1));

                var first = transactions.GetAllRecurringSingleRules().Single();
                Assert.That(first.Description, Is.EqualTo("Donation: DSF"));
                Assert.That(first.Frequency, Is.EqualTo(RecurrenceFrequency.Monthly));
                Assert.That(first.Amount, Is.EqualTo(-25m));
                Assert.That(first.IsUserCreated, Is.True);
                Assert.That(first.IsActive, Is.True);
                Assert.That(first.Category, Is.EqualTo("Charity"));
                Assert.That(first.NextOccurrence, Is.EqualTo(new DateTime(2026, 9, 15)));

                rows[0].NextOccurrence = new DateTime(2026, 10, 15);
                Assert.That(orchestrator.ImportRules(rows).Count, Is.EqualTo(1));
                var updated = transactions.GetAllRecurringSingleRules().Single();
                Assert.That(updated.Id, Is.EqualTo(first.Id));
                Assert.That(updated.NextOccurrence, Is.EqualTo(new DateTime(2026, 10, 15)));
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
                () => orchestrator.LoadRulesFromFile(@"C:\missing\recurring.xlsx"),
                Throws.TypeOf<FileNotFoundException>());
        }

        [TestCase("Weekly", RecurrenceFrequency.Weekly)]
        [TestCase("Bi-weekly", RecurrenceFrequency.BiWeekly)]
        [TestCase("Biweekly", RecurrenceFrequency.BiWeekly)]
        [TestCase("Monthly", RecurrenceFrequency.Monthly)]
        [TestCase("Quarterly", RecurrenceFrequency.Quarterly)]
        [TestCase("Yearly", RecurrenceFrequency.Yearly)]
        [TestCase("Annual", RecurrenceFrequency.Yearly)]
        public void TryParseFrequency_AcceptsSpreadsheetLabels(string text, RecurrenceFrequency expected)
        {
            Assert.That(SpreadsheetRecurringRuleImporter.TryParseFrequency(text, out var frequency), Is.True);
            Assert.That(frequency, Is.EqualTo(expected));
        }

        private static (RecurringRuleImportOrchestrator orchestrator, InMemoryTransactionDataStore transactions, InMemoryAccountDataStore accounts) Create()
        {
            var accounts = new InMemoryAccountDataStore();
            accounts.UpsertAccount(new BankAccount
            {
                Name = "WF Checking",
                Institution = "Wells Fargo",
                AccountNumber = "1234",
                WebsiteUrl = ""
            });
            var transactions = new InMemoryTransactionDataStore();
            return (new RecurringRuleImportOrchestrator(accounts, transactions), transactions, accounts);
        }

        private static string WriteCsv(string csv)
        {
            var path = Path.Combine(Path.GetTempPath(), $"recurring-rules-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, csv);
            return path;
        }
    }
}
