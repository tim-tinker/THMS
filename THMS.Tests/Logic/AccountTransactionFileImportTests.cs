using Microsoft.Data.Sqlite;
using THMS.Data.Stores;
using THMS.Data.Stores.SQLite;
using THMS.Domain.Finance.Accounts;
using THMS.Domain.Finance.Transactions;
using THMS.Ingestion.Importers.Finance;
using THMS.Logic.Orchestrators;

namespace THMS.Tests.Logic
{
    [TestFixture]
    public class AccountTransactionFileImportTests
    {
        [Test]
        public void Read_ParsesAmexStyleCsv_AndSkipsBlankLines()
        {
            var path = WriteTempCsv(
                "\nDate,Description,Amount\n" +
                "09/22/2026,AUTOPAY PAYMENT - THANK YOU,-953.86\n" +
                "09/07/2026,Interest Charge,219.30\n" +
                "06/28/2026,AMEX LOAN DISBURSEMENT,\"30,000.00\"\n");
            try
            {
                var sheet = new AccountTransactionFileReader().Read(path);
                Assert.That(sheet.Headers, Is.EqualTo(new[] { "Date", "Description", "Amount" }));
                Assert.That(sheet.Rows, Has.Count.EqualTo(3));
                Assert.That(sheet.Rows[0].SourceRow, Is.EqualTo(3));
                Assert.That(sheet.Rows[2].Cells[2], Is.EqualTo("30,000.00"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Apply_AmountColumn_FlipsSignAndParsesAccountingFormats()
        {
            var sheet = Sheet(
                ["Date", "Description", "Amount"],
                Row(2, "09/22/2026", "Payment", "($1,234.56)"),
                Row(3, "09/07/2026", "Interest", "10.00-"),
                Row(4, "09/01/2026", "Plain", "15.25"));

            var reader = new AccountTransactionFileReader();
            var normal = reader.Apply(sheet, Map(flip: false));
            Assert.That(normal[0].Amount, Is.EqualTo(-1234.56m));
            Assert.That(normal[1].Amount, Is.EqualTo(-10m));
            Assert.That(normal[2].Amount, Is.EqualTo(15.25m));

            var flipped = reader.Apply(sheet, Map(flip: true));
            Assert.That(flipped[2].Amount, Is.EqualTo(-15.25m));
        }

        [Test]
        public void Apply_DebitCredit_CreditsIncreaseAndDebitsDecrease()
        {
            var sheet = Sheet(
                ["Posted Date", "Memo", "Debit", "Credit", "Balance"],
                Row(2, "01/02/2026", "Deposit", "", "15.00", "100"),
                Row(3, "01/03/2026", "Withdrawal", "40", "-", "60"),
                Row(4, "01/04/2026", "Net", "10.00", "3.00", "53"),
                Row(5, "01/05/2026", "Missing", "-", "-", "53"),
                Row(6, "", "", "", "", ""));

            var map = new TransactionFileColumnMap
            {
                DateColumn = "Posted Date",
                DescriptionColumn = "Memo",
                UseDebitCredit = true,
                DebitColumn = "Debit",
                CreditColumn = "Credit"
            };
            var rows = new AccountTransactionFileReader().Apply(sheet, map);

            Assert.That(rows, Has.Count.EqualTo(4));
            Assert.That(rows[0].Amount, Is.EqualTo(15m));
            Assert.That(rows[1].Amount, Is.EqualTo(-40m));
            Assert.That(rows[2].Amount, Is.EqualTo(-7m));
            Assert.That(rows[3].Problem, Does.Contain("Amount"));
        }

        [Test]
        public void Suggest_PrefersAmount_AndFallsBackToDebitCredit()
        {
            var accountId = Guid.NewGuid();
            var amount = TransactionFileColumnMapping.Suggest(accountId, ["Date", "Description", "Amount", "Debit", "Credit"]);
            Assert.That(amount.UseDebitCredit, Is.False);
            Assert.That(amount.AmountColumn, Is.EqualTo("Amount"));

            var split = TransactionFileColumnMapping.Suggest(accountId, ["Transaction Date", "Details", "Withdrawal", "Deposit"]);
            Assert.That(split.UseDebitCredit, Is.True);
            Assert.That(split.DateColumn, Is.EqualTo("Transaction Date"));
            Assert.That(split.DescriptionColumn, Is.EqualTo("Details"));
            Assert.That(split.DebitColumn, Is.EqualTo("Withdrawal"));
            Assert.That(split.CreditColumn, Is.EqualTo("Deposit"));
        }

        [Test]
        public void BuildPreview_MarksLedgerAndFileDuplicates_AndImportKeepsCheckedDuplicates()
        {
            var accounts = new InMemoryAccountDataStore();
            var transactions = new InMemoryTransactionDataStore();
            var account = new BankAccount { Name = "Amex Loan", Institution = "American Express", AccountNumber = "1" };
            accounts.UpsertAccount(account);
            transactions.AddPostedTransaction(new PostedTransaction
            {
                AccountId = account.Id,
                Date = new DateTime(2026, 9, 7),
                Amount = 219.30m,
                Description = "interest charge"
            });

            var sheet = Sheet(
                ["Date", "Description", "Amount"],
                Row(2, "09/22/2026", "AUTOPAY PAYMENT - THANK YOU", "-953.86"),
                Row(3, "09/07/2026", "Interest Charge", "219.30"),
                Row(4, "09/22/2026", "AUTOPAY PAYMENT - THANK YOU", "-953.86"),
                Row(5, "not-a-date", "Bad row", "12.00"));

            var orchestrator = new AccountTransactionFileImportOrchestrator(accounts, transactions);
            var preview = orchestrator.BuildPreview(account.Id, sheet, Map(flip: false));

            Assert.That(preview[0].Import, Is.True);
            Assert.That(preview[0].IsDuplicate, Is.False);
            Assert.That(preview[0].Amount, Is.EqualTo(-953.86m));
            Assert.That(preview[1].IsDuplicate, Is.True);
            Assert.That(preview[1].Import, Is.False);
            Assert.That(preview[2].IsDuplicate, Is.True);
            Assert.That(preview[2].Import, Is.False);
            Assert.That(preview[3].Problem, Does.Contain("Date"));
            Assert.That(preview[3].Import, Is.False);

            preview[1].Import = true;
            var result = orchestrator.Import(account.Id, preview);
            Assert.That(result.Count, Is.EqualTo(2));

            var posted = transactions.GetPostedTransactions(account.Id).ToList();
            Assert.That(posted, Has.Count.EqualTo(3));
            Assert.That(posted.Count(tx => tx.Amount == 219.30m), Is.EqualTo(2));
            Assert.That(
                posted.Where(tx => tx.Description == "AUTOPAY PAYMENT - THANK YOU"),
                Has.All.Matches<PostedTransaction>(tx => tx.ImportedStatus == ImportedStatus.Unreconciled));
            Assert.That(posted.Single(tx => tx.Amount == -953.86m).ImportedStatus, Is.EqualTo(ImportedStatus.Unreconciled));
        }

        [Test]
        public void Sqlite_RemembersColumnMapUntilAccountIsDeleted()
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-tx-map-{Guid.NewGuid():N}.db");
            try
            {
                var store = new SQLiteAccountDataStore(path);
                var account = new BankAccount
                {
                    Name = "Amex Loan",
                    Institution = "American Express",
                    AccountNumber = "1"
                };
                store.UpsertAccount(account);
                store.UpsertTransactionFileColumnMap(new TransactionFileColumnMap
                {
                    AccountId = account.Id,
                    DateColumn = "Date",
                    DescriptionColumn = "Description",
                    UseDebitCredit = true,
                    DebitColumn = "Debit",
                    CreditColumn = "Credit",
                    FlipSign = true
                });

                var loaded = new SQLiteAccountDataStore(path).GetTransactionFileColumnMap(account.Id);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.UseDebitCredit, Is.True);
                Assert.That(loaded.DateColumn, Is.EqualTo("Date"));
                Assert.That(loaded.DebitColumn, Is.EqualTo("Debit"));
                Assert.That(loaded.CreditColumn, Is.EqualTo("Credit"));
                Assert.That(loaded.FlipSign, Is.True);

                store.DeleteAccount(account.Id);
                Assert.That(store.GetTransactionFileColumnMap(account.Id), Is.Null);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static TransactionFileColumnMap Map(bool flip) =>
            new()
            {
                DateColumn = "Date",
                DescriptionColumn = "Description",
                AmountColumn = "Amount",
                FlipSign = flip
            };

        private static TransactionFileSheet Sheet(string[] headers, params TransactionFileRawRow[] rows) =>
            new() { Headers = headers, Rows = rows };

        private static TransactionFileRawRow Row(int sourceRow, params string[] cells) =>
            new() { SourceRow = sourceRow, Cells = cells };

        private static string WriteTempCsv(string contents)
        {
            var path = Path.Combine(Path.GetTempPath(), $"thms-tx-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, contents);
            return path;
        }
    }
}
